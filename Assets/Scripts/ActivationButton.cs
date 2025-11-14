using System.Collections;
using System.Collections.Generic;
using System.Linq; 
using UnityEngine;

public class ActivatonButton : MonoBehaviour
{
    [Tooltip("The parent object containing all the stepping stone boxes.")]
    public Transform steppingStonesParent; 
    
    [Header("Timing Settings")]
    [Tooltip("Time delay between each stone's fall activation.")]
    public float fallStaggerDelay = 0.2f; 

    [Tooltip("Time in seconds before all stones rise back up after the sequence finishes.")]
    public float riseResetTime = 5.0f; 

    // --- NEW COOLDOWN LOGIC ---
    [Header("Cooldown Settings")]
    [Tooltip("Time in seconds the player must wait after activation before pressing the button again.")]
    public float activationCooldown = 10.0f; 

    private float nextActivationTime = 0f;
    private bool isSequenceRunning = false; 
    // --------------------------

    public void OnPlayerActivate()
    {
        // 1. Check if the button is currently in a sequence (already falling/rising)
        if (isSequenceRunning)
        {
            Debug.Log("Button is already processing the sequence.");
            return;
        }

        // 2. Check the Cooldown Time
        if (Time.time < nextActivationTime)
        {
            float remainingTime = nextActivationTime - Time.time;
            Debug.Log($"Button on cooldown. Wait {remainingTime:F1} seconds.");
            return;
        }

        // Set the next time the button can be pressed
        nextActivationTime = Time.time + activationCooldown; 

        // Start the main coroutine that handles the whole sequence (fall then rise)
        StartCoroutine(ActivationSequence());
    }

    private IEnumerator ActivationSequence()
    {
        isSequenceRunning = true;
        
        // 1. Get all stones and sort them by Z-coordinate (lowest Z first)
        SteppingStone[] unsortedStones = steppingStonesParent.GetComponentsInChildren<SteppingStone>();
        
        // Use LINQ to sort the stones based on their Z position
        List<SteppingStone> sortedStones = unsortedStones
            .OrderBy(s => s.transform.position.z) 
            .ToList();
        
        if (sortedStones.Count == 0)
        {
            Debug.LogError("No SteppingStone scripts found!");
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

        // 3. Wait for the Reset Time (5 seconds)
        Debug.Log($"Stones are down. Waiting for {riseResetTime} seconds before reset...");
        yield return new WaitForSeconds(riseResetTime);

        // 4. Trigger the Simultaneous Rise
        Debug.Log("Stones rising simultaneously!");
        foreach (SteppingStone stone in sortedStones)
        {
            stone.ActivateStoneRise();
        }

        // Wait for the rise to complete before the sequence is declared finished
        float maxRiseDuration = sortedStones[0].durationRange.y; 
        yield return new WaitForSeconds(maxRiseDuration + 0.5f);

        // Sequence complete, ready for cooldown check to lift
        isSequenceRunning = false;
    }

    // Example trigger interaction (add a Collider set to Is Trigger and your Player tag)
    /*
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            OnPlayerActivate();
        }
    }
    */
}