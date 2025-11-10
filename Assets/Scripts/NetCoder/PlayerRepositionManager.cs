using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

namespace DeathrunGame
{
    /// <summary>
    /// Manages player repositioning when the game scene loads.
    /// This script should be placed in the game scene (not the bootstrap scene).
    /// Automatically finds spawn points and repositions players when the scene starts.
    /// </summary>
    public class PlayerRepositionManager : NetworkBehaviour
    {
        [Header("Spawn Points")]
        [Tooltip("Leave empty to auto-find spawn points tagged 'SpawnPoint'")]
        [SerializeField] private Transform[] spawnPoints;

        [Header("Settings")]
        [SerializeField] private float repositionDelay = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        private bool hasRepositioned = false;

        private void Start()
        {
            // Only execute on server
            if (!IsServer) return;

            Log("🎬 PlayerRepositionManager initialized in game scene");

            // Find spawn points if not manually assigned
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                FindSpawnPoints();
            }

            // Start repositioning players
            StartCoroutine(RepositionPlayersCoroutine());
        }

        private IEnumerator RepositionPlayersCoroutine()
        {
            // Wait for scene to fully initialize and players to spawn
            yield return new WaitForSeconds(repositionDelay);

            if (hasRepositioned)
            {
                Log("⚠️ Already repositioned players, skipping");
                yield break;
            }

            RepositionAllPlayers();
            hasRepositioned = true;
        }

        private void FindSpawnPoints()
        {
            Log("🔍 Searching for spawn points with 'SpawnPoint' tag...");

            GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");

            if (spawnPointObjects.Length > 0)
            {
                spawnPoints = new Transform[spawnPointObjects.Length];
                for (int i = 0; i < spawnPointObjects.Length; i++)
                {
                    spawnPoints[i] = spawnPointObjects[i].transform;
                    Log($"   ✅ Found spawn point {i}: {spawnPointObjects[i].name} at {spawnPointObjects[i].transform.position}");
                }

                Log($"✅ Found {spawnPoints.Length} spawn points");
            }
            else
            {
                LogWarning("⚠️ NO spawn points found! Make sure they are tagged as 'SpawnPoint'");
                LogWarning("   Creating fallback spawn point at (0, 2, 0)");

                // Create fallback spawn point
                GameObject fallbackSpawn = new GameObject("FallbackSpawnPoint");
                fallbackSpawn.transform.position = Vector3.up * 2f;
                spawnPoints = new Transform[] { fallbackSpawn.transform };
            }
        }

        private void RepositionAllPlayers()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                LogWarning("⚠️ No spawn points available! Cannot reposition players.");
                return;
            }

            Log($"👥 Repositioning {NetworkManager.Singleton.ConnectedClientsList.Count} players...");

            int spawnIndex = 0;

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject != null)
                {
                    Vector3 spawnPosition = spawnPoints[spawnIndex % spawnPoints.Length].position;
                    Quaternion spawnRotation = spawnPoints[spawnIndex % spawnPoints.Length].rotation;

                    Log($"📍 Moving player {client.ClientId} to spawn point {spawnIndex}");
                    Log($"   Position: {spawnPosition}");

                    // Move player on server
                    MovePlayer(client.PlayerObject, spawnPosition, spawnRotation);

                    // Notify client to sync
                    SyncPlayerPositionClientRpc(spawnPosition, spawnRotation, client.ClientId);

                    spawnIndex++;
                }
                else
                {
                    LogWarning($"⚠️ Client {client.ClientId} has no PlayerObject!");
                }
            }

            Log("✅ All players repositioned successfully!");
        }

        private void MovePlayer(NetworkObject playerObject, Vector3 position, Quaternion rotation)
        {
            // Handle CharacterController if present
            var characterController = playerObject.GetComponent<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = false;
                playerObject.transform.SetPositionAndRotation(position, rotation);
                characterController.enabled = true;
                Log($"   ✅ Server moved player with CharacterController");
            }
            else
            {
                // No CharacterController, just move directly
                playerObject.transform.SetPositionAndRotation(position, rotation);
                Log($"   ✅ Server moved player without CharacterController");
            }
        }

        [ClientRpc]
        private void SyncPlayerPositionClientRpc(Vector3 position, Quaternion rotation, ulong targetClientId)
        {
            // Only execute on the target client
            if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

            Log($"📥 Client {targetClientId} syncing position to {position}");

            if (NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                GameObject player = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;

                var characterController = player.GetComponent<CharacterController>();
                if (characterController != null)
                {
                    characterController.enabled = false;
                    player.transform.SetPositionAndRotation(position, rotation);
                    characterController.enabled = true;
                }
                else
                {
                    player.transform.SetPositionAndRotation(position, rotation);
                }

                Log($"✨ Client repositioned successfully");
            }
        }

        #region Debug Logging

        private void Log(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[PlayerReposition] {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[PlayerReposition] {message}");
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            if (spawnPoints == null) return;

            // Draw spawn points
            foreach (var spawnPoint in spawnPoints)
            {
                if (spawnPoint != null)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
                    Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + spawnPoint.forward * 2f);
                }
            }
        }

        #endregion
    }
}