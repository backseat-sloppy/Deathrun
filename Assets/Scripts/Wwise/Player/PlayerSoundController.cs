using UnityEngine;
using Unity.Netcode;

namespace DeathrunGame.Audio
{
    /// <summary>
    /// Main player sound controller that handles all player-related audio events.
    /// Integrates with the player movement system and provides multiplayer-aware audio playback.
    /// Manages footsteps, jumping, landing, and death sounds with proper surface detection and height parameters.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerSoundController : NetworkBehaviour, IAudioSource, INetworkAudioSource
    {
        #region Serialized Fields
        [Header("Audio Configuration")]
        [SerializeField] protected bool enableAudio = true;
        [SerializeField] protected bool debugAudio = false;
        
        [Header("Footstep Settings")]
        [SerializeField] private bool useFootColliders = true;
        [SerializeField] private float footstepInterval = 0.5f; // Fallback if no foot colliders
        [SerializeField] private float minSpeedForFootsteps = 1f;
        [SerializeField] private LayerMask groundLayerMask = -1;
        
        [Header("Jump/Land Settings")]
        [SerializeField] protected float landHeightThreshold = 2f;
        [SerializeField] protected float maxLandHeight = 20f;
        
        [Header("Surface Detection")]
        [SerializeField] private float surfaceDetectionDistance = 1.5f;
        [SerializeField] private Vector3 surfaceDetectionOffset = Vector3.zero;
        #endregion

        #region Private Fields
        protected Rigidbody _rigidbody;
        private ISurfaceDetector _surfaceDetector;
        protected WwiseAudioManager _audioManager;
        
        // Foot collider system
        private FootstepCollider[] _footColliders;
        private bool _hasFootColliders = false;
        
        // State tracking
        private bool _wasGroundedLastFrame = false;
        private float _lastFootstepTime = 0f;
        protected float _jumpStartHeight = 0f;
        private Vector3 _lastVelocity = Vector3.zero;
        private SurfaceType _currentSurfaceType = SurfaceType.Default;
        
        // Audio IDs
        private uint _currentFootstepId = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
        private static uint _nextAudioSourceId = 1;
        private uint _audioSourceId;
        #endregion

        #region IAudioSource Implementation
        public GameObject AudioGameObject => gameObject;
        public bool IsAudioActive => enableAudio && gameObject.activeInHierarchy;
        public uint AudioSourceId => _audioSourceId;
        #endregion

        #region INetworkAudioSource Implementation
        public bool IsLocalPlayer => IsOwner;

        public virtual void OnNetworkAudioEvent(string eventName, AudioParameterData[] parameters = null)
        {
            if (!IsAudioActive) return;

            // Play the event locally for non-owner clients
            uint playingId = _audioManager.PostEvent(eventName, gameObject);

            // Apply any parameters
            if (parameters != null)
            {
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
            }

            if (debugAudio)
            {
                Debug.Log($"PlayerSoundController: Received network audio event '{eventName}' on {gameObject.name}");
            }
        }
        #endregion

        #region Unity Lifecycle
        protected virtual void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _audioSourceId = _nextAudioSourceId++;
            
            // Try to find surface detector on this object or parent
            _surfaceDetector = GetComponent<ISurfaceDetector>() ?? GetComponentInParent<ISurfaceDetector>();
        }

        protected virtual void Start()
        {
            _audioManager = WwiseAudioManager.Instance;
            
            if (_audioManager == null)
            {
                Debug.LogError("PlayerSoundController: WwiseAudioManager not found! Audio will not work.");
                enableAudio = false;
                return;
            }

            if (!_audioManager.IsInitialized())
            {
                Debug.LogWarning("PlayerSoundController: WwiseAudioManager not initialized yet. Audio may not work immediately.");
            }

            // Subscribe to surface detector events if available
            if (_surfaceDetector != null)
            {
                _surfaceDetector.OnSurfaceTypeChanged += OnSurfaceTypeChanged;
            }

            // Initialize foot colliders
            InitializeFootColliders();

            // Initialize surface type
            UpdateSurfaceType();
        }

        protected virtual void Update()
        {
            if (!IsAudioActive || !IsOwner) return;

            UpdateAudioState();
        }

