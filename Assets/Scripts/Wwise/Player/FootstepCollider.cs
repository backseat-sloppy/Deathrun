using UnityEngine;

namespace DeathrunGame.Audio
{
    /// <summary>
    /// Foot collider component that detects when a foot touches the ground.
    /// Triggers footstep audio events when collision occurs with proper surface detection.
    /// Should be attached to foot colliders (typically on ankle/foot bones).
    /// </summary>
    public class FootstepCollider : MonoBehaviour
    {
        [Header("Foot Settings")]
        [SerializeField] private FootType footType = FootType.Left;
        [SerializeField] private bool enableDebug = false;
        
        [Header("Collision Settings")]
        [SerializeField] private LayerMask groundLayerMask = -1;
        [SerializeField] private float minimumVelocityThreshold = 1f;
        [SerializeField] private float footstepCooldown = 0.1f; // Prevent rapid-fire footsteps
        
        [Header("Detection")]
        [SerializeField] private bool requireMovement = true;
        [SerializeField] private float movementThreshold = 0.5f;

        #region Private Fields
        private PlayerSoundController _playerSoundController;
        private Rigidbody _playerRigidbody;
        private float _lastFootstepTime = 0f;
        private bool _wasGroundedLastFrame = false;
        private Vector3 _lastPosition;
        #endregion

        #region Events
        public event System.Action<FootType, SurfaceType, Vector3> OnFootstepTriggered;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            InitializeFootstepCollider();
        }

        private void Update()
        {
            // Track position for movement detection
            _lastPosition = transform.position;
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleFootContact(other, true);
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleFootContact(collision.collider, false);
        }
        #endregion

        #region Initialization
        private void InitializeFootstepCollider()
        {
            // Find player sound controller in parent hierarchy
            _playerSoundController = GetComponentInParent<PlayerSoundController>();
            _playerRigidbody = GetComponentInParent<Rigidbody>();
            
            if (_playerSoundController == null)
            {
                Debug.LogWarning($"FootstepCollider: No PlayerSoundController found in parent hierarchy on {gameObject.name}");
            }

            if (_playerRigidbody == null)
            {
                Debug.LogWarning($"FootstepCollider: No Rigidbody found in parent hierarchy on {gameObject.name}");
            }

            // Ensure this GameObject has a collider
            Collider footCollider = GetComponent<Collider>();
            if (footCollider == null)
            {
                Debug.LogError($"FootstepCollider: No Collider component found on {gameObject.name}. Adding a trigger collider.");
                SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true;
                sphereCollider.radius = 0.1f;
            }

            _lastPosition = transform.position;
        }
        #endregion

        #region Foot Contact Handling
        private void HandleFootContact(Collider hitCollider, bool isTrigger)
        {
            // Check if this is a valid ground collision
            if (!IsValidGroundContact(hitCollider))
                return;

            // Check cooldown to prevent rapid-fire footsteps
            if (Time.time - _lastFootstepTime < footstepCooldown)
                return;

            // Check movement requirements
            if (requireMovement && !HasSufficientMovement())
                return;

            // Check velocity threshold
            if (_playerRigidbody != null && _playerRigidbody.linearVelocity.magnitude < minimumVelocityThreshold)
                return;

            // Detect surface type at contact point
            SurfaceType surfaceType = DetectSurfaceType(hitCollider, transform.position);

            // Trigger footstep
            TriggerFootstep(surfaceType, transform.position);

            _lastFootstepTime = Time.time;

            if (enableDebug)
            {
                Debug.Log($"FootstepCollider: {footType} foot stepped on {surfaceType} surface at {transform.position}");
            }
        }

        private bool IsValidGroundContact(Collider hitCollider)
        {
            // Check if the collider is in the ground layer mask
            int colliderLayer = 1 << hitCollider.gameObject.layer;
            return (groundLayerMask.value & colliderLayer) != 0;
        }

        private bool HasSufficientMovement()
        {
            if (_playerRigidbody == null) return true;

            Vector3 horizontalVelocity = new Vector3(_playerRigidbody.linearVelocity.x, 0, _playerRigidbody.linearVelocity.z);
            return horizontalVelocity.magnitude >= movementThreshold;
        }

        private SurfaceType DetectSurfaceType(Collider hitCollider, Vector3 contactPoint)
        {
            // First, check for SurfaceTypeProvider on the hit collider
            SurfaceTypeProvider provider = hitCollider.GetComponent<SurfaceTypeProvider>();
            if (provider != null)
            {
                return provider.SurfaceType;
            }

            // Fallback to player's surface detector if available
            if (_playerSoundController != null)
            {
                ISurfaceDetector surfaceDetector = _playerSoundController.GetComponent<ISurfaceDetector>();
                if (surfaceDetector != null)
                {
                    return surfaceDetector.GetSurfaceType(contactPoint);
                }
            }

            // Last resort: determine from material/tag
            return AudioUtils.GetSurfaceTypeFromMaterial(hitCollider.tag);
        }

        private void TriggerFootstep(SurfaceType surfaceType, Vector3 position)
        {
            // Trigger footstep through player sound controller
            if (_playerSoundController != null)
            {
                _playerSoundController.TriggerFootstep(footType, surfaceType, position);
            }

            // Fire event for other systems
            OnFootstepTriggered?.Invoke(footType, surfaceType, position);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Manually trigger a footstep (useful for animation events).
        /// </summary>
        public void ManualTriggerFootstep()
        {
            if (Time.time - _lastFootstepTime < footstepCooldown)
                return;

            // Detect surface at current position
            Collider[] groundColliders = Physics.OverlapSphere(transform.position, 0.2f, groundLayerMask);
            if (groundColliders.Length > 0)
            {
                SurfaceType surfaceType = DetectSurfaceType(groundColliders[0], transform.position);
                TriggerFootstep(surfaceType, transform.position);
                _lastFootstepTime = Time.time;
            }
        }

        /// <summary>
        /// Set the foot type for this collider.
        /// </summary>
        /// <param name="newFootType">New foot type</param>
        public void SetFootType(FootType newFootType)
        {
            footType = newFootType;
        }

        /// <summary>
        /// Enable or disable this footstep collider.
        /// </summary>
        /// <param name="enabled">Whether the collider should be enabled</param>
        public void SetFootstepEnabled(bool enabled)
        {
            Collider footCollider = GetComponent<Collider>();
            if (footCollider != null)
            {
                footCollider.enabled = enabled;
            }
        }

        /// <summary>
        /// Set movement threshold for footstep triggering.
        /// </summary>
        /// <param name="threshold">Minimum movement speed to trigger footsteps</param>
        public void SetMovementThreshold(float threshold)
        {
            movementThreshold = threshold;
        }
        #endregion

        #region Debug
        private void OnDrawGizmosSelected()
        {
            if (enableDebug)
            {
                // Draw detection sphere
                Gizmos.color = footType == FootType.Left ? Color.red : Color.blue;
                Gizmos.DrawWireSphere(transform.position, 0.2f);
                
                // Draw foot type label
                Gizmos.color = Color.white;
                
#if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position + Vector3.up * 0.3f, $"{footType} Foot");
#endif
            }
        }
        #endregion
    }

    /// <summary>
    /// Enum representing which foot this collider belongs to.
    /// </summary>
    public enum FootType
    {
        Left,
        Right
    }
}