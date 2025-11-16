using UnityEngine;

/// <summary>
/// Singleton manager for UI audio events (non-networked, local only).
/// Handles all UI sounds like clicks, hovers, etc.
/// Place this on a GameObject in your UI scene or as a persistent manager.
/// </summary>
public class UIAudioManager : MonoBehaviour
{
    #region Singleton

    private static UIAudioManager instance;

    /// <summary>
    /// Singleton instance for global access.
    /// </summary>
    public static UIAudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogError("[UIAudioManager] No instance found in scene. Please add UIAudioManager to your scene.");
            }
            return instance;
        }
    }

    #endregion

    #region Serialized Fields

    [Header("Wwise UI Events")]
    [Tooltip("Wwise event for UI button clicks")]
    [SerializeField] private AK.Wwise.Event uiClickEvent;

    [Tooltip("Wwise event for UI hover/mouseover (optional)")]
    [SerializeField] private AK.Wwise.Event uiHoverEvent;

    [Tooltip("Wwise event for UI navigation/selection (optional)")]
    [SerializeField] private AK.Wwise.Event uiSelectEvent;

    [Tooltip("Wwise event for UI back/cancel action (optional)")]
    [SerializeField] private AK.Wwise.Event uiBackEvent;

    [Header("Settings")]
    [Tooltip("If true, this GameObject will persist across scene loads")]
    [SerializeField] private bool persistAcrossScenes = false;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Enforce singleton pattern
        if (instance != null && instance != this)
        {
            Debug.LogWarning("[UIAudioManager] Multiple instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        instance = this;

        // Optional: Make persistent across scenes
        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Clear instance reference when destroyed
        if (instance == this)
        {
            instance = null;
        }
    }

    #endregion

    #region Public Audio Methods

    /// <summary>
    /// Plays the UI click sound. Call this when a button is clicked.
    /// This is a local-only sound (not networked).
    /// </summary>
    public void PlayClick()
    {
        if (uiClickEvent != null)
        {
            uiClickEvent.Post(gameObject);
        }
        else
        {
            Debug.LogWarning("[UIAudioManager] UI Click event is not assigned.");
        }
    }

    /// <summary>
    /// Plays the UI hover sound. Call this when hovering over UI elements.
    /// This is a local-only sound (not networked).
    /// </summary>
    public void PlayHover()
    {
        if (uiHoverEvent != null)
        {
            uiHoverEvent.Post(gameObject);
        }
    }

    /// <summary>
    /// Plays the UI select sound. Call this when selecting/highlighting UI elements.
    /// This is a local-only sound (not networked).
    /// </summary>
    public void PlaySelect()
    {
        if (uiSelectEvent != null)
        {
            uiSelectEvent.Post(gameObject);
        }
    }

    /// <summary>
    /// Plays the UI back/cancel sound. Call this when pressing back or cancel buttons.
    /// This is a local-only sound (not networked).
    /// </summary>
    public void PlayBack()
    {
        if (uiBackEvent != null)
        {
            uiBackEvent.Post(gameObject);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Checks if the UIAudioManager instance exists in the scene.
    /// </summary>
    public static bool Exists()
    {
        return instance != null;
    }

    #endregion
}
