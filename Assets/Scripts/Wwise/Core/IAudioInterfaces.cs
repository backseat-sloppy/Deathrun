using UnityEngine;
using Unity.Netcode;

namespace DeathrunGame.Audio
{
    /// <summary>
    /// Interface for objects that can be audio sources in the game.
    /// Provides a unified way to handle audio events across different game objects.
    /// </summary>
    public interface IAudioSource
    {
        /// <summary>
        /// The GameObject that this audio source is attached to.
        /// Used for Wwise's 3D positioning and game object registration.
        /// </summary>
        GameObject AudioGameObject { get; }
        
        /// <summary>
        /// Whether this audio source is currently active and should play sounds.
        /// </summary>
        bool IsAudioActive { get; }
        
        /// <summary>
        /// Unique identifier for this audio source, useful for networking.
        /// </summary>
        uint AudioSourceId { get; }
    }
    
    /// <summary>
    /// Interface for multiplayer-aware audio components.
    /// Handles synchronization of audio events across network clients.
    /// </summary>
    public interface INetworkAudioSource : IAudioSource
    {
        /// <summary>
        /// Whether this client owns this audio source (for multiplayer).
        /// Non-owners may have limited audio playback (e.g., no first-person sounds).
        /// </summary>
        bool IsLocalPlayer { get; }
        
        /// <summary>
        /// Called when an audio event should be synchronized across the network.
        /// </summary>
        /// <param name="eventName">Name of the Wwise event</param>
        /// <param name="parameters">Any RTPC parameters to sync</param>
        void OnNetworkAudioEvent(string eventName, AudioParameterData[] parameters = null);
    }
    
    /// <summary>
    /// Interface for objects that can detect and report surface types.
    /// Used by the footstep system to switch surface materials.
    /// </summary>
    public interface ISurfaceDetector
    {
        /// <summary>
        /// Get the current surface type at the specified position.
        /// </summary>
        /// <param name="position">World position to check</param>
        /// <param name="normal">Surface normal (if available)</param>
        /// <returns>Detected surface type</returns>
        SurfaceType GetSurfaceType(Vector3 position, Vector3 normal = default);
        
        /// <summary>
        /// Event fired when the surface type changes.
        /// </summary>
        event System.Action<SurfaceType> OnSurfaceTypeChanged;
    }
    
    /// <summary>
    /// Data structure for passing audio parameters in networking.
    /// Implements INetworkSerializable for Unity Netcode compatibility.
    /// </summary>
    [System.Serializable]
    public struct AudioParameterData : INetworkSerializable
    {
        public string parameterName;
        public float value;
        public bool isGlobal;
        
        public AudioParameterData(string name, float val, bool global = false)
        {
            parameterName = name;
            value = val;
            isGlobal = global;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref parameterName);
            serializer.SerializeValue(ref value);
            serializer.SerializeValue(ref isGlobal);
        }
    }
}