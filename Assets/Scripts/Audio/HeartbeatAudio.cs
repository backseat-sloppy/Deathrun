using System.Collections;
using UnityEngine;

public class HeartbeatAudio : MonoBehaviour
{
    [Header("Wwise Events")]
    public string playHeartInEvent = "Play_Heart_In";
    public string playHeartOutEvent = "Play_Heart_Out";

    [Header("Heartbeat Settings")]
    [Tooltip("Clamp BPM to avoid crazy timings")]
    public float minBpm = 40f;
    public float maxBpm = 180f;

    [Tooltip("Fraction of beat interval before the OUT sound (0–1)")]
    [Range(0f, 1f)]
    public float outDelayFraction = 0.3f;

    // Call this from your HR tracker script whenever you get a new beat/BPM
    public void OnNewHeartbeat(float bpm)
    {
        bpm = Mathf.Clamp(bpm, minBpm, maxBpm);

        // Send BPM to Wwise RTPC (optional)
        AkSoundEngine.SetRTPCValue("Heartbeat_BPM", bpm, gameObject);

        float beatInterval = 60f / bpm; // seconds per beat
        float outDelay = beatInterval * outDelayFraction;

        // Play IN immediately
        AkSoundEngine.PostEvent(playHeartInEvent, gameObject);

        // Schedule OUT after delay
        StartCoroutine(PlayHeartOutAfterDelay(outDelay));
    }

    private IEnumerator PlayHeartOutAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        AkSoundEngine.PostEvent(playHeartOutEvent, gameObject);
    }
}