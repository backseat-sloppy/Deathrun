using Unity.Netcode;
using UnityEngine;

namespace DeathrunGame
{
    /// <summary>
    /// Handles air movement and jump mechanics.
    /// Only active when player is not grounded.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerStateStatus))]
    public class PlayerAirController : NetworkBehaviour
    {
        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private float jumpCooldown = 0.1f; // Prevent double-jump spam
        private float lastJumpTime;

        [Header("Air Control")]
        [SerializeField] private float airMoveSpeed = 3f; // Reduced control in air
        [SerializeField] private float airAcceleration = 10f; // How fast you reach air move speed

        [Header("Gravity")]
        [SerializeField] private float gravityMultiplier = 1f; // Extra gravity for snappier feel
        [SerializeField] private float maxFallSpeed = -20f; // Terminal velocity

        [Header("References")]
        [SerializeField] private PlayerCameraController cameraController;
        [SerializeField] private PlayerStateStatus stateStatus;

        private Rigidbody rb;
        private Vector3 airMoveInput;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            stateStatus = GetComponent<PlayerStateStatus>();
        }

        public override void OnNetworkSpawn()
        {
            // Auto-find camera controller if owner
            if (IsOwner && cameraController == null)
            {
                cameraController = GetComponent<PlayerCameraController>();
            }
        }

        /// <summary>
        /// Call this from PlayerMovementController.Update() to handle jump input
        /// </summary>
        public void HandleJumpInput(Vector3 moveInput)
        {
            if (!IsOwner) return;
            if (stateStatus.IsDead.Value) return;

            airMoveInput = moveInput;

            // Jump input
            if (Input.GetButtonDown("Jump") && CanJump())
            {
                Jump();
            }
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;
            if (stateStatus.IsDead.Value) return;

            // Only apply air movement when NOT grounded
            if (!stateStatus.IsGrounded.Value)
            {
                ApplyAirMovement();
                ApplyGravity();
                UpdateFallingState();
            }
        }

        private bool CanJump()
        {
            // Can jump if:
            // 1. Grounded (checked in coyote time)
            // 2. Cooldown has passed
            return stateStatus.IsGrounded.Value && 
                   Time.time >= lastJumpTime + jumpCooldown;
        }

        private void Jump()
        {
            // Apply upward force
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            
            // Update state
            stateStatus.SetJumping(true);
            stateStatus.SetFalling(false);
            
            lastJumpTime = Time.time;

            Debug.Log("🦘 Jump initiated");
        }

        private void ApplyAirMovement()
        {
            if (airMoveInput.magnitude > 0.1f)
            {
                // Gradual acceleration in air (feels better than instant speed change)
                Vector3 targetVelocity = airMoveInput * airMoveSpeed;
                Vector3 currentHorizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                
                Vector3 newHorizontal = Vector3.MoveTowards(
                    currentHorizontal,
                    targetVelocity,
                    airAcceleration * Time.fixedDeltaTime
                );

                rb.linearVelocity = new Vector3(newHorizontal.x, rb.linearVelocity.y, newHorizontal.z);
                
                stateStatus.SetCurrentSpeed(newHorizontal.magnitude);
            }
        }

        private void ApplyGravity()
        {
            // Apply extra gravity for snappier jumps
            if (rb.linearVelocity.y < 0)
            {
                rb.linearVelocity += Vector3.up * Physics.gravity.y * (gravityMultiplier - 1) * Time.fixedDeltaTime;
            }

            // Clamp fall speed
            if (rb.linearVelocity.y < maxFallSpeed)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, maxFallSpeed, rb.linearVelocity.z);
            }
        }

        private void UpdateFallingState()
        {
            // Set falling if moving downward and not jumping
            if (rb.linearVelocity.y < -0.1f && !stateStatus.IsJumping.Value)
            {
                stateStatus.SetFalling(true);
            }
        }

        /// <summary>
        /// Call this when landing to reset air state
        /// </summary>
        public void OnLanded()
        {
            stateStatus.SetJumping(false);
            stateStatus.SetFalling(false);
        }
    }
}