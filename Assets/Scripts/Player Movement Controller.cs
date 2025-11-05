using Unity.Netcode;
using UnityEngine;

namespace DeathrunGame
{
    /// <summary>
    /// Camera-relative rigidbody movement controller.
    /// Owner-only input with physics-based movement.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerMovementController : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("References")]
        [SerializeField] private PlayerCameraController cameraController;

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

            // Auto-find camera controller if not set
            if (cameraController == null)
            {
                cameraController = GetComponent<PlayerCameraController>();
            }
        }

        private void Update()
        {
            // Owner-only input capture
            if (!IsOwner) return;

            // Get WASD input
            float horizontal = Input.GetAxisRaw("Horizontal"); // A/D
            float vertical = Input.GetAxisRaw("Vertical");     // W/S

            // Calculate movement direction relative to camera
            if (cameraController != null)
            {
                Vector3 forward = cameraController.GetCameraForward();
                Vector3 right = cameraController.GetCameraRight();

                moveInput = (forward * vertical + right * horizontal).normalized;
            }
            else
            {
                // Fallback to world-space movement if no camera
                moveInput = new Vector3(horizontal, 0f, vertical).normalized;
            }
        }

        private void FixedUpdate()
        {
            // Apply movement in FixedUpdate for physics
            if (!IsOwner) return;

            if (moveInput.magnitude > 0.1f)
            {
                // Move the rigidbody (preserve vertical velocity for gravity)
                Vector3 movement = moveInput * moveSpeed;
                rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);
            }
            else
            {
                // Stop horizontal movement when no input
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }
        }
    }
}