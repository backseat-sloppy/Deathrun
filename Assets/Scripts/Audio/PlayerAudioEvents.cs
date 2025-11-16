using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Handles all audio events for the player character in multiplayer.
/// Uses Netcode RPCs to sync audio across all clients.
/// Attach this to the player prefab root.
/// </summary>
public class PlayerAudioEvents : NetworkBehaviour
{
    #region Serialized Fields

    [Header("Wwise Events")]
    [Tooltip("Wwise event for player death sound")]
    [SerializeField] private AK.Wwise.Event deathEvent;

    [Tooltip("Wwise event for player jump sound")]
    [SerializeField] private AK.Wwise.Event jumpEvent;

    [Tooltip("Wwise event for player land sound")]
    [SerializeField] private AK.Wwise.Event landEvent;

    [Tooltip("Wwise event for player melee swing sound")]
    [SerializeField] private AK.Wwise.Event swingEvent;

    [Tooltip("Wwise event for footstep sounds")]
    [SerializeField] private AK.Wwise.Event footstepEvent;

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
    /// Plays the death sound. Should be called by gameplay scripts when the player dies.
    /// Only the owner or server should call this method.
    /// </summary>
    public void PlayDeath()
    {
        // Only the owner should request audio playback to avoid duplicate sounds
        if (!IsOwner) return;

        // Send request to server for validation and broadcasting
        PlayDeathServerRpc();
    }

    /// <summary>
    /// Plays the jump sound. Call this from Animation Events on your jump animation.
    /// The animation system will automatically call this only on the owner's instance.
    /// </summary>
    public void PlayJump()
    {
        if (!IsOwner) return;
        PlayJumpServerRpc();
    }

    /// <summary>
    /// Plays the land sound. Call this from Animation Events on your landing animation.
    /// The animation system will automatically call this only on the owner's instance.
    /// </summary>
    public void PlayLand()
    {
        if (!IsOwner) return;
        PlayLandServerRpc();
    }

    /// <summary>
    /// Plays the swing sound. Call this from Animation Events on your attack animation.
    /// Typically placed at the frame where the weapon swings through the arc.
    /// </summary>
    public void PlaySwing()
    {
        if (!IsOwner) return;
        PlaySwingServerRpc();
    }

    /// <summary>
    /// Plays a footstep sound. Should be called by FootstepTrigger components (collision-based).
    /// Do NOT call this from animation events - use FootstepTrigger instead for better ground detection.
    /// </summary>
    /// <param name="footId">Identifier for which foot (e.g., "LeftFoot" or "RightFoot")</param>
    public void PlayFootstep(string footId)
    {
        if (!IsOwner) return;
        PlayFootstepServerRpc(footId);
    }

    #endregion

    #region Death Audio - Network RPCs

    /// <summary>
    /// ServerRpc: Client requests to play death sound.
    /// Server validates and broadcasts to all clients.
    /// </summary>
    [ServerRpc]
    private void PlayDeathServerRpc()
    {
        // Server validates the request (e.g., check if player actually died)
        // For now, we trust the client's request
        
        // Broadcast to all clients to play the sound
        PlayDeathClientRpc();
    }

    /// <summary>
    /// ClientRpc: All clients play the death sound on this player.
    /// This ensures everyone hears the death audio in sync.
    /// </summary>
    [ClientRpc]
    private void PlayDeathClientRpc()
    {
        // Post the Wwise event on the local client
        if (deathEvent != null && audioSource != null)
        {
            deathEvent.Post(audioSource);
        }
    }

    #endregion

    #region Jump Audio - Network RPCs

    /// <summary>
    /// ServerRpc: Client requests to play jump sound.
    /// Server validates and broadcasts to all clients.
    /// </summary>
    [ServerRpc]
    private void PlayJumpServerRpc()
    {
        // Server can add validation here if needed
        PlayJumpClientRpc();
    }

    /// <summary>
    /// ClientRpc: All clients play the jump sound on this player.
    /// </summary>
    [ClientRpc]
    private void PlayJumpClientRpc()
    {
        if (jumpEvent != null && audioSource != null)
        {
            jumpEvent.Post(audioSource);
        }
    }

    #endregion

    #region Land Audio - Network RPCs

    /// <summary>
    /// ServerRpc: Client requests to play land sound.
    /// Server validates and broadcasts to all clients.
    /// </summary>
    [ServerRpc]
    private void PlayLandServerRpc()
    {
        // Server validation point
        PlayLandClientRpc();
    }

    /// <summary>
    /// ClientRpc: All clients play the land sound on this player.
    /// </summary>
    [ClientRpc]
    private void PlayLandClientRpc()
    {
        if (landEvent != null && audioSource != null)
        {
            landEvent.Post(audioSource);
        }
    }

    #endregion

    #region Swing Audio - Network RPCs

    /// <summary>
    /// ServerRpc: Client requests to play swing sound.
    /// Server validates and broadcasts to all clients.
    /// </summary>
    [ServerRpc]
    private void PlaySwingServerRpc()
    {
        // Server can validate if player is allowed to attack
        PlaySwingClientRpc();
    }

    /// <summary>
    /// ClientRpc: All clients play the swing sound on this player.
    /// </summary>
    [ClientRpc]
    private void PlaySwingClientRpc()
    {
        if (swingEvent != null && audioSource != null)
        {
            swingEvent.Post(audioSource);
        }
    }

    #endregion

    #region Footstep Audio - Network RPCs

    /// <summary>
    /// ServerRpc: Client requests to play footstep sound.
    /// Server validates and broadcasts to all clients.
    /// </summary>
    /// <param name="footId">Identifier for which foot made the step</param>
    [ServerRpc]
    private void PlayFootstepServerRpc(string footId)
    {
        // Server can add validation (e.g., rate limiting to prevent spam)
        PlayFootstepClientRpc(footId);
    }

    /// <summary>
    /// ClientRpc: All clients play the footstep sound on this player.
    /// You can use the footId parameter to set Wwise switches/states if needed.
    /// </summary>
    /// <param name="footId">Identifier for which foot made the step</param>
    [ClientRpc]
    private void PlayFootstepClientRpc(string footId)
    {
        if (footstepEvent != null && audioSource != null)
        {
            // Optional: Set Wwise switch/state based on footId
            // Example: AkSoundEngine.SetSwitch("Foot", footId, audioSource);
            
            footstepEvent.Post(audioSource);
        }
    }

    #endregion
}
