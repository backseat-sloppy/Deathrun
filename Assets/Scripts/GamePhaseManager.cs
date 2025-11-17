using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Centralized game phase manager that tracks the current game state.
/// Other systems can subscribe to phase changes (like music, UI, etc.)
/// </summary>
public class GamePhaseManager : MonoBehaviour
{
    #region Singleton

    private static GamePhaseManager instance;
    public static GamePhaseManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GamePhaseManager>();
                if (instance == null)
                {
                    Debug.LogWarning("No GamePhaseManager found in scene.");
                }
            }
            return instance;
        }
    }

    #endregion

    #region Game Phase Enum

    public enum GamePhase
    {
        None,
        Menu,
        Countdown,
        Playing,
        Completed,
        Failed
    }

    public enum Focus
    {
        Normal,
        Paused
    }

    #endregion

    #region Events

    [System.Serializable]
    public class GamePhaseEvent : UnityEvent<GamePhase> { }

    [System.Serializable]
    public class FocusEvent : UnityEvent<Focus> { }

    [Header("Phase Change Events")]
    public GamePhaseEvent OnPhaseChanged = new GamePhaseEvent();
    
    [Header("Focus Change Events")]
    public FocusEvent OnFocusChanged = new FocusEvent();

    #endregion

    #region Serialized Fields

    [Header("Current Phase")]
    [SerializeField] private GamePhase currentPhase = GamePhase.None;
    
    [Header("Current Focus")]
    [SerializeField] private Focus currentFocus = Focus.Normal;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Singleton setup
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Changes the current game phase and notifies all listeners.
    /// </summary>
    public void SetPhase(GamePhase newPhase)
    {
        if (currentPhase == newPhase) return;

        GamePhase previousPhase = currentPhase;
        currentPhase = newPhase;

        if (showDebugLogs)
        {
            Debug.Log($"🎮 Game Phase: {previousPhase} → {newPhase}");
        }

        // Notify all listeners
        OnPhaseChanged?.Invoke(newPhase);
    }

    /// <summary>
    /// Gets the current game phase.
    /// </summary>
    public GamePhase GetCurrentPhase()
    {
        return currentPhase;
    }

    /// <summary>
    /// Gets the current phase as a string (for Wwise states).
    /// </summary>
    public string GetCurrentPhaseString()
    {
        return currentPhase.ToString();
    }

    /// <summary>
    /// Changes the current focus state (Normal/Paused) and notifies all listeners.
    /// </summary>
    public void SetFocus(Focus newFocus)
    {
        if (currentFocus == newFocus) return;

        Focus previousFocus = currentFocus;
        currentFocus = newFocus;

        if (showDebugLogs)
        {
            Debug.Log($"🎯 Focus: {previousFocus} → {newFocus}");
        }

        // Notify all listeners
        OnFocusChanged?.Invoke(newFocus);
    }

    /// <summary>
    /// Gets the current focus state.
    /// </summary>
    public Focus GetCurrentFocus()
    {
        return currentFocus;
    }

    /// <summary>
    /// Gets the current focus as a string (for Wwise states).
    /// </summary>
    public string GetCurrentFocusString()
    {
        return currentFocus.ToString();
    }

    #endregion

    #region Helper Methods - Quick Access

    public void SetMenu() => SetPhase(GamePhase.Menu);
    public void SetCountdown() => SetPhase(GamePhase.Countdown);
    public void SetPlaying() => SetPhase(GamePhase.Playing);
    public void SetCompleted() => SetPhase(GamePhase.Completed);
    public void SetFailed() => SetPhase(GamePhase.Failed);

    public bool IsMenu() => currentPhase == GamePhase.Menu;
    public bool IsPlaying() => currentPhase == GamePhase.Playing;
    public bool IsGameOver() => currentPhase == GamePhase.Completed || currentPhase == GamePhase.Failed;

    // Focus helpers
    public void SetNormal() => SetFocus(Focus.Normal);
    public void SetPaused() => SetFocus(Focus.Paused);
    public bool IsPaused() => currentFocus == Focus.Paused;
    public bool IsNormal() => currentFocus == Focus.Normal;

    #endregion
}
