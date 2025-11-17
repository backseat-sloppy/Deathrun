using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DeathrunGame
{
    /// <summary>
    /// Manages the pause menu - local only (not networked).
    /// Handles pausing, resuming, and player-specific settings.
    /// Press ESC to toggle pause menu.
    /// </summary>
    public class PauseMenuManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Pause Menu Panel")]
        [SerializeField] private GameObject pauseMenuPanel;

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitToMenuButton;

        [Header("Settings Panel (Optional)")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button closeSettingsButton;

        [Header("Settings - Audio")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider playerVolumeSlider;
        [SerializeField] private TextMeshProUGUI playerVolumeValueText;

        [Header("Settings")]
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
        [SerializeField] private bool disablePauseInMenu = true;

        #endregion

        #region Private Variables

        private bool isPaused = false;
        private bool isInMainMenu = true; // Assume we start in main menu

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Ensure pause menu is hidden on start
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);

            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        private void Start()
        {
            SetupButtons();
            LoadSettings();
            
            // Try to detect if we're in main menu by checking for LobbyUIManager
            isInMainMenu = FindObjectOfType<LobbyUIManager>() != null;
        }

        private void Update()
        {
            // Check for pause input
            if (Input.GetKeyDown(pauseKey))
            {
                // Don't allow pausing in main menu if disabled
                if (disablePauseInMenu && isInMainMenu)
                    return;

                TogglePause();
            }
        }

        private void OnDestroy()
        {
            // Save settings when destroyed
            SaveSettings();
        }

        #endregion

        #region Setup

        private void SetupButtons()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResume);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnOpenSettings);

            if (quitToMenuButton != null)
                quitToMenuButton.onClick.AddListener(OnQuitToMenu);

            if (closeSettingsButton != null)
                closeSettingsButton.onClick.AddListener(OnCloseSettings);

            // Audio sliders
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

            if (playerVolumeSlider != null)
                playerVolumeSlider.onValueChanged.AddListener(OnPlayerVolumeChanged);
        }

        #endregion

        #region Pause/Resume

        /// <summary>
        /// Toggles between paused and unpaused state.
        /// </summary>
        public void TogglePause()
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }

        /// <summary>
        /// Pauses the game and shows the pause menu.
        /// </summary>
        public void Pause()
        {
            isPaused = true;
            Time.timeScale = 0f;

            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(true);

            // Set focus to Paused
            if (GamePhaseManager.Instance != null)
            {
                GamePhaseManager.Instance.SetPaused();
            }

            // Show cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Play pause sound
            if (UIAudioManager.Exists())
            {
                UIAudioManager.Instance.PlaySelect();
            }

            Debug.Log("⏸️ Game Paused");
        }

        /// <summary>
        /// Resumes the game and hides the pause menu.
        /// </summary>
        public void Resume()
        {
            isPaused = false;
            Time.timeScale = 1f;

            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);

            if (settingsPanel != null)
                settingsPanel.SetActive(false);

            // Set focus to Normal
            if (GamePhaseManager.Instance != null)
            {
                GamePhaseManager.Instance.SetNormal();
            }

            // Hide cursor (only if in game, not in menu)
            if (!isInMainMenu)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // Play resume sound
            if (UIAudioManager.Exists())
            {
                UIAudioManager.Instance.PlayBack();
            }

            Debug.Log("▶️ Game Resumed");
        }

        #endregion

        #region Button Callbacks

        private void OnResume()
        {
            Resume();
        }

        private void OnOpenSettings()
        {
            // Play click sound
            if (UIAudioManager.Exists())
            {
                UIAudioManager.Instance.PlayClick();
            }

            if (settingsPanel != null)
                settingsPanel.SetActive(true);
        }

        private void OnCloseSettings()
        {
            // Play back sound
            if (UIAudioManager.Exists())
            {
                UIAudioManager.Instance.PlayBack();
            }

            if (settingsPanel != null)
                settingsPanel.SetActive(false);

            SaveSettings();
        }

        private void OnQuitToMenu()
        {
            // Play click sound
            if (UIAudioManager.Exists())
            {
                UIAudioManager.Instance.PlayClick();
            }

            // Resume time before loading scene
            Time.timeScale = 1f;

            // TODO: Implement proper scene loading
            // For now, just log
            Debug.Log("🚪 Quitting to main menu...");
            
            // Example: UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        #endregion

        #region Audio Settings

        private void OnMasterVolumeChanged(float value)
        {
            // Convert 0-1 slider range to 0-100 for Wwise
            float wwiseValue = value * 100f;
            AkSoundEngine.SetRTPCValue("Master_Volume", wwiseValue);
            Debug.Log($"🔊 Master Volume: {value:F2} ({wwiseValue:F0}%)");
        }

        private void OnMusicVolumeChanged(float value)
        {
            // Convert 0-1 slider range to 0-100 for Wwise
            float wwiseValue = value * 100f;
            AkSoundEngine.SetRTPCValue("Music_Volume", wwiseValue);
            Debug.Log($"🎵 Music Volume: {value:F2} ({wwiseValue:F0}%)");
        }

        private void OnSFXVolumeChanged(float value)
        {
            // Convert 0-1 slider range to 0-100 for Wwise
            float wwiseValue = value * 100f;
            AkSoundEngine.SetRTPCValue("SFX_Volume", wwiseValue);
            Debug.Log($"🔔 SFX Volume: {value:F2} ({wwiseValue:F0}%)");
        }

        private void OnPlayerVolumeChanged(float value)
        {
            // Update player volume text
            if (playerVolumeValueText != null)
            {
                playerVolumeValueText.text = value.ToString("F2");
            }

            // Convert 0-1 slider range to 0-100 for Wwise
            float wwiseValue = value * 100f;
            AkSoundEngine.SetRTPCValue("Player_Volume", wwiseValue);
            Debug.Log($"🎮 Player Volume: {value:F2} ({wwiseValue:F0}%)");
        }

        #endregion

        #region Save/Load Settings

        private void SaveSettings()
        {
            if (masterVolumeSlider != null)
                PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);

            if (musicVolumeSlider != null)
                PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);

            if (sfxVolumeSlider != null)
                PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);

            if (playerVolumeSlider != null)
                PlayerPrefs.SetFloat("PlayerVolume", playerVolumeSlider.value);

            PlayerPrefs.Save();
            Debug.Log("💾 Settings saved");
        }

        private void LoadSettings()
        {
            // Load audio settings
            if (masterVolumeSlider != null)
            {
                float volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
                masterVolumeSlider.value = volume;
                OnMasterVolumeChanged(volume);
            }

            if (musicVolumeSlider != null)
            {
                float volume = PlayerPrefs.GetFloat("MusicVolume", 1f);
                musicVolumeSlider.value = volume;
                OnMusicVolumeChanged(volume);
            }

            if (sfxVolumeSlider != null)
            {
                float volume = PlayerPrefs.GetFloat("SFXVolume", 1f);
                sfxVolumeSlider.value = volume;
                OnSFXVolumeChanged(volume);
            }

            if (playerVolumeSlider != null)
            {
                float volume = PlayerPrefs.GetFloat("PlayerVolume", 1f);
                playerVolumeSlider.value = volume;
                OnPlayerVolumeChanged(volume);
            }

            Debug.Log("📂 Settings loaded");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Check if the game is currently paused.
        /// </summary>
        public bool IsPaused()
        {
            return isPaused;
        }

        /// <summary>
        /// Set whether we're in the main menu (disables pause if needed).
        /// </summary>
        public void SetInMainMenu(bool inMenu)
        {
            isInMainMenu = inMenu;
        }

        #endregion
    }
}