        private void OnDestroy()
        {
            StopAllPlayerAudio();
            
            if (_surfaceDetector != null)
            {
                _surfaceDetector.OnSurfaceTypeChanged -= OnSurfaceTypeChanged;
            }

            // Unsubscribe from foot collider events
            if (_footColliders != null)
            {
                foreach (FootstepCollider footCollider in _footColliders)
                {
                    if (footCollider != null)
                    {
                        footCollider.OnFootstepTriggered -= OnFootstepTriggered;
                    }
                }
            }
        }
        #endregion

        #region Audio State Management
        private void UpdateAudioState()
        {
            bool isGrounded = IsGrounded();
            Vector3 currentVelocity = _rigidbody.linearVelocity;
            float horizontalSpeed = new Vector3(currentVelocity.x, 0, currentVelocity.z).magnitude;

            // Handle landing
            if (isGrounded && !_wasGroundedLastFrame)
            {
                HandleLanding();
            }
            
            // Handle jumping
            if (!isGrounded && _wasGroundedLastFrame)
            {
                HandleJumping();
            }
            
            // Handle footsteps
            if (isGrounded && horizontalSpeed > minSpeedForFootsteps)
            {
                // Only use fallback time-based footsteps if no foot colliders are available
                if (!_hasFootColliders)
                {
                    HandleFootsteps(horizontalSpeed);
                }
                // Foot colliders handle footsteps automatically when they are available
            }

            // Update parameters
            UpdateAudioParameters(horizontalSpeed);

            _wasGroundedLastFrame = isGrounded;
            _lastVelocity = currentVelocity;
        }

        private void HandleJumping()
        {
            _jumpStartHeight = transform.position.y;
            PlayAudioEvent(WwiseEvents.PLAYER_JUMP_PLAY);
            
            if (debugAudio)
            {
                Debug.Log($"PlayerSoundController: Jump started at height {_jumpStartHeight}");
            }
        }

        protected virtual void HandleLanding()
        {
            float landHeight = _jumpStartHeight - transform.position.y;
            landHeight = Mathf.Clamp(landHeight, 0f, maxLandHeight);
            
            if (landHeight >= landHeightThreshold)
            {
                // Set height parameter before playing land sound
                float normalizedHeight = landHeight / maxLandHeight;
                SetAudioParameter(WwiseParameters.GP_HEIGHT, normalizedHeight);
                
                PlayAudioEvent(WwiseEvents.PLAYER_LAND_PLAY);
                
                if (debugAudio)
                {
                    Debug.Log($"PlayerSoundController: Landed from height {landHeight} (normalized: {normalizedHeight})");
                }
            }
        }

        private void HandleFootsteps(float speed)
        {
            float timeSinceLastFootstep = Time.time - _lastFootstepTime;
            float adjustedInterval = footstepInterval / Mathf.Max(speed / 5f, 1f); // Faster footsteps when moving faster
            
            if (timeSinceLastFootstep >= adjustedInterval)
            {
                UpdateSurfaceType();
                PlayAudioEvent(WwiseEvents.PLAYER_FOOTSTEP_PLAY);
                _lastFootstepTime = Time.time;
                
                if (debugAudio)
                {
                    Debug.Log($"PlayerSoundController: Footstep on {_currentSurfaceType} surface (speed: {speed:F1})");
                }
            }
        }

        private void UpdateAudioParameters(float speed)
        {
            // Update player speed parameter
            SetAudioParameter(WwiseParameters.GP_PLAYER_SPEED, speed);
        }
        #endregion

        #region Foot Collider System
        private void InitializeFootColliders()
        {
            // Find all FootstepCollider components in children
            _footColliders = GetComponentsInChildren<FootstepCollider>();
            _hasFootColliders = _footColliders.Length > 0;
            
            if (_hasFootColliders)
            {
                // Subscribe to foot collider events
                foreach (FootstepCollider footCollider in _footColliders)
                {
                    footCollider.OnFootstepTriggered += OnFootstepTriggered;
                }
                
                if (debugAudio)
                {
                    Debug.Log($"PlayerSoundController: Found {_footColliders.Length} foot colliders. Using collision-based footsteps.");
                }
            }
            else if (useFootColliders)
            {
                if (debugAudio)
                {
                    Debug.LogWarning("PlayerSoundController: useFootColliders is enabled but no FootstepCollider components found in children. Falling back to time-based footsteps.");
                }
            }
        }

