using Unity.Netcode;
using UnityEngine;

namespace DeathrunGame
{
    /// <summary>
    /// Minimal rigidbody-based movement controller for network testing.
    /// Owner-only input with Rigidbody physics.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerMovementController : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f;

        private Rigidbody rb;
        private Vector3 moveInput;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();

            // Configure Rigidbody for character movement
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        public override void OnNetworkSpawn()
        {
            // Only enable input for the owner
            if (!IsOwner)
            {
                enabled = false;
                return;
            }
        }

        private void Update()
        {
            // Owner-only input capture
            if (!IsOwner) return;

            // Get WASD input
            float horizontal = Input.GetAxisRaw("Horizontal"); // A/D
            float vertical = Input.GetAxisRaw("Vertical");     // W/S

            moveInput = new Vector3(horizontal, 0f, vertical).normalized;
        }

        private void FixedUpdate()
        {
            // Apply movement in FixedUpdate for physics
            if (!IsOwner) return;

            if (moveInput.magnitude > 0.1f)
            {
                // Move the rigidbody
                Vector3 movement = moveInput * moveSpeed;
                rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);

                // Rotate to face movement direction
                Quaternion targetRotation = Quaternion.LookRotation(moveInput);
                rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
            else
            {
                // Stop horizontal movement when no input
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }
        }
    }
}