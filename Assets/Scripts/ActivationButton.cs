using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Keep System.Linq for the OrderBy function

/// <summary>
/// Controls the activation sequence (fall and rise) of stepping stones 
/// when triggered by an object with the "Hand" tag.
/// </summary>
public class ActivationButton : MonoBehaviour // Renamed: ActivatonButton -> ActivationButton
{
    [Tooltip("The parent object containing all the stepping stone boxes.")]
    public Transform steppingStonesParent; 
    
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

    // --- TRIGGER MECHANISM ---

    /// <summary>
    /// Checks for a trigger event and calls OnPlayerActivate if the tag is "Hand".
    /// This requires a Collider component on this GameObject set to 'Is Trigger' = true.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Check if the entering object has the specified tag ("Hand")
        if (other.CompareTag("Hand")) 
        {
            OnPlayerActivate();
        }
    }

    // --- ACTIVATION LOGIC ---

    /// <summary>
    /// Initiates the stepping stone sequence, checking for running status and cooldown.
    /// </summary>
    public void OnPlayerActivate()
    {
        // 1. Check if the button is currently running the sequence
        if (isSequenceRunning)
        {
            Debug.Log("Button is already processing the sequence. Please wait.");
            return;
        }

        // 2. Check the Cooldown Time
        if (Time.time < nextActivationTime)
        {
            float remainingTime = nextActivationTime - Time.time;
            // Use Math.Ceiling to show a cleaner integer for the wait time
            Debug.Log($"Button on cooldown. Wait {Mathf.Ceil(remainingTime)} seconds."); 
            return;
        }

        // Set the next time the button can be pressed
        nextActivationTime = Time.time + activationCooldown; 

        // Start the main coroutine that handles the whole sequence (fall then rise)
        StartCoroutine(ActivationSequence());
    }

    /// <summary>
    /// Coroutine to manage the staggered fall, wait period, and simultaneous rise sequence.
    /// </summary>
    private IEnumerator ActivationSequence()
    {
        isSequenceRunning = true;
        
        // 1. Get all stones and sort them by Z-coordinate (lowest Z first)
        // Ensure SteppingStone script is attached to each child object
        SteppingStone[] unsortedStones = steppingStonesParent.GetComponentsInChildren<SteppingStone>();
        
        // Use LINQ to sort the stones based on their Z position.
        // This is crucial for the "staggered fall" effect across a path.
        List<SteppingStone> sortedStones = unsortedStones
            .OrderBy(s => s.transform.position.z) 
            .ToList();
        
        if (sortedStones.Count == 0)
        {
            Debug.LogError("No SteppingStone scripts found in children of the parent!");
            isSequenceRunning = false;
            yield break;
        }

        // 2. Trigger the Staggered Fall (from lowest Z to highest Z)
        Debug.Log("Starting staggered fall...");
        foreach (SteppingStone stone in sortedStones)
        {
            stone.ActivateStoneFall();
            yield return new WaitForSeconds(fallStaggerDelay); 
        }

        // 3. Wait for the Reset Time
        Debug.Log($"Stones are down. Waiting for {riseResetTime} seconds before reset...");
        yield return new WaitForSeconds(riseResetTime);

        // 4. Trigger the Simultaneous Rise
        Debug.Log("Stones rising simultaneously!");
        foreach (SteppingStone stone in sortedStones)
        {
            stone.ActivateStoneRise();
        }

        // Wait for the rise to complete before the sequence is declared finished.
        // The time is based on the stone's rise duration (assuming durationRange is a Vector2 min/max).
        // It uses the max duration (y) of the first stone as a proxy.
        // **NOTE: Ensure your SteppingStone script has a public Vector2 durationRange.**
        float maxRiseDuration = sortedStones[0].durationRange.y; 
        yield return new WaitForSeconds(maxRiseDuration + 0.5f); // Added 0.5s buffer

        // Sequence complete, allow the button to be pressed again (subject to cooldown)
        isSequenceRunning = false;
        Debug.Log("Sequence finished. Cooldown active.");
    }
}