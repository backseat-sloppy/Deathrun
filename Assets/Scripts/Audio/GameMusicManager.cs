using UnityEngine;

/// <summary>
/// Manages dynamic music based on GamePhase states.
/// Switches music tracks or states when the game phase changes.
/// </summary>
public class GameMusicManager : MonoBehaviour
{
    #region Serialized Fields

    [Header("Wwise Music Events")]
    [Tooltip("Music event to play during Menu phase")]
    [SerializeField] private AK.Wwise.Event menuMusicEvent;

    [Tooltip("Music event to play during Playing phase")]
    [SerializeField] private AK.Wwise.Event playingMusicEvent;

    [Tooltip("Music event to play during Countdown phase")]
    [SerializeField] private AK.Wwise.Event countdownMusicEvent;

    [Tooltip("Music event to play during Completed phase")]
    [SerializeField] private AK.Wwise.Event completedMusicEvent;

    [Tooltip("Music event to play during Failed phase")]
    [SerializeField] private AK.Wwise.Event failedMusicEvent;

    [Header("Wwise Music States (Alternative Approach)")]
    [Tooltip("Use Wwise States instead of separate events")]
    [SerializeField] private bool useWwiseStates = true;

    [Tooltip("Name of the Wwise State Group (e.g., 'GamePhase')")]
    [SerializeField] private string stateGroup = "GamePhase";

    [Tooltip("Name of the Wwise Focus State Group (e.g., 'Focus')")]
    [SerializeField] private string focusStateGroup = "Focus";

    [Tooltip("Main music event that responds to states (e.g., 'Play_Music')")]
    [SerializeField] private AK.Wwise.Event mainMusicEvent;

    [Header("Settings")]
    [Tooltip("GameObject to use as music emitter")]
    [SerializeField] private GameObject musicEmitter;

    [Tooltip("Auto-start music on scene load")]
    [SerializeField] private bool autoStartMusic = true;

    [Tooltip("Listen to GamePhaseManager for automatic music changes")]
    [SerializeField] private bool listenToGamePhase = true;

    [Tooltip("Listen to Focus state changes for pause/resume")]
    [SerializeField] private bool listenToFocus = true;

    #endregion

    #region Private Variables

