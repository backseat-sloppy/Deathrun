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

    [Header("Spawn Settings")]
    [SerializeField] private string pcSpawnPointTag = "PCSpawn";
    [SerializeField] private string arSpawnPointTag = "ARSpawn";
    [SerializeField] private bool removeSpawnPointAfterUse = true;

    [Header("Auto Spawn")]
    [SerializeField] private float autoSpawnDelay = 10f;

    private bool hasSpawnedAvatar = false;
    private float autoSpawnTimer = 0f;
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
            // Request the server to spawn the prefab at index 0 (PC)
            RequestSpawnPrefabServerRpc(0, false); // false = PC Runner
            return; // Exit to prevent auto-spawn
        }

        // Auto-spawn AR prefab after delay if no input received
        autoSpawnTimer += Time.deltaTime;

        if (autoSpawnTimer >= autoSpawnDelay)
        {
            // Request the server to spawn the prefab at index 1 (AR)
            RequestSpawnPrefabServerRpc(1, true); // true = AR Director
        }
    }

    /// <summary>
    /// This is an RPC (Remote Procedure Call) executed only on the server.
    /// The client's input (in Update) triggers this.
    /// </summary>
    /// <param name="prefabIndex">The index (0 or 1) identifying the prefab to spawn.</param>
    /// <param name="isARRole">Whether this is an AR Director (true) or PC Runner (false)</param>
    [ServerRpc]
    private void RequestSpawnPrefabServerRpc(int prefabIndex, bool isARRole)
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

        // Determine which spawn point tag to use based on role
        string spawnTag = isARRole ? arSpawnPointTag : pcSpawnPointTag;

        // Find an available spawn point with the role-specific tag
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag(spawnTag);

        Vector3 spawnPosition;
        Quaternion spawnRotation;
        GameObject selectedSpawnPoint = null;

        if (spawnPoints.Length > 0)
        {
            // Pick the first available spawn point
            selectedSpawnPoint = spawnPoints[0];
            spawnPosition = selectedSpawnPoint.transform.position;
            spawnRotation = selectedSpawnPoint.transform.rotation;

            Debug.Log($"✅ Found {(isARRole ? "AR" : "PC")} spawn point '{selectedSpawnPoint.name}' at {spawnPosition}");
        }
        else
        {
            // Fallback if no spawn points exist for this role
            Debug.LogWarning($"⚠️ No spawn points found with tag '{spawnTag}'! Using fallback position.");

            // Use different fallback positions for each role
            if (isARRole)
            {
                spawnPosition = transform.position + new Vector3(5f, 0.5f, 0f); // AR spawns 5 units to the right
            }
            else
            {
                spawnPosition = transform.position + Vector3.up * 0.5f; // PC spawns at center
            }

            spawnRotation = Quaternion.identity;
        }
            
        // 3. Server Instantiation at the spawn point
        GameObject spawnedObject = Instantiate(prefabToSpawn, spawnPosition, spawnRotation);

        // 4. Critical Step: Spawn the object and assign ownership.
        NetworkObject netObj = spawnedObject.GetComponent<NetworkObject>();

        // This line makes the client who pressed 'P' or auto-spawned the owner of the new prefab.
        netObj.SpawnWithOwnership(OwnerClientId);

        Debug.Log($"🎮 Server spawned '{prefabToSpawn.name}' ({(isARRole ? "AR" : "PC")}) for client {OwnerClientId} at {spawnPosition}");

        // 5. Remove the spawn point after use if enabled
        if (removeSpawnPointAfterUse && selectedSpawnPoint != null)
        {
            Debug.Log($"🗑️ Removing {(isARRole ? "AR" : "PC")} spawn point '{selectedSpawnPoint.name}' after use");
            Destroy(selectedSpawnPoint);
        }

        // Mark that an avatar has been successfully spawned
        hasSpawnedAvatar = true;
    }
}