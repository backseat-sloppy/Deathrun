using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Attached to the Goblin player prefab. Manages input, visuals, and end state.
/// </summary>
public class GoblinPlayerController : NetworkBehaviour
{
    // Assign this in the Inspector: A Canvas or Panel containing the end screen text/buttons
    [Tooltip("The UI element to show when the player reaches the goal.")]
    public GameObject endScreenUI; 

    // Components to disable
    private CharacterController characterController; // Or similar movement component
    private MeshRenderer meshRenderer;               // Or SkinnedMeshRenderer for visuals

    public override void OnNetworkSpawn()
    {
        // Initialization
        if (endScreenUI != null)
        {
            // Ensure the end screen is hidden when the player spawns
            endScreenUI.SetActive(false); 
        }

        // Get necessary components
        characterController = GetComponent<CharacterController>();
        meshRenderer = GetComponentInChildren<MeshRenderer>(); 
    }

    /// <summary>
    /// Called via ClientRpc when this specific player reaches the goal.
    /// This runs ONLY on the local client (since IsLocalPlayer is checked implicitly).
    /// </summary>
    public void HandleGoalReached()
    {
        if (!IsLocalPlayer) return; // Safety check

        // 1. Disable Movement/Interaction
        if (characterController != null)
        {
            characterController.enabled = false;
        }
        // Disable any input handling script you might have
        // this.GetComponent<PlayerInputScript>().enabled = false; 

        // 2. Hide the Player Object (Visuals)
        // Note: The physical collider remains to prevent other players from falling through
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }

        // 3. Show the Local End Screen UI
        if (endScreenUI != null)
        {
            endScreenUI.SetActive(true);
        }
        
        // Optional: Notify the server that the player finished, if needed for a final scoreboard
        // SubmitFinishedTimeServerRpc(Time.time); 
    }
}