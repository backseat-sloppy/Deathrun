using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; 
using Unity.Netcode; // Include Netcode for networked functionality/checking (optional, but good practice if this parent is a NetworkObject)

/// <summary>
/// Controls the activation sequence (fall and rise) of stepping stones 
/// when triggered by the VRTrapActivator script.
/// </summary>
public class ActivationButton : MonoBehaviour 
{
    // The stepping stones are assumed to be children of this GameObject (the one with this script attached).
    
    // --- TIMING SETTINGS ---
    [Header("Timing Settings")]
    [Tooltip("Time delay between each stone's fall activation.")]
    public float fallStaggerDelay = 0.2f; 

    [Tooltip("Time in seconds before all stones rise back up after the sequence finishes.")]
    public float riseResetTime = 5.0f; 

    // --- COOLDOWN LOGIC ---
    [Header("Cooldown Settings")]
    [Tooltip("Time in seconds the player must wait after activation before pressing the button again.")]
    public float activationCooldown = 10.0f; 

    private float nextActivationTime = 0f;
    private bool isSequenceRunning = false; 

    // **REMOVED: OnTriggerEnter and OnPlayerActivate()**
    // The activation is now handled externally by VRTrapActivator calling ActivateTrap()

    /// <summary>
    /// PUBLIC method called by VRTrapActivator's SendMessage across the network.
    /// Initiates the stepping stone sequence, checking for running status and cooldown.
    /// </summary>
    public void ActivateTrap() // **RENAME: OnPlayerActivate() -> ActivateTrap()**
    {
        // 1. Check if the button is currently running the sequence
        if (isSequenceRunning)
        {
            Debug.Log("[VRSteppingStoneTrap] Sequence is already processing. Please wait.");
            return;
        }

        // 2. Check the Cooldown Time
        if (Time.time < nextActivationTime)
        {
            float remainingTime = nextActivationTime - Time.time;
            Debug.Log($"[VRSteppingStoneTrap] On cooldown. Wait {Mathf.Ceil(remainingTime)} seconds."); 
            return;
        }

        // Set the next time the button can be pressed
        nextActivationTime = Time.time + activationCooldown; 

        // Start the main coroutine that handles the whole sequence (fall then rise)
        // **IMPORTANT:** Since VRTrapActivator ensures this runs on ALL clients, 
        // the Coroutine will also run on all clients, causing stones to fall for everyone.
        StartCoroutine(ActivationSequence());
    }

    /// <summary>
    /// Coroutine to manage the staggered fall, wait period, and simultaneous rise sequence.
    /// </summary>
    private IEnumerator ActivationSequence()
    {
        isSequenceRunning = true;
        
        // 1. Get all stones and sort them by Z-coordinate (lowest Z first)
        // This script is on the parent, so we use GetComponentsInChildren directly.
        SteppingStone[] unsortedStones = GetComponentsInChildren<SteppingStone>();
        
        // Use LINQ to sort the stones based on their Z position.
        List<SteppingStone> sortedStones = unsortedStones
            .OrderBy(s => s.transform.position.z) 
            .ToList();
        
        if (sortedStones.Count == 0)
        {
            Debug.LogError("[VRSteppingStoneTrap] No SteppingStone scripts found in children!");
            isSequenceRunning = false;
            yield break;
        }

        // 2. Trigger the Staggered Fall
        Debug.Log("[VRSteppingStoneTrap] Starting staggered fall...");
        foreach (SteppingStone stone in sortedStones)
        {
            stone.ActivateTrap();
            yield return new WaitForSeconds(fallStaggerDelay); 
        }

        // 3. Wait for the Reset Time
        Debug.Log($"[VRSteppingStoneTrap] Stones are down. Waiting for {riseResetTime} seconds before reset...");
        yield return new WaitForSeconds(riseResetTime);

        // 4. Trigger the Simultaneous Rise
        Debug.Log("[VRSteppingStoneTrap] Stones rising simultaneously!");
        foreach (SteppingStone stone in sortedStones)
        {
            stone.ActivateStoneRise();
        }

        // Wait for the rise to complete
        // This relies on the SteppingStone script having a public Vector2 durationRange.
        float maxRiseDuration = sortedStones[0].durationRange.y; 
        yield return new WaitForSeconds(maxRiseDuration + 0.5f); 

        // Sequence complete, allow the button to be pressed again (subject to cooldown)
        isSequenceRunning = false;
        Debug.Log("[VRSteppingStoneTrap] Sequence finished. Cooldown active.");
    }
}