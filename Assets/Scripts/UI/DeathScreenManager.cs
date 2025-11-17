using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the death screen UI - shows when player dies.
/// Local only (not networked), each player sees their own death screen.
/// </summary>
public class DeathScreenManager : MonoBehaviour
{
    #region Serialized Fields

    [Header("Death Screen Panel")]
    [SerializeField] private GameObject deathScreenPanel;

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI deathMessageText;
        [SerializeField] private Button respawnButton;
        [SerializeField] private Button quitButton;

        [Header("Messages")]
        [SerializeField] private string[] deathMessages = new string[]
        {
            "You Died!",
            "Wasted!",
            "Game Over",
            "Try Again!",
            "Better Luck Next Time",
            "Ouch! That Hurt!",
            "You Failed!",
            "Mission Failed"
        };

        [Header("Settings")]
        [SerializeField] private bool showCursorOnDeath = true;
        [SerializeField] private float autoRespawnDelay = 0f; // 0 = disabled

        #endregion

        #region Private Variables

        private bool isDeathScreenActive = false;
        private float deathTimer = 0f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Ensure death screen is hidden on start
            if (deathScreenPanel != null)
                deathScreenPanel.SetActive(false);
        }

        private void Start()
        {
            SetupButtons();
        }

        private void Update()
        {
            // Handle auto-respawn timer
            if (isDeathScreenActive && autoRespawnDelay > 0f)
            {
                deathTimer += Time.deltaTime;
                if (deathTimer >= autoRespawnDelay)
                {
                    OnRespawn();
                }
            }
        }

        #endregion

        #region Setup

        private void SetupButtons()
        {
            if (respawnButton != null)
                respawnButton.onClick.AddListener(OnRespawn);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuit);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the death screen with a random death message.
        /// </summary>
        public void ShowDeathScreen()
        {
            ShowDeathScreen(GetRandomDeathMessage());
        }

        /// <summary>
        /// Shows the death screen with a custom message.
        /// </summary>
        public void ShowDeathScreen(string message)
        {
            if (deathScreenPanel == null) return;

            isDeathScreenActive = true;
            deathTimer = 0f;

            // Set death message
            if (deathMessageText != null)
            {
                deathMessageText.text = message;
            }

            // Show panel
            deathScreenPanel.SetActive(true);

            // Pause time (optional - you can disable this if you want game to continue)
            // Time.timeScale = 0f;

            // Show cursor
            if (showCursorOnDeath)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Play death sound
            if (UIAudioManager.Exists())
            {
                UIAudioManager.Instance.PlayBack(); // Or create a death sound
            }

            Debug.Log("💀 Death screen shown");
        }

        /// <summary>
        /// Hides the death screen.
        /// </summary>
        public void HideDeathScreen()
        {
            if (deathScreenPanel == null) return;

            isDeathScreenActive = false;

            // Hide panel
            deathScreenPanel.SetActive(false);

            // Resume time if it was paused
            Time.timeScale = 1f;

            // Hide cursor (player camera controller will handle this)
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Debug.Log("✅ Death screen hidden");
        }

        /// <summary>
        /// Check if death screen is currently active.
        /// </summary>
        public bool IsActive()
        {
            return isDeathScreenActive;
        }

        #endregion

        #region Button Callbacks

        private void OnRespawn()
        {
            // Play click sound
            if (UIAudioManager.Exists())
            {
                UIAudioManager.Instance.PlayClick();
            }

            // Find the player and call respawn
            var playerDeath = FindObjectOfType<PlayerDeathOnImpact>();
            if (playerDeath != null)
            {
                playerDeath.Respawn();
            }

            HideDeathScreen();
        }

        private void OnQuit()
        {
            // Play click sound
            if (UIAudioManager.Exists())
            {
                UIAudioManager.Instance.PlayClick();
            }

            // Resume time before quitting
            Time.timeScale = 1f;

            // TODO: Implement proper quit to menu
            Debug.Log("🚪 Quitting to main menu...");
            
            // Example: UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        #endregion

        #region Helper Methods

        private string GetRandomDeathMessage()
        {
            if (deathMessages == null || deathMessages.Length == 0)
                return "You Died!";

            int randomIndex = Random.Range(0, deathMessages.Length);
            return deathMessages[randomIndex];
        }

        #endregion
    }
