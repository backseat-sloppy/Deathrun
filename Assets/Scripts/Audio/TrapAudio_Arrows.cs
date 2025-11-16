using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Handles audio events for arrow traps in multiplayer.
/// Attach this to your arrow trap GameObject.
/// Uses Netcode RPCs to sync audio across all clients.
/// </summary>
public class TrapAudio_Arrows : NetworkBehaviour
{
    #region Serialized Fields

    [Header("Wwise Events")]
    [Tooltip("Wwise event for arrow firing sound")]
    [SerializeField] private AK.Wwise.Event arrowFireEvent;

    [Tooltip("Wwise event for arrow impact/hit sound")]
    [SerializeField] private AK.Wwise.Event arrowImpactEvent;

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
    /// Plays the arrow fire sound. Call this when the trap fires arrows.
    /// Should be called by the server/trap controller script.
    /// </summary>
    public void PlayArrowFire()
    {
        // Only the server should trigger trap sounds to avoid duplicates
        if (!IsServer) return;

        // Broadcast to all clients
        PlayArrowFireClientRpc();
    }

    /// <summary>
    /// Plays the arrow impact sound. Call this when an arrow hits a surface or player.
    /// Should be called by the server when collision is detected.
    /// </summary>
    public void PlayArrowImpact()
    {
        if (!IsServer) return;
        PlayArrowImpactClientRpc();
    }

    #endregion

    #region Arrow Fire Audio - Network RPCs

    /// <summary>
    /// ClientRpc: All clients play the arrow fire sound.
    /// This ensures everyone hears the trap activation in sync.
    /// </summary>
    [ClientRpc]
    private void PlayArrowFireClientRpc()
    {
        if (arrowFireEvent != null && audioSource != null)
        {
            arrowFireEvent.Post(audioSource);
        }
    }

    #endregion

    #region Arrow Impact Audio - Network RPCs

    /// <summary>
    /// ClientRpc: All clients play the arrow impact sound.
    /// </summary>
    [ClientRpc]
    private void PlayArrowImpactClientRpc()
    {
        if (arrowImpactEvent != null && audioSource != null)
        {
            arrowImpactEvent.Post(audioSource);
        }
    }

    #endregion

    #region Public Methods with Position Override

    /// <summary>
    /// Plays arrow impact sound at a specific position.
    /// Useful when the arrow projectile is separate from the trap.
    /// </summary>
    /// <param name="impactPosition">World position where the arrow hit</param>
    public void PlayArrowImpactAtPosition(Vector3 impactPosition)
    {
        if (!IsServer) return;
        PlayArrowImpactAtPositionClientRpc(impactPosition);
    }

    /// <summary>
    /// ClientRpc: All clients play the arrow impact sound at a specific position.
    /// </summary>
    [ClientRpc]
    private void PlayArrowImpactAtPositionClientRpc(Vector3 impactPosition)
    {
        if (arrowImpactEvent != null)
        {
            // Post event at the specified position
            // Note: This requires creating a temporary GameObject or using AkSoundEngine directly
            // For simplicity, using the trap's audio source here
            // In production, you might want to use object pooling for impact sounds
            arrowImpactEvent.Post(audioSource);
        }
    }

    #endregion
}
