using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;

namespace DeathrunGame
{
    /// <summary>
    /// Manages synchronized scene transitions for all players.
    /// The host/server can initiate a scene change by holding 'R' for 1 second.
    /// </summary>
    public class SynchronisedSceneChange : NetworkBehaviour
    {
        [Header("Scene Settings")]
        [SerializeField] private string gameSceneName = "GameScene";

        [Header("Hold-to-Start Settings")]
        [SerializeField] private float holdDuration = 1f;
        [SerializeField] private KeyCode startKey = KeyCode.R;
        [Tooltip("UI Text to show countdown (optional)")]
        [SerializeField] private TMPro.TextMeshProUGUI countdownText;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        private float holdTimer = 0f;
        private bool isHoldingKey = false;
        private bool isTransitioning = false;

        // Network variable to sync countdown state
        private NetworkVariable<bool> isCountdownActive = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<float> countdownProgress = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public override void OnNetworkSpawn()
        {
            isCountdownActive.OnValueChanged += OnCountdownStateChanged;
            countdownProgress.OnValueChanged += OnCountdownProgressChanged;

            if (IsServer)
            {
                Log("🎮 Server: SynchronisedSceneChange initialized (Hold 'R' to start)");
            }
        }

        public override void OnNetworkDespawn()
        {
            isCountdownActive.OnValueChanged -= OnCountdownStateChanged;
            countdownProgress.OnValueChanged -= OnCountdownProgressChanged;
        }

        private void Update()
        {
            // Only the server/host can initiate scene changes
            if (!IsServer)
            {
                return;
            }

            // Prevent interaction if already transitioning
            if (isTransitioning)
            {
                return;
            }

            // Check if the key is being held down
            if (Input.GetKey(startKey))
            {
                if (!isHoldingKey)
                {
                    isHoldingKey = true;
                    holdTimer = 0f;
                    isCountdownActive.Value = true;
                    Log($"🎮 Host started holding '{startKey}' key");
                }

                holdTimer += Time.deltaTime;
                countdownProgress.Value = Mathf.Clamp01(holdTimer / holdDuration);

                // Check if held long enough
                if (holdTimer >= holdDuration)
                {
                    InitiateSceneChange();
                }
            }
            else if (isHoldingKey)
            {
                // Key was released before duration completed
                CancelHold();
            }
        }

        private void InitiateSceneChange()
        {
            if (isTransitioning) return;

            isTransitioning = true;
            isCountdownActive.Value = false;
            countdownProgress.Value = 1f;

            Log($"🚀 Host completed hold! Loading scene: {gameSceneName}");
            LoadGameScene();
        }

        private void CancelHold()
        {
            isHoldingKey = false;
            holdTimer = 0f;
            isCountdownActive.Value = false;
            countdownProgress.Value = 0f;

            Log("⏸️ Hold cancelled - key released");
            NotifyHoldCancelledClientRpc();
        }

        private void LoadGameScene()
        {
            if (!IsServer) return;

            Log($"🎮 Loading scene: {gameSceneName}");
            
            // Load the scene - repositioning is handled by PlayerRepositionManager in the new scene
            NetworkManager.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }

        [ClientRpc]
        private void NotifyHoldCancelledClientRpc()
        {
            if (countdownText != null)
            {
                countdownText.text = "Host released key - Scene change cancelled!";
                StartCoroutine(ClearCountdownTextAfterDelay(2f));
            }
        }

        private void OnCountdownStateChanged(bool previous, bool current)
        {
            if (current)
            {
                Log("⏰ Host started holding key");
            }
            else
            {
                Log("⏹️ Hold stopped");
            }
        }

        private void OnCountdownProgressChanged(float previous, float current)
        {
            if (countdownText != null && isCountdownActive.Value)
            {
                float timeRemaining = holdDuration * (1f - current);
                countdownText.text = $"Host loading scene: {timeRemaining:F1}s";
            }
            else if (countdownText != null && !isCountdownActive.Value && current > 0f && current < 1f)
            {
                // Hold was cancelled mid-way
                countdownText.text = "";
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
    }
}