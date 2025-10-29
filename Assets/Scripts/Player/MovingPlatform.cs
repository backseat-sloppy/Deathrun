using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace DeathrunGame
{
    /// <summary>
    /// Example implementation of IMovingPlatform using transform delta calculations.
    /// Supports linear movement, rotation, teleportation detection, and smooth networking.
    /// Runs in FixedUpdate for consistent physics integration.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MovingPlatform : NetworkBehaviour, IMovingPlatform
    {
        #region Serialized Fields
        
        [Header("Platform Configuration")]
        [SerializeField] private bool isActive = true;
        [SerializeField] private int priority = 0;
        [SerializeField] private LayerMask affectedLayers = -1;
        
        [Header("Movement Limits")]
        [SerializeField] private float maxLinearSpeed = 20f;
        [SerializeField] private float maxAngularSpeed = 180f;
        [SerializeField] private float teleportThreshold = 5f;
        
        [Header("Smoothing")]
        [SerializeField] private bool smoothVelocity = true;
        [SerializeField] private float velocitySmoothTime = 0.1f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool showGizmos = true;
        
        #endregion

        #region Private Fields
        
        // Position tracking
        private Vector3 previousPosition;
        private Vector3 currentPosition;
        private Vector3 calculatedLinearVelocity;
        private Vector3 smoothedLinearVelocity;
        private Vector3 linearVelocityDampVel;
        
        // Rotation tracking
        private Quaternion previousRotation;
        private Quaternion currentRotation;
        private Vector3 calculatedAngularVelocity;
        private Vector3 smoothedAngularVelocity;
        private Vector3 angularVelocityDampVel;
        
        // State tracking
        private bool hasTeleportedThisFrame = false;
        private bool isFirstFrame = true;
        
        // Character tracking
        private readonly HashSet<GameObject> charactersOnPlatform = new HashSet<GameObject>();
        
        // Components
        private new Collider collider;
        
        #endregion

        #region IMovingPlatform Implementation
        
        public Vector3 CurrentLinearVelocity => smoothVelocity ? smoothedLinearVelocity : calculatedLinearVelocity;
        public Vector3 CurrentAngularVelocity => smoothVelocity ? smoothedAngularVelocity : calculatedAngularVelocity;
        public bool IsActive => isActive;
        public int Priority => priority;
        public bool HasTeleportedThisFrame => hasTeleportedThisFrame;

        public Vector3 GetSurfaceNormal(Vector3 contactPoint)
        {
            // For simple platforms, return world up
            // More complex platforms could raycast or use mesh normals
            return transform.up;
        }

        public Vector3 GetVelocityAtPoint(Vector3 worldPoint)
        {
            Vector3 linearVel = CurrentLinearVelocity;
            Vector3 angularVel = CurrentAngularVelocity;
            
            if (angularVel.magnitude > 0.01f)
            {
                // Calculate velocity due to rotation
                Vector3 radiusVector = worldPoint - transform.position;
                Vector3 rotationalVelocity = Vector3.Cross(angularVel * Mathf.Deg2Rad, radiusVector);
                return linearVel + rotationalVelocity;
            }
            
            return linearVel;
        }

        public virtual void OnCharacterEnter(GameObject character)
        {
            if (charactersOnPlatform.Add(character))
            {
                Debug.Log($"Character {character.name} entered platform {name}");
            }
        }

        public virtual void OnCharacterExit(GameObject character)
        {
            if (charactersOnPlatform.Remove(character))
            {
                Debug.Log($"Character {character.name} exited platform {name}");
            }
        }

        #endregion

        #region Unity Lifecycle
        
        protected virtual void Awake()
        {
            collider = GetComponent<Collider>();
            
            // Initialize tracking variables
            previousPosition = currentPosition = transform.position;
            previousRotation = currentRotation = transform.rotation;
        }

        protected virtual void Start()
        {
            // Ensure the platform has a trigger collider for detection
            if (collider != null && !collider.isTrigger)
            {
                Debug.LogWarning($"MovingPlatform {name} should have a trigger collider for character detection. " +
                               "Consider adding a separate trigger collider as a child object.");
            }
        }

        protected virtual void Update()
        {
            // Base class doesn't need Update, but derived classes might
        }

        protected virtual void FixedUpdate()
        {
            UpdatePlatformTracking();
        }

        #endregion

        #region Platform Tracking
        
        private void UpdatePlatformTracking()
        {
            // Store previous values
            previousPosition = currentPosition;
            previousRotation = currentRotation;
            
            // Get current values
            currentPosition = transform.position;
            currentRotation = transform.rotation;
            
            if (isFirstFrame)
            {
                // Skip velocity calculation on first frame
                isFirstFrame = false;
                calculatedLinearVelocity = Vector3.zero;
                calculatedAngularVelocity = Vector3.zero;
                smoothedLinearVelocity = Vector3.zero;
                smoothedAngularVelocity = Vector3.zero;
                hasTeleportedThisFrame = false;
                return;
            }
            
            // Calculate linear velocity
            Vector3 deltaPosition = currentPosition - previousPosition;
            Vector3 newLinearVelocity = deltaPosition / Time.fixedDeltaTime;
            
            // Check for teleportation
            hasTeleportedThisFrame = deltaPosition.magnitude > teleportThreshold;
            
            if (hasTeleportedThisFrame)
            {
                // Platform teleported - don't apply this velocity to players
                calculatedLinearVelocity = Vector3.zero;
                smoothedLinearVelocity = Vector3.zero;
                Debug.Log($"Platform {name} teleported: {deltaPosition.magnitude:F2} units");
            }
            else
            {
                // Clamp velocity to maximum
                if (newLinearVelocity.magnitude > maxLinearSpeed)
                {
                    newLinearVelocity = newLinearVelocity.normalized * maxLinearSpeed;
                }
                
                calculatedLinearVelocity = newLinearVelocity;
            }
            
            // Calculate angular velocity
            Quaternion deltaRotation = currentRotation * Quaternion.Inverse(previousRotation);
            Vector3 newAngularVelocity = GetAngularVelocityFromQuaternion(deltaRotation) / Time.fixedDeltaTime;
            
            // Clamp angular velocity
            if (newAngularVelocity.magnitude > maxAngularSpeed)
            {
                newAngularVelocity = newAngularVelocity.normalized * maxAngularSpeed;
            }
            
            calculatedAngularVelocity = newAngularVelocity;
            
            // Apply smoothing if enabled
            if (smoothVelocity)
            {
                smoothedLinearVelocity = Vector3.SmoothDamp(smoothedLinearVelocity, calculatedLinearVelocity, 
                    ref linearVelocityDampVel, velocitySmoothTime);
                    
                smoothedAngularVelocity = Vector3.SmoothDamp(smoothedAngularVelocity, calculatedAngularVelocity, 
                    ref angularVelocityDampVel, velocitySmoothTime);
            }
        }

        private Vector3 GetAngularVelocityFromQuaternion(Quaternion deltaRotation)
        {
            // Convert quaternion to axis-angle representation
            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
            
            // Handle the quaternion double-cover (shortest path)
            if (angle > 180f)
            {
                angle -= 360f;
            }
            
            // Convert to degrees per second
            return axis * angle;
        }

        #endregion

        #region Public Interface
        
        /// <summary>
        /// Manually set the platform as active/inactive
        /// </summary>
        /// <param name="active">Whether the platform should affect players</param>
        public void SetActive(bool active)
        {
            isActive = active;
        }

        /// <summary>
        /// Set the platform priority
        /// </summary>
        /// <param name="newPriority">Priority value (higher = more important)</param>
        public void SetPriority(int newPriority)
        {
            priority = newPriority;
        }

        /// <summary>
        /// Teleport the platform instantly without affecting players
        /// </summary>
        /// <param name="newPosition">New position</param>
        /// <param name="newRotation">New rotation</param>
        public void TeleportTo(Vector3 newPosition, Quaternion newRotation)
        {
            transform.position = newPosition;
            transform.rotation = newRotation;
            
            // Update tracking to reflect teleport
            previousPosition = currentPosition = newPosition;
            previousRotation = currentRotation = newRotation;
            hasTeleportedThisFrame = true;
            
            // Reset velocities
            calculatedLinearVelocity = Vector3.zero;
            calculatedAngularVelocity = Vector3.zero;
            smoothedLinearVelocity = Vector3.zero;
            smoothedAngularVelocity = Vector3.zero;
            
            Debug.Log($"Platform {name} teleported to {newPosition}");
        }

        /// <summary>
        /// Get all characters currently on this platform
        /// </summary>
        /// <returns>Collection of character GameObjects</returns>
        public IReadOnlyCollection<GameObject> GetCharactersOnPlatform()
        {
            return charactersOnPlatform;
        }

        /// <summary>
        /// Force clear all character tracking (useful for platform resets)
        /// </summary>
        public void ClearCharacterTracking()
        {
            charactersOnPlatform.Clear();
        }

        #endregion

        #region Collision Detection
        
        private void OnTriggerEnter(Collider other)
        {
            if (ShouldAffectCollider(other))
            {
                OnCharacterEnter(other.gameObject);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (ShouldAffectCollider(other))
            {
                OnCharacterExit(other.gameObject);
            }
        }

        private bool ShouldAffectCollider(Collider other)
        {
            // Check if the collider is on an affected layer
            return (affectedLayers.value & (1 << other.gameObject.layer)) != 0;
        }

        #endregion

        #region Network Synchronization
        
        public override void OnNetworkSpawn()
        {
            // Platform should be synchronized across all clients
            // The server is authoritative for platform movement
            if (!IsServer)
            {
                // Clients follow server position but still calculate local velocities
                enabled = false; // Disable FixedUpdate on clients
            }
        }

        /// <summary>
        /// Network RPC to synchronize platform teleportation
        /// </summary>
        /// <param name="newPosition">New position</param>
        /// <param name="newRotation">New rotation</param>
        [ClientRpc]
        public void TeleportPlatformClientRpc(Vector3 newPosition, Quaternion newRotation)
        {
            if (!IsServer)
            {
                TeleportTo(newPosition, newRotation);
            }
        }

        #endregion

        #region Animation Support
        
        /// <summary>
        /// Called by Animation Events to trigger teleportation
        /// Useful for scripted sequences or cutscenes
        /// </summary>
        public void AnimationTeleport()
        {
            // Mark as teleported to prevent velocity calculation
            hasTeleportedThisFrame = true;
            
            if (IsServer)
            {
                TeleportPlatformClientRpc(transform.position, transform.rotation);
            }
        }

        #endregion

        #region Debug and Visualization
        
        protected virtual void OnDrawGizmos()
        {
            if (!showGizmos) return;
            
            // Draw platform bounds
            Gizmos.color = isActive ? Color.green : Color.red;
            if (collider != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                
                if (collider is BoxCollider boxCollider)
                {
                    Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
                }
                else if (collider is SphereCollider sphereCollider)
                {
                    Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
                }
                else
                {
                    Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                }
                
                Gizmos.matrix = Matrix4x4.identity;
            }
            
            // Draw velocity vectors
            if (Application.isPlaying)
            {
                Vector3 basePos = transform.position + Vector3.up * 2f;
                
                // Linear velocity
                if (CurrentLinearVelocity.magnitude > 0.1f)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(basePos, CurrentLinearVelocity * 0.5f);
                }
                
                // Angular velocity representation
                if (CurrentAngularVelocity.magnitude > 0.1f)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawRay(basePos + Vector3.up * 0.5f, CurrentAngularVelocity.normalized * 2f);
                }
            }
        }

        protected virtual void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;
            
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 3f);
            
            if (screenPos.z > 0)
            {
                Rect labelRect = new Rect(screenPos.x - 100, Screen.height - screenPos.y - 60, 200, 60);
                
                GUI.color = Color.white;
                GUI.Box(labelRect, "");
                
                GUI.color = Color.black;
                GUI.Label(labelRect, $"Platform: {name}\n" +
                                    $"Linear: {CurrentLinearVelocity.magnitude:F1} m/s\n" +
                                    $"Angular: {CurrentAngularVelocity.magnitude:F1} °/s\n" +
                                    $"Characters: {charactersOnPlatform.Count}");
            }
        }

        #endregion

        #region Editor Utilities
        
        #if UNITY_EDITOR
        [ContextMenu("Test Teleport")]
        private void TestTeleport()
        {
            Vector3 testPos = transform.position + Vector3.up * 5f + Vector3.right * 5f;
            TeleportTo(testPos, transform.rotation);
        }

        private void OnValidate()
        {
            // Clamp values in editor
            maxLinearSpeed = Mathf.Max(0f, maxLinearSpeed);
            maxAngularSpeed = Mathf.Max(0f, maxAngularSpeed);
            teleportThreshold = Mathf.Max(0.1f, teleportThreshold);
            velocitySmoothTime = Mathf.Max(0f, velocitySmoothTime);
        }
        #endif

        #endregion
    }
}