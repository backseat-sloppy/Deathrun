using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

namespace DeathrunGame
{
    /// <summary>
    /// Manages synchronized scene transitions for all players.
    /// Monitors a ready zone (BoxCollider trigger) and initiates scene change when all players are present.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class SynchronisedSceneChange : NetworkBehaviour
    {
        [Header("Scene Settings")]
        [SerializeField] private string gameSceneName = "GameScene";

        [Header("Ready Zone Settings")]
        [SerializeField] private float readyDelay = 5f;
        [Tooltip("UI Text to show countdown (optional)")]
        [SerializeField] private TMPro.TextMeshProUGUI countdownText;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // Track players in the ready zone
        private HashSet<ulong> playersInZone = new HashSet<ulong>();
        private bool isCountingDown = false;
        private Coroutine countdownCoroutine;

        // Network variable to sync countdown state
        private NetworkVariable<bool> isReadyCountdownActive = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<float> countdownTimer = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private BoxCollider readyZone;

        private void Awake()
        {
            readyZone = GetComponent<BoxCollider>();
            readyZone.isTrigger = true;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
                Log("🎮 Server: SynchronisedSceneChange initialized");
            }

            isReadyCountdownActive.OnValueChanged += OnCountdownStateChanged;
            countdownTimer.OnValueChanged += OnCountdownTimerChanged;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }

            isReadyCountdownActive.OnValueChanged -= OnCountdownStateChanged;
            countdownTimer.OnValueChanged -= OnCountdownTimerChanged;
        }

        private void OnClientConnected(ulong clientId)
        {
            Log($"🔌 Client {clientId} connected");
            CheckReadyStatus();
        }

        private void OnClientDisconnected(ulong clientId)
        {
            Log($"🔌 Client {clientId} disconnected");

            if (playersInZone.Contains(clientId))
            {
                playersInZone.Remove(clientId);
                Log($"👋 Client {clientId} removed from ready zone");
            }

            if (isCountingDown)
            {
                CancelCountdown();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;

            var networkObject = other.GetComponentInParent<NetworkObject>();
            if (networkObject != null && networkObject.IsPlayerObject)
            {
                ulong clientId = networkObject.OwnerClientId;

                if (playersInZone.Add(clientId))
                {
                    Log($"✅ Player {clientId} entered ready zone ({playersInZone.Count}/{GetConnectedPlayerCount()})");
                    CheckReadyStatus();
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsServer) return;

            var networkObject = other.GetComponentInParent<NetworkObject>();
            if (networkObject != null && networkObject.IsPlayerObject)
            {
                ulong clientId = networkObject.OwnerClientId;

                if (playersInZone.Remove(clientId))
                {
                    Log($"❌ Player {clientId} left ready zone ({playersInZone.Count}/{GetConnectedPlayerCount()})");

                    if (isCountingDown)
                    {
                        CancelCountdown();
                    }
                }
            }
        }

        private void CheckReadyStatus()
        {
            if (!IsServer) return;

            int connectedPlayers = GetConnectedPlayerCount();
            int playersReady = playersInZone.Count;

            if (connectedPlayers > 0 && playersReady == connectedPlayers && !isCountingDown)
            {
                Log($"🎉 All {connectedPlayers} players are ready! Starting countdown...");
                StartCountdown();
            }
        }

        private int GetConnectedPlayerCount()
        {
            return NetworkManager.Singleton.ConnectedClientsList.Count;
        }

        private void StartCountdown()
        {
            if (isCountingDown) return;

            isCountingDown = true;
            isReadyCountdownActive.Value = true;
            countdownTimer.Value = readyDelay;

            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
            }

            countdownCoroutine = StartCoroutine(CountdownRoutine());
        }

        private void CancelCountdown()
        {
            if (!isCountingDown) return;

            Log("⏸️ Countdown cancelled - player left ready zone");

            isCountingDown = false;
            isReadyCountdownActive.Value = false;
            countdownTimer.Value = 0f;

            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
                countdownCoroutine = null;
            }

            NotifyCountdownCancelledClientRpc();
        }

        private IEnumerator CountdownRoutine()
        {
            float timeRemaining = readyDelay;

            while (timeRemaining > 0f)
            {
                countdownTimer.Value = timeRemaining;

                if (playersInZone.Count != GetConnectedPlayerCount())
                {
                    CancelCountdown();
                    yield break;
                }

                yield return new WaitForSeconds(0.1f);
                timeRemaining -= 0.1f;
            }

            countdownTimer.Value = 0f;
            Log("🚀 Countdown complete! Loading game scene...");
            LoadGameScene();
        }

        private void LoadGameScene()
        {
            if (!IsServer) return;

            isCountingDown = false;
            isReadyCountdownActive.Value = false;

            Log($"🎮 Loading scene: {gameSceneName}");
            
            // Just load the scene - repositioning is handled by PlayerRepositionManager in the new scene
            NetworkManager.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }

        [ClientRpc]
        private void NotifyCountdownCancelledClientRpc()
        {
            if (countdownText != null)
            {
                countdownText.text = "Player left ready zone - Countdown cancelled!";
                StartCoroutine(ClearCountdownTextAfterDelay(2f));
            }
        }

        private void OnCountdownStateChanged(bool previous, bool current)
        {
            if (current)
            {
                Log("⏰ Countdown started on client");
            }
            else
            {
                Log("⏹️ Countdown stopped on client");
            }
        }

        private void OnCountdownTimerChanged(float previous, float current)
        {
            if (countdownText != null && isReadyCountdownActive.Value)
            {
                int seconds = Mathf.CeilToInt(current);
                countdownText.text = $"Starting in {seconds}...";
            }
        }

        private IEnumerator ClearCountdownTextAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (countdownText != null)
            {
                countdownText.text = "";
            }
        }

        #region Debug Logging

        private void Log(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[SceneChange] {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[SceneChange] {message}");
        }

        #endregion

        #region Debug Gizmos

        private void OnDrawGizmos()
        {
            if (readyZone != null || TryGetComponent(out readyZone))
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(readyZone.center, readyZone.size);

                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(readyZone.center, readyZone.size);
            }
        }

        #endregion
    }
}