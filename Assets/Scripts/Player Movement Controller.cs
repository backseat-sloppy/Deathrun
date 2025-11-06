using Unity.Netcode;
using UnityEngine;

namespace DeathrunGame
{
    /// <summary>
    /// Camera-relative rigidbody movement controller.
    /// Owner-only input with physics-based movement.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerStateStatus))]
    public class PlayerMovementController : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float airMoveSpeed = 3f; // Reduced air control

        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 5f;

        [Header("References")]
        [SerializeField] private PlayerCameraController cameraController;
        [SerializeField] private PlayerStateStatus stateStatus;

        private Rigidbody rb;
        private Vector3 moveInput;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            stateStatus = GetComponent<PlayerStateStatus>();

            // Configure Rigidbody for character movement
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        public override void OnNetworkSpawn()
        {
            // Auto-find camera controller if owner
            if (IsOwner && cameraController == null)
            {
                cameraController = GetComponent<PlayerCameraController>();
            }
        }

        private void Update()
        {
            // Owner-only input capture
            if (!IsOwner) return; // ✅ FIXED - just check IsOwner

            // Don't process input if dead
            if (stateStatus.IsDead.Value) return;

            // Get WASD input
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            // Calculate movement direction relative to camera
            if (cameraController != null)
            {
                Vector3 forward = cameraController.GetCameraForward();
                Vector3 right = cameraController.GetCameraRight();

                moveInput = (forward * vertical + right * horizontal).normalized;
            }
            else
            {
                moveInput = new Vector3(horizontal, 0f, vertical).normalized;
            }

            // Update movement state
            stateStatus.SetMoving(moveInput.magnitude > 0.1f);

            // DEBUG: Check jump state
            if (Input.GetButtonDown("Jump"))
            {
                Debug.Log($"🎮 Jump pressed! IsGrounded: {stateStatus.IsGrounded.Value}");
            }

            // Jump input
            if (Input.GetButtonDown("Jump") && stateStatus.IsGrounded.Value)
            {
                Jump();
            }
        }

        private void FixedUpdate()
        {
            // Apply movement in FixedUpdate for physics
            if (!IsOwner) return; // ✅ FIXED - just check IsOwner

            // Don't move if dead
            if (stateStatus.IsDead.Value) return;

            ApplyMovement();
            UpdateFallingState();
        }

        private void ApplyMovement()
        {
            if (moveInput.magnitude > 0.1f)
            {
                // Use different speed based on grounded state
                float currentMoveSpeed = stateStatus.IsGrounded.Value ? moveSpeed : airMoveSpeed;

                Vector3 movement = moveInput * currentMoveSpeed;
                rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);

                // Update speed for animations
                stateStatus.SetCurrentSpeed(movement.magnitude);
            }
            else
            {
                // Stop horizontal movement when no input (only if grounded)
                if (stateStatus.IsGrounded.Value)
                {
                    rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                }

                stateStatus.SetCurrentSpeed(0f);
            }
        }

        private void Jump()
        {
            // Apply upward force
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            
            // Update state
            stateStatus.SetJumping(true);
            stateStatus.SetFalling(false);

            Debug.Log("🦘 Jump initiated");
        }

        private void UpdateFallingState()
        {
            // Reset jumping state when grounded
            if (stateStatus.IsGrounded.Value)
            {
                stateStatus.SetJumping(false);
                stateStatus.SetFalling(false);
            }
            // Set falling if moving downward and not jumping
            else if (rb.linearVelocity.y < -0.1f && !stateStatus.IsJumping.Value)
            {
                stateStatus.SetFalling(true);
            }
        }
    }
}