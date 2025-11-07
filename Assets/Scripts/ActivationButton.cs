using UnityEngine;
using System.Collections.Generic; // Required for List

public class ActivationButton : MonoBehaviour
{
    [Tooltip("The parent object containing all the stepping stone boxes.")]
    public Transform steppingStonesParent; 

    // Used to prevent accidental multiple activations
    private bool isActivated = false; 

    // Function called when the player interacts with the button
    // (You will need to connect this to your player's input/trigger logic)
    public void OnPlayerActivate()
    {
        if (isActivated)
        {
            Debug.Log("Stepping stones already activated.");
            return;
        }

        // Get all SteppingStone components from children of the parent object
        SteppingStone[] stones = steppingStonesParent.GetComponentsInChildren<SteppingStone>();
        
        if (stones.Length == 0)
        {
            Debug.LogError("No SteppingStone scripts found on children of the parent object!");
            return;
        }

        Debug.Log($"Activating {stones.Length} stepping stones!");

        foreach (SteppingStone stone in stones)
        {
            // Call the public method on each stone to start its rising coroutine
            stone.ActivateStoneRise(); 
        }

        isActivated = true;
    }

    // Example of how to connect this to a simple trigger collider:
    /*
    private void OnTriggerEnter(Collider other)
    {
        // Check for your player's tag or component
        if (other.CompareTag("Player")) 
        {
            OnPlayerActivate();
        }
    }
    */
}