using Unity.Netcode;
using UnityEngine;

namespace DeathrunGame
{
    /// <summary>
    /// Handles ground detection for the player using raycasts.
    /// Updates PlayerStateStatus with ground contact information.
    /// Owner-only ground checking.
    /// </summary>
    [RequireComponent(typeof(PlayerStateStatus))]
    public class PlayerGroundCheck : NetworkBehaviour
    {
        [Header("Ground Check Settings")]
        [SerializeField] private float groundCheckDistance = 0.3f;
        [SerializeField] private LayerMask groundLayers;
        
        [Header("Raycast Points")]
        [Tooltip("Use the character's center if null")]
        [SerializeField] private Transform groundCheckOrigin;
        [SerializeField] private float raycastSpread = 0.3f; // Distance from center for corner rays
        [SerializeField] private bool useMultipleRays = true; // 5 rays vs single center ray

        [Header("Coyote Time")]
        [SerializeField] private float coyoteTime = 0.1f; // Grace period after leaving ground
        private float coyoteTimeCounter;

        [Header("Debug Visualization")]
        [SerializeField] private bool showDebugRays = true;

        private PlayerStateStatus stateStatus;

        private void Awake()
        {
            stateStatus = GetComponent<PlayerStateStatus>();
        }

        private void Update()
        {
            // Only owner performs ground check
            if (!IsOwner) return;

            bool isGrounded = CheckGrounded();

            // Coyote time: brief grace period after leaving ground
            if (isGrounded)
            {
                coyoteTimeCounter = coyoteTime;
            }
            else
            {
                coyoteTimeCounter -= Time.deltaTime;
            }

            // Update state (includes coyote time)
            bool effectivelyGrounded = isGrounded || coyoteTimeCounter > 0f;
            stateStatus.SetGrounded(effectivelyGrounded);
        }

        /// <summary>
        /// Performs raycast(s) to check if the player is grounded.
        /// Uses 5 rays in a cross pattern for reliable edge detection.
        /// </summary>
        private bool CheckGrounded()
        {
            Vector3 origin = groundCheckOrigin != null 
                ? groundCheckOrigin.position 
                : transform.position;

            if (useMultipleRays)
            {
                // 5-point raycast check (center + 4 corners in a cross pattern)
                // This catches edges and slopes better than a single raycast
                return CheckRaycast(origin) || // Center
                       CheckRaycast(origin + transform.right * raycastSpread) || // Right
                       CheckRaycast(origin - transform.right * raycastSpread) || // Left
                       CheckRaycast(origin + transform.forward * raycastSpread) || // Forward
                       CheckRaycast(origin - transform.forward * raycastSpread); // Back
            }
            else
            {
                // Single center raycast (most performant, but less reliable on edges)
                return CheckRaycast(origin);
            }
        }

        /// <summary>
        /// Performs a single raycast check from the given origin point
        /// </summary>
        private bool CheckRaycast(Vector3 origin)
        {
            bool hit = Physics.Raycast(
                origin,
                Vector3.down,
                groundCheckDistance,
                groundLayers,
                QueryTriggerInteraction.Ignore
            );

            // Debug visualization
            if (showDebugRays && IsOwner)
            {
                Debug.DrawRay(origin, Vector3.down * groundCheckDistance, hit ? Color.green : Color.red);
            }

            return hit;
        }

        // Visualize raycast origins in editor
        private void OnDrawGizmosSelected()
        {
            Vector3 origin = groundCheckOrigin != null 
                ? groundCheckOrigin.position 
                : transform.position;

            // Draw raycast origin points
            Gizmos.color = Color.yellow;
            
            if (useMultipleRays)
            {
                // Center
                Gizmos.DrawWireSphere(origin, 0.05f);
                // Right
                Gizmos.DrawWireSphere(origin + transform.right * raycastSpread, 0.05f);
                // Left
                Gizmos.DrawWireSphere(origin - transform.right * raycastSpread, 0.05f);
                // Forward
                Gizmos.DrawWireSphere(origin + transform.forward * raycastSpread, 0.05f);
                // Back
                Gizmos.DrawWireSphere(origin - transform.forward * raycastSpread, 0.05f);
            }
            else
            {
                Gizmos.DrawWireSphere(origin, 0.05f);
            }

            // Draw ground check distance
            Gizmos.color = stateStatus != null && stateStatus.IsGrounded.Value ? Color.green : Color.red;
            Gizmos.DrawLine(origin, origin + Vector3.down * groundCheckDistance);
        }
    }
}