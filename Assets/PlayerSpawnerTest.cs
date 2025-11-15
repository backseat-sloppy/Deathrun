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
    [SerializeField] private GameObject PrefabToSpawnP; // Index 0: E.g., The PC Player Avatar
    [SerializeField] private GameObject PrefabToSpawnA; // Index 1: E.g., The AR Player Avatar

    private bool hasSpawnedAvatar = false;
    private GameObject[] spawnablePrefabs; // Array to hold the prefabs for easy lookup

    // Called when the NetworkObject is spawned (synced across the network).
    public override void OnNetworkSpawn()
    {
        // Initialize the array for easy lookup
        spawnablePrefabs = new GameObject[] { PrefabToSpawnP, PrefabToSpawnA };

        // 1. Initial Spawning (Runs only on the Owner's client)
        if (IsOwner)
        {
            Debug.Log($"Client {OwnerClientId} is now the owner of the Spawner.");
        }
    }

    // Recommended best practice is to always clean up the array
    public override void OnNetworkDespawn()
    {
        spawnablePrefabs = null;
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
            return;
        }

        // Check for 'P' key press (for PC/Player Avatar - Index 0)
        if (Input.GetKeyDown(KeyCode.P))
        {
            // Request the server to spawn the prefab at index 0
            RequestSpawnPrefabServerRpc(0);
        }

        // Check for 'A' key press (for AR/Avatar - Index 1)
        if (Input.GetKeyDown(KeyCode.A))
        {
            // Request the server to spawn the prefab at index 1
            RequestSpawnPrefabServerRpc(1);
        }
    }

    /// <summary>
    /// This is an RPC (Remote Procedure Call) executed only on the server.
    /// The client's input (in Update) triggers this.
    /// </summary>
    /// <param name="prefabIndex">The index (0 or 1) identifying the prefab to spawn.</param>
    [ServerRpc]
    private void RequestSpawnPrefabServerRpc(int prefabIndex)
    {
        // Input validation: Check if the index is valid for our array
        if (prefabIndex < 0 || prefabIndex >= spawnablePrefabs.Length)
        {
            Debug.LogError($"Server received invalid prefab index: {prefabIndex} from client {OwnerClientId}");
            return;
        }

        // Get the GameObject template from the local (server) array
        GameObject prefabToSpawn = spawnablePrefabs[prefabIndex];

        if (prefabToSpawn == null)
        {
            Debug.LogError($"Server prefab index {prefabIndex} is null. Check Inspector assignments.");
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
}