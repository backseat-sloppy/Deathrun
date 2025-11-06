using Unity.Netcode;
using UnityEngine;

namespace DeathrunGame.Audio
{
    /// <summary>
    /// NetworkBehaviour component for synchronizing audio events across multiplayer clients.
    /// Handles RPCs for audio event triggering and parameter synchronization.
    /// </summary>
    public class NetworkAudioSync : NetworkBehaviour
    {
        [Header("Network Settings")]
        [SerializeField] private bool enableNetworkAudio = true;
        [SerializeField] private bool debugNetworkAudio = false;
        [SerializeField] private float parameterSyncThreshold = 0.1f; // Only sync parameters if change is greater than this
        
        #region Private Fields
        private WwiseAudioManager _audioManager;
        private readonly float _lastParameterValues = 0f; // For throttling parameter updates
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            _audioManager = WwiseAudioManager.Instance;
            
            if (_audioManager == null)
            {
                Debug.LogError("NetworkAudioSync: WwiseAudioManager not found!");
                enableNetworkAudio = false;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Trigger an audio event across the network.
        /// Only the owner can trigger events.
        /// </summary>
        /// <param name="eventName">Wwise event name</param>
        /// <param name="includeOwner">Whether to play the event on the owner's client as well</param>
        public void TriggerNetworkAudioEvent(string eventName, bool includeOwner = true)
        {
            if (!enableNetworkAudio || !IsOwner) return;

            if (includeOwner)
            {
                // Play locally first
                _audioManager?.PostEvent(eventName, gameObject);
            }

            // Send to other clients
            TriggerAudioEventRpc(eventName);
            
            if (debugNetworkAudio)
            {
                Debug.Log($"NetworkAudioSync: Triggered network event '{eventName}' on {gameObject.name}");
            }
        }

        /// <summary>
        /// Trigger an audio event with parameters across the network.
        /// </summary>
        /// <param name="eventName">Wwise event name</param>
        /// <param name="parameters">Parameters to set before triggering</param>
        /// <param name="includeOwner">Whether to play the event on the owner's client as well</param>
        public void TriggerNetworkAudioEventWithParameters(string eventName, AudioParameterData[] parameters, bool includeOwner = true)
        {
            if (!enableNetworkAudio || !IsOwner || parameters == null) return;

            if (includeOwner)
            {
                // Play locally first
                PlayEventWithParameters(eventName, parameters);
            }

            // For network efficiency, send parameters individually rather than as array
            // This avoids complex array serialization
            foreach (var param in parameters)
            {
                SyncParameterRpc(param.parameterName, param.value, param.isGlobal);
            }
            
            // Then trigger the event
            TriggerAudioEventRpc(eventName);
            
            if (debugNetworkAudio)
            {
                Debug.Log($"NetworkAudioSync: Triggered network event '{eventName}' with {parameters.Length} parameters on {gameObject.name}");
            }
        }

        /// <summary>
        /// Sync a single parameter across the network.
        /// </summary>
        /// <param name="parameterName">Parameter name</param>
        /// <param name="value">Parameter value</param>
        /// <param name="isGlobal">Whether this is a global parameter</param>
        public void SyncParameter(string parameterName, float value, bool isGlobal = false)
        {
            if (!enableNetworkAudio || !IsOwner) return;

            // Apply locally
            if (isGlobal)
            {
                _audioManager?.SetParameter(parameterName, value);
            }
            else
            {
                _audioManager?.SetParameter(parameterName, value, gameObject);
            }

            // Send to other clients
            SyncParameterRpc(parameterName, value, isGlobal);
            
            if (debugNetworkAudio)
            {
                Debug.Log($"NetworkAudioSync: Synced parameter '{parameterName}' = {value} ({(isGlobal ? "global" : "local")}) on {gameObject.name}");
            }
        }

        /// <summary>
        /// Sync a switch across the network.
        /// </summary>
        /// <param name="switchGroup">Switch group name</param>
        /// <param name="switchValue">Switch value name</param>
        public void SyncSwitch(string switchGroup, string switchValue)
        {
            if (!enableNetworkAudio || !IsOwner) return;

            // Apply locally
            _audioManager?.SetSwitch(switchGroup, switchValue, gameObject);

            // Send to other clients
            SyncSwitchRpc(switchGroup, switchValue);
            
            if (debugNetworkAudio)
            {
                Debug.Log($"NetworkAudioSync: Synced switch '{switchGroup}' = '{switchValue}' on {gameObject.name}");
            }
        }

        /// <summary>
        /// Stop all audio events on this object across the network.
        /// </summary>
        public void StopAllNetworkAudio()
        {
            if (!enableNetworkAudio || !IsOwner) return;

            // Stop locally
            _audioManager?.StopAllEvents(gameObject);

            // Send to other clients
            StopAllAudioRpc();
            
            if (debugNetworkAudio)
            {
                Debug.Log($"NetworkAudioSync: Stopped all audio on {gameObject.name}");
            }
        }
        #endregion

        #region RPCs
        [Rpc(SendTo.NotOwner)]
        private void TriggerAudioEventRpc(string eventName)
        {
            if (!enableNetworkAudio || _audioManager == null) return;

            _audioManager.PostEvent(eventName, gameObject);
            
            if (debugNetworkAudio)
            {
                Debug.Log($"NetworkAudioSync: Received RPC to play '{eventName}' on {gameObject.name}");
            }
        }

        [Rpc(SendTo.NotOwner)]
        private void SyncParameterRpc(string parameterName, float value, bool isGlobal)
        {
            if (!enableNetworkAudio || _audioManager == null) return;

            if (isGlobal)
            {
                _audioManager.SetParameter(parameterName, value);
            }
            else
            {
                _audioManager.SetParameter(parameterName, value, gameObject);
            }
            
            if (debugNetworkAudio)
            {
                Debug.Log($"NetworkAudioSync: Received RPC to set parameter '{parameterName}' = {value} on {gameObject.name}");
            }
        }

        [Rpc(SendTo.NotOwner)]
        private void SyncSwitchRpc(string switchGroup, string switchValue)
        {
            if (!enableNetworkAudio || _audioManager == null) return;

            _audioManager.SetSwitch(switchGroup, switchValue, gameObject);
            
            if (debugNetworkAudio)
            {
                Debug.Log($"NetworkAudioSync: Received RPC to set switch '{switchGroup}' = '{switchValue}' on {gameObject.name}");
            }
        }

        [Rpc(SendTo.NotOwner)]
        private void StopAllAudioRpc()
        {
            if (!enableNetworkAudio || _audioManager == null) return;

            _audioManager.StopAllEvents(gameObject);
            
            if (debugNetworkAudio)
            {
                Debug.Log($"NetworkAudioSync: Received RPC to stop all audio on {gameObject.name}");
            }
        }
        #endregion

        #region Helper Methods
        private void PlayEventWithParameters(string eventName, AudioParameterData[] parameters)
        {
            if (_audioManager == null) return;

            // Set parameters first
            foreach (var param in parameters)
            {
                if (param.isGlobal)
                {
                    _audioManager.SetParameter(param.parameterName, param.value);
                }
                else
                {
                    _audioManager.SetParameter(param.parameterName, param.value, gameObject);
                }
            }

            // Then play the event
            _audioManager.PostEvent(eventName, gameObject);
        }
        #endregion

        #region Network Lifecycle
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (debugNetworkAudio)
            {
                Debug.Log($"NetworkAudioSync: Spawned on {gameObject.name} (IsOwner: {IsOwner})");
            }
        }

        public override void OnNetworkDespawn()
        {
            // Stop all audio when despawning
            if (_audioManager != null)
            {
                _audioManager.StopAllEvents(gameObject);
            }
            
            base.OnNetworkDespawn();
            
            if (debugNetworkAudio)
            {
                Debug.Log($"NetworkAudioSync: Despawned on {gameObject.name}");
            }
        }
        #endregion
    }
}