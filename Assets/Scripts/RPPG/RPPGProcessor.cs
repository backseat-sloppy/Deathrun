using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RPPGProcessor : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private ROISelector roiSelector;
    
    [Header("Filter Settings")]
    [SerializeField] private float minBPM = 45f;
    [SerializeField] private float maxBPM = 150f;
    [SerializeField] private float samplingRate = 30f; // Should match webcam FPS
    
    [Header("Peak Detection")]
    [SerializeField] private float thresholdMultiplier = 0.6f;
    [SerializeField] private int windowSize = 60; // Number of samples for dynamic threshold
    
    [Header("Output")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Public properties
    public float CurrentBPM { get; private set; }
    public float SignalQuality { get; private set; }
    public bool IsProcessing { get; private set; }
    
    // IIR Bandpass filter coefficients and state
    private float[] b = new float[5]; // Numerator coefficients
    private float[] a = new float[5]; // Denominator coefficients
    private float[] x = new float[5]; // Input history
    private float[] y = new float[5]; // Output history
    
    // Signal processing
    private Queue<float> rawSignal = new Queue<float>();
    private Queue<float> filteredSignal = new Queue<float>();
    private List<float> peakTimes = new List<float>();
    
    // Peak detection
    private float lastPeakTime = 0f;
    private float minPeakDistance = 0.4f; // Minimum 0.4s between peaks (150 BPM)
    private bool wasAboveThreshold = false;
    
    // Signal quality metrics
    private float runningMean = 128f;
    private float signalPower = 0f;
    private float noisePower = 0f;
    
    void Start()
    {
        if (roiSelector == null)
        {
            roiSelector = GetComponent<ROISelector>();
        }
        
        CalculateFilterCoefficients();
        IsProcessing = true;
        
        Debug.Log("RPPGProcessor initialized. Filter range: " + minBPM + "-" + maxBPM + " BPM");
    }
    
    void Update()
    {
        if (!IsProcessing) return;
        
        ProcessFrame();
    }
    
    void ProcessFrame()
    {
        // Get ROI pixels
        Color32[] roiPixels = roiSelector.GetROIPixels();
        if (roiPixels == null || roiPixels.Length == 0) return;
        
        // Extract green channel average (most sensitive to blood volume changes)
        float greenAverage = 0f;
        foreach (Color32 pixel in roiPixels)
        {
            greenAverage += pixel.g;
        }
        greenAverage /= roiPixels.Length;
        
        // Update running mean with exponential moving average
        runningMean = 0.98f * runningMean + 0.02f * greenAverage;
        
        // Normalize by removing DC component (detrending)
        float normalized = greenAverage - runningMean;
        
        // Add to raw signal buffer
        rawSignal.Enqueue(normalized);
        if (rawSignal.Count > windowSize * 2)
        {
            rawSignal.Dequeue();
        }
        
        // Apply bandpass filter
        float filtered = ApplyBandpassFilter(normalized);
        filteredSignal.Enqueue(filtered);
        if (filteredSignal.Count > windowSize * 2)
        {
            filteredSignal.Dequeue();
        }
        
        // Calculate signal quality based on SNR
        UpdateSignalQuality(filtered, normalized);
        
        // Detect peaks and calculate BPM
        if (filteredSignal.Count >= windowSize)
        {
            DetectPeaksAndCalculateBPM(filtered);
        }
    }
    
    void UpdateSignalQuality(float filtered, float raw)
    {
        // Signal power (from filtered bandpass output)
        signalPower = 0.95f * signalPower + 0.05f * (filtered * filtered);
        
        // Noise power (difference between raw and filtered)
        float noise = raw - filtered;
        noisePower = 0.95f * noisePower + 0.05f * (noise * noise);
        
        // Calculate SNR (Signal-to-Noise Ratio)
        if (noisePower > 0.001f)
        {
            float snr = signalPower / noisePower;
            // Convert SNR to 0-1 quality metric (SNR > 2 is good for rPPG)
            SignalQuality = Mathf.Clamp01(snr / 3f);
        }
        else
        {
            SignalQuality = 0f;
        }
    }
    
    float ApplyBandpassFilter(float input)
    {
        // Shift input history
        for (int i = 4; i > 0; i--)
        {
            x[i] = x[i - 1];
        }
        x[0] = input;
        
        // Shift output history
        for (int i = 4; i > 0; i--)
        {
            y[i] = y[i - 1];
        }
        
        // Apply IIR filter: y[n] = b0*x[n] + b1*x[n-1] + ... - a1*y[n-1] - a2*y[n-2] - ...
        y[0] = b[0] * x[0] + b[1] * x[1] + b[2] * x[2] + b[3] * x[3] + b[4] * x[4]
             - a[1] * y[1] - a[2] * y[2] - a[3] * y[3] - a[4] * y[4];
        
        return y[0];
    }
    
    void DetectPeaksAndCalculateBPM(float currentValue)
    {
        // Calculate dynamic threshold based on recent signal
        float threshold = CalculateDynamicThreshold();
        
        // Peak detection: look for threshold crossing with local maxima
        bool isAboveThreshold = currentValue > threshold;
        
        if (isAboveThreshold && !wasAboveThreshold)
        {
            // Rising edge - potential peak
            float timeSinceLastPeak = Time.time - lastPeakTime;
            
            if (timeSinceLastPeak >= minPeakDistance)
            {
                // Valid peak detected
                peakTimes.Add(Time.time);
                lastPeakTime = Time.time;
                
                // Keep only recent peaks (last 10 seconds)
                while (peakTimes.Count > 0 && Time.time - peakTimes[0] > 10f)
                {
                    peakTimes.RemoveAt(0);
                }
                
                // Calculate BPM from recent peaks
                if (peakTimes.Count >= 3)
                {
                    CalculateBPMFromPeaks();
                }
                
                if (showDebugInfo)
                {
                    Debug.Log($"Peak detected! BPM: {CurrentBPM:F1}, Quality: {SignalQuality:F2}");
                }
            }
        }
        
        wasAboveThreshold = isAboveThreshold;
    }
    
    float CalculateDynamicThreshold()
    {
        if (filteredSignal.Count < windowSize) return 0f;
        
        float sum = 0f;
        float sumSquared = 0f;
        int count = 0;
        
        foreach (float value in filteredSignal)
        {
            sum += value;
            sumSquared += value * value;
            count++;
        }
        
        float mean = sum / count;
        float variance = (sumSquared / count) - (mean * mean);
        float stdDev = Mathf.Sqrt(Mathf.Max(0, variance));
        
        return mean + (stdDev * thresholdMultiplier);
    }
    
    void CalculateBPMFromPeaks()
    {
        if (peakTimes.Count < 2) return;
        
        // Calculate average interval between recent peaks
        float totalInterval = 0f;
        for (int i = 1; i < peakTimes.Count; i++)
        {
            totalInterval += peakTimes[i] - peakTimes[i - 1];
        }
        
        float avgInterval = totalInterval / (peakTimes.Count - 1);
        
        // Convert to BPM
        float bpm = 60f / avgInterval;
        
        // Clamp to valid range
        bpm = Mathf.Clamp(bpm, minBPM, maxBPM);
        
        // Smooth BPM update
        CurrentBPM = Mathf.Lerp(CurrentBPM, bpm, 0.3f);
    }
    
    void CalculateFilterCoefficients()
    {
        // Convert BPM to Hz
        float lowFreq = minBPM / 60f;
        float highFreq = maxBPM / 60f;
        
        // Calculate center frequency and bandwidth
        float centerFreq = (lowFreq + highFreq) / 2f;
        float bandwidth = highFreq - lowFreq;
        
        // Normalize frequencies
        float w0 = 2f * Mathf.PI * centerFreq / samplingRate;
        float bw = 2f * Mathf.PI * bandwidth / samplingRate;
        
        // Calculate Q factor
        float Q = w0 / bw;
        
        // Second-order bandpass filter coefficients (bilinear transform)
        float K = Mathf.Tan(w0 / 2f);
        float norm = 1f / (1f + K / Q + K * K);
        
        // Numerator coefficients
        b[0] = K / Q * norm;
        b[1] = 0f;
        b[2] = -b[0];
        b[3] = 0f;
        b[4] = 0f;
        
        // Denominator coefficients (a[0] is always 1)
        a[0] = 1f;
        a[1] = 2f * (K * K - 1f) * norm;
        a[2] = (1f - K / Q + K * K) * norm;
        a[3] = 0f;
        a[4] = 0f;
        
        if (showDebugInfo)
        {
            Debug.Log($"Filter coefficients calculated: Center={centerFreq:F3}Hz, BW={bandwidth:F3}Hz, Q={Q:F2}");
        }
    }
    
    void OnGUI()
    {
        if (!showDebugInfo || !IsProcessing) return;
        
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 24;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.UpperLeft;
        
        string info = $"BPM: {CurrentBPM:F1}\nQuality: {(SignalQuality * 100):F0}%\nPeaks: {peakTimes.Count}\nSNR: {(signalPower / Mathf.Max(noisePower, 0.001f)):F2}";
        
        // Draw background
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.Box(new Rect(10, 10, 200, 120), "");
        
        // Draw text
        GUI.color = Color.white;
        GUI.Label(new Rect(20, 20, 180, 100), info, style);
    }
}