    private GameObject audioSource;
    private string currentPhase = "Menu";
    private string currentFocus = "Normal";
    private bool isMusicPlaying = false;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Determine audio source
        audioSource = musicEmitter != null ? musicEmitter : gameObject;
    }

    private void Start()
    {
        // Subscribe to GamePhaseManager if enabled
        if (listenToGamePhase && GamePhaseManager.Instance != null)
        {
            GamePhaseManager.Instance.OnPhaseChanged.AddListener(OnGamePhaseChanged);
            Debug.Log("🎵 GameMusicManager subscribed to GamePhaseManager");
        }

        // Subscribe to Focus state changes if enabled
        if (listenToFocus && GamePhaseManager.Instance != null)
        {
            GamePhaseManager.Instance.OnFocusChanged.AddListener(OnFocusChanged);
            Debug.Log("🎵 GameMusicManager subscribed to Focus state changes");
        }

        // Get current state from GamePhaseManager
        if (GamePhaseManager.Instance != null)
        {
            currentPhase = GamePhaseManager.Instance.GetCurrentPhase().ToString();
            currentFocus = GamePhaseManager.Instance.GetCurrentFocus().ToString();
            Debug.Log($"🎵 Initialized with phase: {currentPhase}, focus: {currentFocus}");
        }

        if (autoStartMusic)
        {
            // Start music with current state
            UpdateCombinedState();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from GamePhaseManager
        if (listenToGamePhase && GamePhaseManager.Instance != null)
        {
            GamePhaseManager.Instance.OnPhaseChanged.RemoveListener(OnGamePhaseChanged);
        }

        // Unsubscribe from Focus state changes
        if (listenToFocus && GamePhaseManager.Instance != null)
        {
            GamePhaseManager.Instance.OnFocusChanged.RemoveListener(OnFocusChanged);
        }
    }

    #endregion

    #region GamePhaseManager Listener

    /// <summary>
    /// Called automatically when GamePhaseManager changes phase.
    /// </summary>
    private void OnGamePhaseChanged(GamePhaseManager.GamePhase newPhase)
    {
        currentPhase = newPhase.ToString();
        UpdateCombinedState();
    }

    /// <summary>
    /// Called automatically when GamePhaseManager changes focus state.
    /// </summary>
    private void OnFocusChanged(GamePhaseManager.Focus newFocus)
    {
        currentFocus = newFocus.ToString();
        UpdateCombinedState();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Updates the Wwise states for GamePhase and Focus.
    /// </summary>
    private void UpdateCombinedState()
    {
        Debug.Log($"🎵 Music: Setting states - Phase: {currentPhase}, Focus: {currentFocus}");

        if (useWwiseStates)
        {
            // Set GamePhase state (e.g., "Menu", "Playing")
            AkSoundEngine.SetState(stateGroup, currentPhase);
            Debug.Log($"🎵 Set Wwise State: {stateGroup}/{currentPhase}");

            // Set Focus state (e.g., "Normal", "Paused")
            AkSoundEngine.SetState(focusStateGroup, currentFocus);
            Debug.Log($"🎵 Set Wwise State: {focusStateGroup}/{currentFocus}");

            // Start music if not already playing
            if (!isMusicPlaying && mainMusicEvent != null)
            {
                mainMusicEvent.Post(audioSource);
                isMusicPlaying = true;
                Debug.Log("🎵 Music: Started main music event");
            }
        }
        else
        {
            // Use separate music events (legacy approach)
            PlayMusicForPhase(currentPhase);
        }
    }

    /// <summary>
    /// Stops all music.
    /// </summary>
    public void StopMusic()
    {
        AkSoundEngine.StopAll(audioSource);
        isMusicPlaying = false;
        Debug.Log("🎵 Music: Stopped all music");
    }

    /// <summary>
    /// Pauses the music.
    /// </summary>
    public void PauseMusic()
    {
        AkSoundEngine.PostEvent("Pause_All", audioSource);
        Debug.Log("🎵 Music: Paused");
    }

    /// <summary>
    /// Resumes the music.
    /// </summary>
    public void ResumeMusic()
    {
        AkSoundEngine.PostEvent("Resume_All", audioSource);
        Debug.Log("🎵 Music: Resumed");
    }

    #endregion

    #region Private Methods - Wwise States

    private void SetWwiseState(string phase)
    {
        // Set Wwise State based on game phase
        AkSoundEngine.SetState(stateGroup, phase);
        Debug.Log($"🎵 Set Wwise State: {stateGroup}/{phase}");
    }

    #endregion

    #region Private Methods - Music Events

    private void PlayMusicForPhase(string phase)
    {
        // Stop current music before starting new one
        StopMusic();

        // Play appropriate music event
        switch (phase)
        {
            case "Menu":
                if (menuMusicEvent != null)
                    menuMusicEvent.Post(audioSource);
                break;

            case "Playing":
                if (playingMusicEvent != null)
                    playingMusicEvent.Post(audioSource);
                break;

            case "Countdown":
                if (countdownMusicEvent != null)
                    countdownMusicEvent.Post(audioSource);
                break;

            case "Completed":
                if (completedMusicEvent != null)
                    completedMusicEvent.Post(audioSource);
                break;

            case "Failed":
                if (failedMusicEvent != null)
                    failedMusicEvent.Post(audioSource);
                break;

            default:
                Debug.LogWarning($"🎵 Unknown game phase: {phase}");
                break;
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the current game phase.
    /// </summary>
    public string GetCurrentPhase()
    {
        return currentPhase;
    }

    #endregion
}
