using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Handles audio events for falling platform traps in multiplayer.
/// Attach this to your falling platform GameObject.
/// Uses Netcode RPCs to sync audio across all clients.
/// </summary>
public class TrapAudio_FallingPlatform : NetworkBehaviour
{
    #region Serialized Fields

    [Header("Wwise Events")]
    [Tooltip("Wwise event for platform creaking (warning sound before falling)")]
    [SerializeField] private AK.Wwise.Event platformCreakEvent;

    [Tooltip("Wwise event for platform falling/dropping")]
    [SerializeField] private AK.Wwise.Event platformFallEvent;

    [Tooltip("Wwise event for platform impact when hitting the ground")]
    [SerializeField] private AK.Wwise.Event platformImpactEvent;

    [Header("Audio Settings")]
    [Tooltip("Optional: Specific GameObject to use as Wwise emitter. If null, uses this GameObject.")]
    [SerializeField] private GameObject wwiseEmitter;

    #endregion

    #region Private Variables

    private GameObject audioSource; // The actual emitter used for posting events

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Determine which GameObject will be used for Wwise audio emission
        audioSource = wwiseEmitter != null ? wwiseEmitter : gameObject;
    }

    #endregion

    #region Public Audio Methods

    /// <summary>
    /// Plays the platform creak sound. Call this when the platform starts to shake/warn before falling.
    /// This gives players an audio cue that the platform is about to drop.
    /// Should be called by the server/trap controller script.
    /// </summary>
    public void PlayPlatformCreak()
    {
        // Only the server should trigger trap sounds to avoid duplicates
        if (!IsServer) return;

        // Broadcast to all clients
        PlayPlatformCreakClientRpc();
    }

    /// <summary>
    /// Plays the platform fall sound. Call this when the platform actually starts falling.
    /// Should be called by the server when the platform begins its descent.
    /// </summary>
    public void PlayPlatformFall()
    {
        if (!IsServer) return;
        PlayPlatformFallClientRpc();
    }

    /// <summary>
    /// Plays the platform impact sound. Call this when the platform hits the ground.
    /// Should be called by the server when collision with ground is detected.
    /// </summary>
    public void PlayPlatformImpact()
    {
        if (!IsServer) return;
        PlayPlatformImpactClientRpc();
    }

    #endregion

    #region Platform Creak Audio - Network RPCs

    /// <summary>
    /// ClientRpc: All clients play the platform creak sound.
    /// This is the warning sound before the platform falls.
    /// </summary>
    [ClientRpc]
    private void PlayPlatformCreakClientRpc()
    {
        if (platformCreakEvent != null && audioSource != null)
        {
            platformCreakEvent.Post(audioSource);
        }
    }

    #endregion

    #region Platform Fall Audio - Network RPCs

    /// <summary>
    /// ClientRpc: All clients play the platform fall sound.
    /// This plays as the platform is dropping.
    /// </summary>
    [ClientRpc]
    private void PlayPlatformFallClientRpc()
    {
        if (platformFallEvent != null && audioSource != null)
        {
            platformFallEvent.Post(audioSource);
        }
    }

    #endregion

    #region Platform Impact Audio - Network RPCs

    /// <summary>
    /// ClientRpc: All clients play the platform impact sound.
    /// This plays when the platform hits the ground.
    /// </summary>
    [ClientRpc]
    private void PlayPlatformImpactClientRpc()
    {
        if (platformImpactEvent != null && audioSource != null)
        {
            platformImpactEvent.Post(audioSource);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Plays the complete falling sequence with timing.
    /// Useful for simple trap controllers that want one method call.
    /// </summary>
    /// <param name="creakDelay">Delay before playing creak sound (default: immediate)</param>
    /// <param name="fallDelay">Delay before playing fall sound after creak (default: 0.5s)</param>
    public void PlayFallingSequence(float creakDelay = 0f, float fallDelay = 0.5f)
    {
        if (!IsServer) return;

        // Play creak sound
        if (creakDelay > 0f)
        {
            Invoke(nameof(PlayPlatformCreak), creakDelay);
        }
        else
        {
            PlayPlatformCreak();
        }

        // Play fall sound after delay
        Invoke(nameof(PlayPlatformFall), creakDelay + fallDelay);
    }

    /// <summary>
    /// Stop any ongoing creak or fall sounds.
    /// Useful if the trap is reset before completing the sequence.
    /// </summary>
    public void StopAllSounds()
    {
        if (!IsServer) return;

        // Cancel any pending Invoke calls
        CancelInvoke();

        // Optionally stop Wwise events if needed
        // AkSoundEngine.StopAll(audioSource);
    }

    #endregion

    #region Animation Event Support

    /// <summary>
    /// Animation-friendly method to trigger creak sound.
    /// Can be called from Animation Events.
    /// </summary>
    public void AnimationEvent_PlayCreak()
    {
        PlayPlatformCreak();
    }

    /// <summary>
    /// Animation-friendly method to trigger fall sound.
    /// Can be called from Animation Events.
    /// </summary>
    public void AnimationEvent_PlayFall()
    {
        PlayPlatformFall();
    }

    /// <summary>
    /// Animation-friendly method to trigger impact sound.
    /// Can be called from Animation Events.
    /// </summary>
    public void AnimationEvent_PlayImpact()
    {
        PlayPlatformImpact();
    }

    #endregion
}