        private void OnFootstepTriggered(FootType footType, SurfaceType surfaceType, Vector3 position)
        {
            if (!IsAudioActive || !IsOwner) return;

            // Update current surface type
            if (surfaceType != _currentSurfaceType)
            {
                _currentSurfaceType = surfaceType;
                OnSurfaceTypeChanged(surfaceType);
            }

            // Play footstep sound
            PlayAudioEvent(WwiseEvents.PLAYER_FOOTSTEP_PLAY);
            
            if (debugAudio)
            {
                Debug.Log($"PlayerSoundController: {footType} footstep on {surfaceType} surface at {position}");
            }
        }

        /// <summary>
        /// Public method for FootstepCollider to trigger footstep sounds.
        /// </summary>
        /// <param name="footType">Which foot triggered the step</param>
        /// <param name="surfaceType">Surface type at contact point</param>
        /// <param name="position">World position of the footstep</param>
        public void TriggerFootstep(FootType footType, SurfaceType surfaceType, Vector3 position)
        {
            OnFootstepTriggered(footType, surfaceType, position);
        }
        #endregion

        #region Surface Detection
        private void UpdateSurfaceType()
        {
            SurfaceType detectedSurface = SurfaceType.Default;
            
            if (_surfaceDetector != null)
            {
                Vector3 detectionPoint = transform.position + surfaceDetectionOffset;
                detectedSurface = _surfaceDetector.GetSurfaceType(detectionPoint);
            }
            else
            {
                // Fallback: raycast to detect surface
                detectedSurface = DetectSurfaceTypeByRaycast();
            }

            if (detectedSurface != _currentSurfaceType)
            {
                _currentSurfaceType = detectedSurface;
                OnSurfaceTypeChanged(detectedSurface);
            }
        }

        private SurfaceType DetectSurfaceTypeByRaycast()
        {
            Vector3 rayStart = transform.position + surfaceDetectionOffset;
            Ray ray = new Ray(rayStart, Vector3.down);
            
            if (Physics.Raycast(ray, out RaycastHit hit, surfaceDetectionDistance, groundLayerMask))
            {
                // Try to get surface type from the hit object
                var surfaceProvider = hit.collider.GetComponent<SurfaceTypeProvider>();
                if (surfaceProvider != null)
                {
                    return surfaceProvider.SurfaceType;
                }
                
                // Fallback: determine by material name or tag
                return DetermineSurfaceTypeFromCollider(hit.collider);
            }
            
            return SurfaceType.Default;
        }

        private SurfaceType DetermineSurfaceTypeFromCollider(Collider collider)
        {
            // Simple tag-based detection
            string tag = collider.tag.ToLower();
            
            if (tag.Contains("concrete") || tag.Contains("stone"))
                return SurfaceType.Concrete;
            if (tag.Contains("dirt") || tag.Contains("ground"))
                return SurfaceType.Dirt;
            if (tag.Contains("metal"))
                return SurfaceType.Metal;
            if (tag.Contains("water"))
                return SurfaceType.Water;
            if (tag.Contains("wood"))
                return SurfaceType.Wood;
                
            return SurfaceType.Default;
        }

        protected virtual void OnSurfaceTypeChanged(SurfaceType newSurfaceType)
        {
            _currentSurfaceType = newSurfaceType;
            
            // Update Wwise switch
            _audioManager.SetSurfaceType(newSurfaceType, gameObject);
            
            if (debugAudio)
            {
                Debug.Log($"PlayerSoundController: Surface type changed to {newSurfaceType}");
            }
        }
        #endregion

        #region Ground Detection
        private bool IsGrounded()
        {
            Vector3 rayStart = transform.position + surfaceDetectionOffset;
            return Physics.Raycast(rayStart, Vector3.down, surfaceDetectionDistance, groundLayerMask);
        }
        #endregion

