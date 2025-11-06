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
        [SerializeField] private float jumpStateDuration = 0.2f; // How long to keep IsJumping true
        
        [Header("Fall Detection")]
        [SerializeField] private float fallVelocityThreshold = -0.5f; // Y velocity below this triggers falling
        
        [Header("References")]
        [SerializeField] private PlayerCameraController cameraController;
        [SerializeField] private PlayerStateStatus stateStatus;

        private Rigidbody rb;
        private Vector3 moveInput;
        private float jumpStateTimer; // Tracks time since jump started

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
            if (!IsOwner) return;

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

            // Jump input (only when grounded and not currently jumping)
            if (Input.GetButtonDown("Jump") && stateStatus.IsGrounded.Value && !stateStatus.IsJumping.Value)
            {
                Jump();
            }
        }

        private void FixedUpdate()
        {
            // Apply movement in FixedUpdate for physics
            if (!IsOwner) return;

            // Don't move if dead
            if (stateStatus.IsDead.Value) return;

            ApplyMovement();
            UpdateAirState();
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
            
            // ✅ Set jumping state
            stateStatus.SetJumping(true);
            stateStatus.SetGrounded(false); // Immediately unground
            stateStatus.SetFalling(false);
            
            // Start jump timer
            jumpStateTimer = jumpStateDuration;

            Debug.Log("🦘 Jump initiated");
        }

        private void UpdateAirState()
        {
            // ✅ Get current Y velocity
            float yVelocity = rb.linearVelocity.y;

            // ✅ Handle jump state timer
            if (stateStatus.IsJumping.Value)
            {
                jumpStateTimer -= Time.fixedDeltaTime;
                
                // End jump state after duration OR if moving downward
                if (jumpStateTimer <= 0f || yVelocity < 0f)
                {
                    stateStatus.SetJumping(false);
                    Debug.Log("⬇️ Jump state ended");
                }
            }

            // ✅ Reset states when grounded
            if (stateStatus.IsGrounded.Value)
            {
                // Only reset if we were in the air
                if (stateStatus.IsFalling.Value || stateStatus.IsJumping.Value)
                {
                    stateStatus.SetJumping(false);
                    stateStatus.SetFalling(false);
                    Debug.Log("🟢 Landed - reset air states");
                }
            }
            // ✅ Set falling ONLY if Y velocity is negative and not jumping
            else if (yVelocity < fallVelocityThreshold && !stateStatus.IsJumping.Value)
            {
                if (!stateStatus.IsFalling.Value)
                {
                    stateStatus.SetFalling(true);
                    Debug.Log($"⬇️ Started falling (Y velocity: {yVelocity:F2})");
                }
            }
            // ✅ Clear falling if moving upward (edge case: bounced or hit from below)
            else if (yVelocity >= 0f && stateStatus.IsFalling.Value && !stateStatus.IsJumping.Value)
            {
                stateStatus.SetFalling(false);
                Debug.Log($"⬆️ Stopped falling (Y velocity: {yVelocity:F2})");
            }
        }
    }
}