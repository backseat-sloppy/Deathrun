using Unity.Netcode;
using UnityEngine;

/// <summary>
/// This script is attached to the main 'GoblinMode' Player Prefab, which is 
/// the default prefab spawned by the NetworkManager. It handles the dynamic 
/// spawning of secondary, role-specific prefabs and ensures correct ownership.
/// </summary>
public class PlayerSpawnerTest : NetworkBehaviour
{
    // Assign these prefabs in the Inspector. 
    // IMPORTANT: Both must have a NetworkObject component and be registered in 
    // the NetworkManager's Network Prefabs List.
    [Header("Spawnable Prefabs")]
    [SerializeField] private GameObject PrefabToSpawnP; // E.g., The PC Player Avatar
    [SerializeField] private GameObject PrefabToSpawnA; // E.g., The AR Player Avatar

    private bool hasSpawnedAvatar = false;

    // Called when the NetworkObject is spawned (synced across the network).
    public override void OnNetworkSpawn()
    {
        // 1. Initial Spawning (Runs only on the Owner's client)
        // We only want the owner to see the input check.
        if (IsOwner)
        {
            Debug.Log($"Client {OwnerClientId} is now the owner of the Spawner.");
        }

        // Only allow input to be processed if the current object belongs to the local player.
        // We do not return here because we still need the Update() to run for IsOwner check.
    }

    private void Update()
    {
        // 2. Ownership Check: Only the client who owns this object should process its input.
        if (!IsOwner)
        {
            return;
        }

        // Prevent spawning multiple times
        if (hasSpawnedAvatar)
        {
            // You could add logic here for respawn or replacement if needed.
            return;
        }

        // Check for 'P' key press (for PC/Player Avatar)
        if (Input.GetKeyDown(KeyCode.P))
        {
            // Request the server to spawn the prefab
            RequestSpawnPrefabServerRpc(PrefabToSpawnP.GetComponent<NetworkObject>().PrefabHash);
        }

        // Check for 'A' key press (for AR/Avatar)
        if (Input.GetKeyDown(KeyCode.A))
        {
            // Request the server to spawn the prefab
            RequestSpawnPrefabServerRpc(PrefabToSpawnA.GetComponent<NetworkObject>().PrefabHash);
        }
    }

    /// <summary>
    /// This is an RPC (Remote Procedure Call) executed only on the server.
    /// The client's input (in Update) triggers this.
    /// </summary>
    /// <param name="prefabHash">The NetworkObject hash of the prefab to spawn.</param>
    [ServerRpc]
    private void RequestSpawnPrefabServerRpc(uint prefabHash)
    {
        // Find the GameObject template based on the hash provided by the client
        GameObject prefabToSpawn = NetworkManager.Singleton.PrefabHandler.GetPrefab(prefabHash);

        if (prefabToSpawn == null)
        {
            Debug.LogError($"Server could not find prefab with hash: {prefabHash}. Is it registered?");
            return;
        }

        // 3. Server Instantiation
        GameObject spawnedObject = Instantiate(prefabToSpawn, transform.position + Vector3.up * 0.5f, Quaternion.identity);

        // 4. Critical Step: Spawn the object and assign ownership.
        // The OwnerClientId is the ID of the client who owns THIS GoblinMode object.
        NetworkObject netObj = spawnedObject.GetComponent<NetworkObject>();

        // This line makes the client who pressed 'P' or 'A' the owner of the new prefab.
        netObj.SpawnWithOwnership(OwnerClientId);

        Debug.Log($"Server spawned '{prefabToSpawn.name}' and assigned ownership to Client ID: {OwnerClientId}");

        // Mark that an avatar has been successfully spawned
        hasSpawnedAvatar = true;
    }

    /// <summary>
    /// Utility function to get a GameObject reference from its hash (for logging/debugging).
    /// </summary>
    private GameObject GetPrefabFromHash(uint hash)
    {
        if (NetworkManager.Singleton.PrefabHandler.TryGetPrefab(hash, out GameObject prefab))
        {
            return prefab;
        }
        return null;
    }
}