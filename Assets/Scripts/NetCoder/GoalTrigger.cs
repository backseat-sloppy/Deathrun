using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// Attached to the Goal Trigger volume. Detects players (tagged "Goblin")
/// reaching the end and triggers a client-specific end state.
/// </summary>
public class GoalTrigger : NetworkBehaviour
{
    [Tooltip("The tag of the objects (players/goblins) that should trigger the end screen.")]
    [SerializeField]
    private string playerTag = "Player";
    
    private void OnTriggerEnter(Collider other)
    {
        // 1. Check the tag to ensure only Goblins trigger the goal
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        // 2. Get the NetworkObject associated with the Goblin
        NetworkObject goblinNetworkObject = other.GetComponentInParent<NetworkObject>();
        
        if (goblinNetworkObject == null)
        {
            Debug.LogWarning($"GoalTrigger detected a '{playerTag}' object without a NetworkObject component. Ignoring.");
            return;
        }

        // 3. Ensure the server handles the RPC command
       // 3. Ensure the server handles the RPC command
        if (IsServer)
        {
            Debug.Log($"Goblin (Client ID: {goblinNetworkObject.OwnerClientId}) reached the goal. Sending end command.");
            
            // --- FINAL FIX FOR CS1061: Use explicit struct assignment ---
            // This method is the most compatible workaround for older/different NGO versions.
            ClientRpcParams clientRpcParams = new ClientRpcParams();
            clientRpcParams.Send.TargetClientIds = new List<ulong> { goblinNetworkObject.OwnerClientId };
            // Note the use of '.Send' and 'List<ulong>' instead of 'ulong[]'.
            
            // Call the ClientRpc method
            LoadPlayerEndStateClientRpc(goblinNetworkObject.NetworkObjectId, clientRpcParams);
        }
    }

    /// <summary>
    /// RPC method called by the Server/Host, executed ONLY on the target client.
    /// </summary>
    [ClientRpc]
    private void LoadPlayerEndStateClientRpc(ulong networkObjectId, ClientRpcParams clientRpcParams = default)
    {
        // This code executes on the client whose Goblin hit the trigger.
        
        // --- FIX FOR CS1061: Using TryGetValue on SpawnedObjects dictionary ---
        NetworkObject playerNetObject = null;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject spawnedObject))
        {
            playerNetObject = spawnedObject;
        }

        // Check for the found object and ensure it is the local player object
        if (playerNetObject != null && playerNetObject.IsLocalPlayer)
        {
            // Get the controller component on that player
            GoblinPlayerController playerController = playerNetObject.GetComponent<GoblinPlayerController>();
            
            if (playerController != null)
            {
                // Call the function that handles disabling and UI locally.
                playerController.HandleGoalReached();
                Debug.Log("Local player has reached the goal and is now disabled.");
            }
        }
    }
}