using Unity.Netcode;
using UnityEngine;

namespace DeathrunGame.Audio
{
    /// <summary>
    /// Enhanced player sound controller with full multiplayer networking support.
    /// Extends the base PlayerSoundController with network audio synchronization.
    /// </summary>
    [RequireComponent(typeof(NetworkAudioSync))]
    public class NetworkPlayerSoundController : PlayerSoundController
    {
        #region Private Fields
        private NetworkAudioSync _networkAudioSync;
        
        [Header("Network Audio Settings")]
        [SerializeField] private bool syncFootsteps = false; // Usually too frequent for networking
        [SerializeField] private bool syncMovementParameters = true;
        [SerializeField] private float parameterSyncInterval = 0.1f; // Throttle parameter updates
        
        private float _lastParameterSyncTime = 0f;
        private float _lastSyncedSpeed = 0f;
        private float _lastSyncedHeight = 0f;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            _networkAudioSync = GetComponent<NetworkAudioSync>();
        }

        protected override void Update()
        {
            base.Update();
            
            // Handle network parameter synchronization
            if (IsOwner && syncMovementParameters && Time.time - _lastParameterSyncTime > parameterSyncInterval)
            {
                SyncMovementParameters();
                _lastParameterSyncTime = Time.time;
            }
        }
        #endregion

        #region Network Audio Methods
        protected override void PlayAudioEvent(string eventName)
        {
            if (!IsAudioActive) return;

            if (IsOwner)
            {
                // Check if this event should be synchronized
                if (ShouldSyncEvent(eventName))
                {
                    _networkAudioSync.TriggerNetworkAudioEvent(eventName, true);
                }
                else
                {
                    // Play only locally (e.g., footsteps)
                    WwiseAudioManager.Instance?.PostEvent(eventName, gameObject);
                }
            }
        }

        protected override void HandleLanding()
        {
            float landHeight = _jumpStartHeight - transform.position.y;
            landHeight = Mathf.Clamp(landHeight, 0f, maxLandHeight);
            
            if (landHeight >= landHeightThreshold)
            {
                float normalizedHeight = landHeight / maxLandHeight;
                
                if (IsOwner)
                {
                    // Create parameter data for network sync
                    AudioParameterData[] parameters = new AudioParameterData[]
                    {
                        new AudioParameterData(WwiseParameters.GP_HEIGHT, normalizedHeight, false)
                    };
                    
                    // Sync landing event with height parameter
                    _networkAudioSync.TriggerNetworkAudioEventWithParameters(
                        WwiseEvents.PLAYER_LAND_PLAY, 
                        parameters, 
                        true
                    );
                }
                
                if (debugAudio)
                {
                    Debug.Log($"NetworkPlayerSoundController: Networked landing from height {landHeight} (normalized: {normalizedHeight})");
                }
            }
        }

        private void SyncMovementParameters()
        {
            if (!IsOwner || !_networkAudioSync) return;

            Vector3 velocity = _rigidbody.linearVelocity;
            float currentSpeed = new Vector3(velocity.x, 0, velocity.z).magnitude;
            float currentHeight = transform.position.y;

            // Only sync if values changed significantly
            if (Mathf.Abs(currentSpeed - _lastSyncedSpeed) > 0.5f)
            {
                _networkAudioSync.SyncParameter(WwiseParameters.GP_PLAYER_SPEED, currentSpeed, false);
                _lastSyncedSpeed = currentSpeed;
            }

            if (Mathf.Abs(currentHeight - _lastSyncedHeight) > 0.5f)
            {
                _networkAudioSync.SyncParameter(WwiseParameters.GP_HEIGHT, currentHeight, false);
                _lastSyncedHeight = currentHeight;
            }
        }

        protected override void OnSurfaceTypeChanged(SurfaceType newSurfaceType)
        {
            base.OnSurfaceTypeChanged(newSurfaceType);
            
            // Sync surface type switch across network
            if (IsOwner && _networkAudioSync)
            {
                string switchValue = WwiseSwitches.GetSurfaceSwitch(newSurfaceType);
                _networkAudioSync.SyncSwitch(WwiseSwitches.SURFACE_TYPE_GROUP, switchValue);
            }
        }

        public override void PlayDeathSound()
        {
            if (IsOwner && _networkAudioSync)
            {
                // Death sound should always be synchronized
                _networkAudioSync.TriggerNetworkAudioEvent(WwiseEvents.PLAYER_DEATH_PLAY, true);
            }
            else if (!IsOwner)
            {
                // Non-owners can still trigger death sound locally (e.g., from server)
                base.PlayDeathSound();
            }
            
            if (debugAudio)
            {
                Debug.Log("NetworkPlayerSoundController: Network death sound triggered");
            }
        }

        public override void StopAllPlayerAudio()
        {
            base.StopAllPlayerAudio();
            
            if (IsOwner && _networkAudioSync)
            {
                _networkAudioSync.StopAllNetworkAudio();
            }
        }
        #endregion

        #region Network Event Handling
        public override void OnNetworkAudioEvent(string eventName, AudioParameterData[] parameters = null)
        {
            // This is called when receiving network audio events from other players
            if (!IsAudioActive || IsOwner) return; // Owners handle their own audio

            base.OnNetworkAudioEvent(eventName, parameters);
            
            if (debugAudio)
            {
                Debug.Log($"NetworkPlayerSoundController: Received network audio event '{eventName}' from remote player");
            }
        }
        #endregion

        #region Network Lifecycle
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // Initialize network audio sync
            if (_networkAudioSync == null)
            {
                _networkAudioSync = GetComponent<NetworkAudioSync>();
            }
        }

        public override void OnNetworkDespawn()
        {
            StopAllPlayerAudio();
            base.OnNetworkDespawn();
        }
        #endregion

        #region Public Network Methods
        /// <summary>
        /// Manually trigger a networked audio event. Can be called from external scripts.
        /// </summary>
        /// <param name="eventName">Wwise event name</param>
        /// <param name="syncAcrossNetwork">Whether to sync across network</param>
        public void TriggerNetworkedAudioEvent(string eventName, bool syncAcrossNetwork = true)
        {
            if (!IsOwner) return;

            if (syncAcrossNetwork && _networkAudioSync)
            {
                _networkAudioSync.TriggerNetworkAudioEvent(eventName, true);
            }
            else
            {
                WwiseAudioManager.Instance?.PostEvent(eventName, gameObject);
            }
        }

        /// <summary>
        /// Set whether footsteps should be synchronized across network.
        /// Warning: This can be bandwidth intensive.
        /// </summary>
        /// <param name="sync">Whether to sync footsteps</param>
        public void SetFootstepSync(bool sync)
        {
            syncFootsteps = sync;
        }

        /// <summary>
        /// Enable or disable movement parameter synchronization.
        /// </summary>
        /// <param name="sync">Whether to sync movement parameters</param>
        public void SetMovementParameterSync(bool sync)
        {
            syncMovementParameters = sync;
        }
        #endregion

        #region Protected Override Methods
        protected override bool ShouldSyncEvent(string eventName)
        {
            return eventName switch
            {
                WwiseEvents.PLAYER_DEATH_PLAY => true,
                WwiseEvents.PLAYER_JUMP_PLAY => true,
                WwiseEvents.PLAYER_LAND_PLAY => true,
                WwiseEvents.PLAYER_FOOTSTEP_PLAY => syncFootsteps,
                _ => false
            };
        }
        #endregion
    }
}