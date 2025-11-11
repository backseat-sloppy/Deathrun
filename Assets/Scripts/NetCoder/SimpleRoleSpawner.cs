using Unity.Netcode;
using UnityEngine;

namespace DeathrunGame
{
    /// <summary>
    /// SIMPLE version: Uses Netcode's built-in PlayerPrefabHash system.
    /// No manual spawning needed!
    /// </summary>
    public class SimpleRoleSpawner : MonoBehaviour
    {
        [Header("Prefab References")]
        [Tooltip("These should match the order in NetworkManager's Default Network Prefabs list")]
        [SerializeField] private GameObject goblinModePrefab;
        [SerializeField] private GameObject ovrCameraRigPrefab;

        private void Start()
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        }

        private void ApprovalCheck(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            // Approve the connection
            response.Approved = true;
            
            // Let Netcode spawn the player automatically
            response.CreatePlayerObject = true;
            
            // Get role from LobbyManager
            var role = GetRoleForClient(request.ClientNetworkId);
            
            // Get the correct prefab based on role
            GameObject prefabToSpawn = role == LobbyManager.PlayerRole.ARDirector 
                ? ovrCameraRigPrefab 
                : goblinModePrefab;

            if (prefabToSpawn != null)
            {
                // Get the prefab's NetworkObject hash
                var networkObject = prefabToSpawn.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    // Use PrefabHash or PrefabIdHash (depending on your Netcode version)
                    response.PlayerPrefabHash = networkObject.PrefabIdHash;
                    Debug.Log($"✅ Spawning {role} (hash: {networkObject.PrefabIdHash}) for client {request.ClientNetworkId}");
                }
                else
                {
                    Debug.LogError($"❌ Prefab {prefabToSpawn.name} has no NetworkObject component!");
                }
            }
            else
            {
                Debug.LogError($"❌ Prefab reference is null for role {role}!");
            }
            
            response.Pending = false;
        }

        private LobbyManager.PlayerRole GetRoleForClient(ulong clientId)
        {
            // Check if LobbyManager instance exists
            if (LobbyManager.Instance == null)
            {
                Debug.LogWarning("⚠️ LobbyManager.Instance is null, defaulting to PCRunner");
                return LobbyManager.PlayerRole.PCRunner;
            }

            return LobbyManager.Instance.GetMyRole();
        }
    }
}