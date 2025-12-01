using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class HeartbeatRumbleWwise_PostEvent : MonoBehaviour
{
    [Header("Heartbeat Settings")]
    public float baseIntensity = 0.03f;     // starting intensity at base BPM
    public float baseBeatDuration = 0.4f;  // starting beat duration at base BPM
    public float intensity; // dynamically updated, visible in Inspector
    public float beatDuration; // dynamically updated, visible in Inspector

    [Header("Wwise Events")]
    public AK.Wwise.Event playHeartInEvent;
    public AK.Wwise.Event playHeartOutEvent;

    [Header("Heart Rate Settings")]
    public float initialBPM = 60f;         // fixed heart rate
    public float bpm = 60f;

    private Gamepad pad;
    private Coroutine heartbeatRoutine;
    
  //  public RPPGProcessor rppgProcessor; // drag your RPPG GameObject here in Inspector

    private void Start()
    {
        pad = Gamepad.current;

        if (pad != null)
        {
            heartbeatRoutine = StartCoroutine(HeartbeatLoop());
        }
        else
        {
            Debug.Log("No gamepad found!");
        }
    }

    private IEnumerator HeartbeatLoop()
    {
        while (true)
        {
       
        //bpm = rppgProccesor.CurrentBPM;
            if (bpm < initialBPM)
            {
                yield return null; // wait one frame, then check again
                continue;          // go back to start of while loop
            }

            // --- Calculate dynamic intensity and beat duration based on BPM ---
            intensity = baseIntensity * ((bpm*bpm) / (initialBPM * initialBPM));          // stronger with higher BPM
            beatDuration = baseBeatDuration * (initialBPM * 3 / bpm);   // shorter with higher BPM

            // Total heartbeat interval
            float timeBetweenHeartbeats = initialBPM / bpm;
            float timeBetweenBeats = beatDuration * 0.3f; // small gap between first and second beat

            // --- First beat: high frequency only ---
            pad.SetMotorSpeeds(0f, intensity);
            playHeartInEvent.Post(gameObject);
            yield return new WaitForSeconds(beatDuration);
            pad.SetMotorSpeeds(0f, 0f);
            yield return new WaitForSeconds(timeBetweenBeats);

            // --- Second beat: low frequency only ---
            pad.SetMotorSpeeds(intensity, 0f);
            playHeartOutEvent.Post(gameObject);
            yield return new WaitForSeconds(beatDuration);
            pad.SetMotorSpeeds(0f, 0f);

            // Wait remaining time to match full BPM
            yield return new WaitForSeconds(timeBetweenHeartbeats - (beatDuration * 2 + timeBetweenBeats));
        }
    }

    private void OnDisable()
    {
        if (pad != null)
            pad.SetMotorSpeeds(0f, 0f);

        if (heartbeatRoutine != null)
            StopCoroutine(heartbeatRoutine);
    }
}
