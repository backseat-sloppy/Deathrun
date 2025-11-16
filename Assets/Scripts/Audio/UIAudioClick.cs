using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach this to UI buttons or other clickable UI elements to play click sounds.
/// Uses Unity's Event System to detect pointer clicks.
/// This is a local-only audio component (not networked).
/// </summary>
public class UIAudioClick : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    #region Serialized Fields

    [Header("Audio Triggers")]
    [Tooltip("Play click sound when this UI element is clicked")]
    [SerializeField] private bool playClickSound = true;

    [Tooltip("Play hover sound when mouse enters this UI element")]
    [SerializeField] private bool playHoverSound = false;

    #endregion

    #region IPointerClickHandler Implementation

    /// <summary>
    /// Called when the UI element is clicked.
    /// Triggers the click sound via UIAudioManager.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!playClickSound) return;

        // Check if UIAudioManager exists before calling
        if (UIAudioManager.Exists())
        {
            UIAudioManager.Instance.PlayClick();
        }
        else
        {
            Debug.LogWarning("[UIAudioClick] UIAudioManager not found in scene. Cannot play click sound.");
        }
    }

    #endregion

    #region IPointerEnterHandler Implementation

    /// <summary>
    /// Called when the pointer enters the UI element.
    /// Triggers the hover sound via UIAudioManager if enabled.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!playHoverSound) return;

        if (UIAudioManager.Exists())
        {
            UIAudioManager.Instance.PlayHover();
        }
    }

    #endregion

    #region Public Methods (Alternative Usage)

    /// <summary>
    /// Manual method to play click sound.
    /// Can be called from Unity UI Button OnClick events as an alternative to IPointerClickHandler.
    /// </summary>
    public void PlayClickSoundManual()
    {
        if (UIAudioManager.Exists())
        {
            UIAudioManager.Instance.PlayClick();
        }
    }

    /// <summary>
    /// Manual method to play hover sound.
    /// Can be called from custom scripts if needed.
    /// </summary>
    public void PlayHoverSoundManual()
    {
        if (UIAudioManager.Exists())
        {
            UIAudioManager.Instance.PlayHover();
        }
    }

    #endregion
}