        #region Audio Playback
        protected virtual void PlayAudioEvent(string eventName)
        {
            if (!IsAudioActive || _audioManager == null) return;

            uint playingId = _audioManager.PostEvent(eventName, gameObject);
            
            // For multiplayer, we might want to sync certain events
            if (IsOwner && ShouldSyncEvent(eventName))
            {
                // TODO: Send network event for synchronization
                SendNetworkAudioEvent(eventName);
            }
        }

        protected virtual void SetAudioParameter(string parameterName, float value)
        {
            if (!IsAudioActive || _audioManager == null) return;

            _audioManager.SetParameter(parameterName, value, gameObject);
        }

        protected virtual bool ShouldSyncEvent(string eventName)
        {
            // Determine which events should be synchronized across network
            return eventName switch
            {
                WwiseEvents.PLAYER_DEATH_PLAY => true,
                WwiseEvents.PLAYER_JUMP_PLAY => true,
                WwiseEvents.PLAYER_LAND_PLAY => true,
                WwiseEvents.PLAYER_FOOTSTEP_PLAY => false, // Too frequent for networking
                _ => false
            };
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Manually trigger a death sound. Can be called from external scripts.
        /// </summary>
        public virtual void PlayDeathSound()
        {
            PlayAudioEvent(WwiseEvents.PLAYER_DEATH_PLAY);
            
            if (debugAudio)
            {
                Debug.Log("PlayerSoundController: Death sound triggered");
            }
        }

        /// <summary>
        /// Stop all audio events for this player.
        /// </summary>
        public virtual void StopAllPlayerAudio()
        {
            if (_audioManager != null)
            {
                _audioManager.StopAllEvents(gameObject);
            }
        }

        /// <summary>
        /// Enable or disable audio for this player.
        /// </summary>
        /// <param name="enabled">Whether audio should be enabled</param>
        public void SetAudioEnabled(bool enabled)
        {
            enableAudio = enabled;
            
            if (!enabled)
            {
                StopAllPlayerAudio();
            }
        }

        /// <summary>
        /// Force update the surface type (useful for debugging or external triggers).
        /// </summary>
        public void ForceUpdateSurfaceType()
        {
            UpdateSurfaceType();
        }

        /// <summary>
        /// Enable or disable foot collider system.
        /// </summary>
        /// <param name="enabled">Whether foot colliders should be used</param>
        public void SetFootCollidersEnabled(bool enabled)
        {
            useFootColliders = enabled;
            
            if (_footColliders != null)
            {
                foreach (FootstepCollider footCollider in _footColliders)
                {
                    if (footCollider != null)
                    {
                        footCollider.SetFootstepEnabled(enabled);
                    }
                }
            }
        }

        /// <summary>
        /// Get whether foot colliders are being used for footstep detection.
        /// </summary>
        /// <returns>True if foot colliders are active</returns>
        public bool IsUsingFootColliders()
        {
            return useFootColliders && _hasFootColliders;
        }

        /// <summary>
        /// Get all foot colliders attached to this player.
        /// </summary>
        /// <returns>Array of foot colliders</returns>
        public FootstepCollider[] GetFootColliders()
        {
            return _footColliders;
        }
        #endregion

        #region Network Methods (Placeholder)
        private void SendNetworkAudioEvent(string eventName)
        {
            // TODO: Implement network synchronization
            // This would send an RPC to other clients to play the same audio event
            if (debugAudio)
            {
                Debug.Log($"PlayerSoundController: Would sync audio event '{eventName}' across network");
            }
        }
        #endregion

        #region Debug
        private void OnDrawGizmosSelected()
        {
            if (debugAudio)
            {
                // Draw surface detection ray
                Gizmos.color = Color.yellow;
                Vector3 rayStart = transform.position + surfaceDetectionOffset;
                Gizmos.DrawRay(rayStart, Vector3.down * surfaceDetectionDistance);
                
                // Draw current surface type as text
                Gizmos.color = Color.white;
                Vector3 textPos = transform.position + Vector3.up * 2f;
                
#if UNITY_EDITOR
                UnityEditor.Handles.Label(textPos, $"Surface: {_currentSurfaceType}");
#endif
            }
        }
        #endregion
    }
}