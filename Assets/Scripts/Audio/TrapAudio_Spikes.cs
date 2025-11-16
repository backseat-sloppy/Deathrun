using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Handles audio events for spike traps in multiplayer.
/// Attach this to your spike trap GameObject.
/// Uses Netcode RPCs to sync audio across all clients.
/// </summary>
public class TrapAudio_Spikes : NetworkBehaviour
{
    #region Serialized Fields

    [Header("Wwise Events")]
    [Tooltip("Wwise event for spikes rising from the ground")]
    [SerializeField] private AK.Wwise.Event spikesRiseEvent;

    [Tooltip("Wwise event for spikes retracting back into the ground")]
    [SerializeField] private AK.Wwise.Event spikesRetractEvent;

    [Tooltip("Wwise event for spikes hitting a player")]
    [SerializeField] private AK.Wwise.Event spikesHitEvent;

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
    /// Plays the spikes rise sound. Call this when the spikes start emerging from the ground.
    /// Should be called by the server/trap controller script.
    /// </summary>
    public void PlaySpikesRise()
    {
        // Only the server should trigger trap sounds to avoid duplicates
        if (!IsServer) return;

        // Broadcast to all clients
        PlaySpikesRiseClientRpc();
    }

    /// <summary>
    /// Plays the spikes retract sound. Call this when the spikes go back into the ground.
    /// Should be called by the server when the trap resets.
    /// </summary>
    public void PlaySpikesRetract()
    {
        if (!IsServer) return;
        PlaySpikesRetractClientRpc();
    }

    /// <summary>
    /// Plays the spikes hit sound. Call this when the spikes damage a player.
    /// Should be called by the server when collision/damage is detected.
    /// </summary>
    public void PlaySpikesHit()
    {
        if (!IsServer) return;
        PlaySpikesHitClientRpc();
    }

    #endregion

    #region Spikes Rise Audio - Network RPCs

    /// <summary>
    /// ClientRpc: All clients play the spikes rise sound.
    /// This ensures everyone hears the trap activation in sync.
    /// </summary>
    [ClientRpc]
    private void PlaySpikesRiseClientRpc()
    {
        if (spikesRiseEvent != null && audioSource != null)
        {
            spikesRiseEvent.Post(audioSource);
        }
    }

    #endregion

    #region Spikes Retract Audio - Network RPCs

    /// <summary>
    /// ClientRpc: All clients play the spikes retract sound.
    /// This plays when the trap resets to its idle state.
    /// </summary>
    [ClientRpc]
    private void PlaySpikesRetractClientRpc()
    {
        if (spikesRetractEvent != null && audioSource != null)
        {
            spikesRetractEvent.Post(audioSource);
        }
    }

    #endregion

    #region Spikes Hit Audio - Network RPCs

    /// <summary>
    /// ClientRpc: All clients play the spikes hit sound.
    /// This plays when a player is damaged by the spikes.
    /// </summary>
    [ClientRpc]
    private void PlaySpikesHitClientRpc()
    {
        if (spikesHitEvent != null && audioSource != null)
        {
            spikesHitEvent.Post(audioSource);
        }
    }

    #endregion

    #region Alternative Methods with Animation Event Support

    /// <summary>
    /// Animation-friendly method to trigger rise sound.
    /// Can be called from Animation Events on the spike trap animation.
    /// </summary>
    public void AnimationEvent_PlayRise()
    {
        PlaySpikesRise();
    }

    /// <summary>
    /// Animation-friendly method to trigger retract sound.
    /// Can be called from Animation Events on the spike trap animation.
    /// </summary>
    public void AnimationEvent_PlayRetract()
    {
        PlaySpikesRetract();
    }

    #endregion
}
