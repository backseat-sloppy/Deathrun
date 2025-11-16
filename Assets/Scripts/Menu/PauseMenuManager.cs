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

        [Header("Settings - Graphics")]
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private Toggle fullscreenToggle;

        [Header("Settings - Mouse Sensitivity")]
        [SerializeField] private Slider mouseSensitivitySlider;
        [SerializeField] private TextMeshProUGUI sensitivityValueText;

        [Header("Settings")]
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
        [SerializeField] private bool disablePauseInMenu = true;

        #endregion

        #region Private Variables

        private bool isPaused = false;
        private bool isInMainMenu = true; // Assume we start in main menu
        private PlayerCameraController cameraController;

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

            // Graphics settings
            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);

            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);

            // Mouse sensitivity
            if (mouseSensitivitySlider != null)
                mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
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
            // TODO: Integrate with Wwise
            // Example: AkSoundEngine.SetRTPCValue("Master_Volume", value);
            Debug.Log($"🔊 Master Volume: {value}");
        }

        private void OnMusicVolumeChanged(float value)
        {
            // TODO: Integrate with Wwise
            // Example: AkSoundEngine.SetRTPCValue("Music_Volume", value);
            Debug.Log($"🎵 Music Volume: {value}");
        }

        private void OnSFXVolumeChanged(float value)
        {
            // TODO: Integrate with Wwise
            // Example: AkSoundEngine.SetRTPCValue("SFX_Volume", value);
            Debug.Log($"🔔 SFX Volume: {value}");
        }

        #endregion

        #region Graphics Settings

        private void OnQualityChanged(int qualityIndex)
        {
            QualitySettings.SetQualityLevel(qualityIndex);
            Debug.Log($"🎨 Quality set to: {QualitySettings.names[qualityIndex]}");
        }

        private void OnFullscreenChanged(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
            Debug.Log($"🖥️ Fullscreen: {isFullscreen}");
        }

        #endregion

        #region Mouse Sensitivity

        private void OnMouseSensitivityChanged(float value)
        {
            // Update sensitivity text
            if (sensitivityValueText != null)
            {
                sensitivityValueText.text = value.ToString("F2");
            }

            // Update camera controller sensitivity if available
            if (cameraController == null)
            {
                // Try to find the player's camera controller
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    cameraController = player.GetComponent<PlayerCameraController>();
                }
            }

            // TODO: Apply sensitivity to camera controller
            // You'll need to add public setters in PlayerCameraController for this
            Debug.Log($"🖱️ Mouse Sensitivity: {value}");
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

            if (qualityDropdown != null)
                PlayerPrefs.SetInt("QualityLevel", qualityDropdown.value);

            if (fullscreenToggle != null)
                PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);

            if (mouseSensitivitySlider != null)
                PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivitySlider.value);

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

            // Load graphics settings
            if (qualityDropdown != null)
            {
                int quality = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
                qualityDropdown.value = quality;
                OnQualityChanged(quality);
            }

            if (fullscreenToggle != null)
            {
                bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
                fullscreenToggle.isOn = isFullscreen;
                OnFullscreenChanged(isFullscreen);
            }

            // Load mouse sensitivity
            if (mouseSensitivitySlider != null)
            {
                float sensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 2f);
                mouseSensitivitySlider.value = sensitivity;
                OnMouseSensitivityChanged(sensitivity);
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
