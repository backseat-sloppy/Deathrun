using UnityEngine;
using UnityEngine.UI;

public class WebcamController : MonoBehaviour
{
    [Header("Display Options (use one)")]
    [SerializeField] private RawImage displayImage;  // For UI display
    [SerializeField] private Renderer planeRenderer;  // For 3D plane display
    
    [Header("Webcam Settings")]
    [SerializeField] private string webcamName = "";
    
    public WebCamTexture CameraTexture { get; private set; }
    public bool IsPlaying => CameraTexture != null && CameraTexture.isPlaying;

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        
        Debug.Log($"Available webcams: {devices.Length}");
        foreach (WebCamDevice webcam in devices)
        {
            Debug.Log($"  - {webcam.name}");
        }

        // Create and start webcam
        CameraTexture = string.IsNullOrEmpty(webcamName) 
            ? new WebCamTexture(640, 480, 30)
            : new WebCamTexture(webcamName, 640, 480, 30);
        
        // Apply to UI if available
        if (displayImage != null)
        {
            displayImage.texture = CameraTexture;
        }
        
        // Apply to 3D plane if available
        if (planeRenderer != null)
        {
            planeRenderer.material.mainTexture = CameraTexture;
        }
        
        CameraTexture.Play();
        
        if (CameraTexture.isPlaying)
        {
            Debug.Log($"Webcam started: {CameraTexture.deviceName} ({CameraTexture.width}x{CameraTexture.height})");
        }
    }

    public Color32[] GetPixelData()
    {
        return CameraTexture?.GetPixels32();
    }

    void OnDestroy()
    {
        if (CameraTexture != null && CameraTexture.isPlaying)
        {
            CameraTexture.Stop();
        }
    }
}
