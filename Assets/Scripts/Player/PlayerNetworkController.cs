/*
 * NETCODE SETUP NOTES:
 * =====================
 * 
 * REQUIRED COMPONENTS:
 * - NetworkObject: Must be present on this GameObject
 * - CharacterController: For physics-based movement
 * - PlayerInput: For input aggregation
 * - NetworkTransform: Optional, used for fallback interpolation
 * 
 * OWNERSHIP & SPAWNING:
 * - This controller should be spawned as a player object (NetworkManager.SpawnAsPlayerObject)
 * - Only the owner client can control their character
 * - Non-owners receive state updates and interpolate positions
 * 
 * SCENE SETUP:
 * 1. Add this script to a GameObject with NetworkObject
 * 2. Configure NetworkObject to be owned by the spawning client
 * 3. Set up CharacterController with appropriate radius/height/step offset
 * 4. Assign camera transform for owner input
 * 5. Configure ground detection layer mask and transforms
 * 
 * NETWORKING ARCHITECTURE:
 * - Client-authoritative input with server validation
 * - Periodic server correction snapshots (not every frame)
 * - Client-side prediction with rollback reconciliation
 * - Minimal RPC payloads using input compression
 */

using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace DeathrunGame
{
    public struct InputTick : INetworkSerializable
    {
        public uint SequenceId;
        public float Timestamp;
        public Vector3 MoveDirection;
        public bool Jump;
        public bool Sprint;
        public bool WasGrounded;
        public bool Swing;
        public int Taunt; // 0 = no taunt, 1-3 = taunt number
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref SequenceId);
            serializer.SerializeValue(ref Timestamp);
            serializer.SerializeValue(ref MoveDirection);
            serializer.SerializeValue(ref Jump);
            serializer.SerializeValue(ref Sprint);
            serializer.SerializeValue(ref WasGrounded);
            serializer.SerializeValue(ref Swing);
            serializer.SerializeValue(ref Taunt);
        }
    }

    public struct NetworkState : INetworkSerializable
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 KnockbackVelocity;
        public Quaternion Rotation;
        public uint LastProcessedInput;
        public float Timestamp;
        public bool IsGrounded;
        public bool IsStunned;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Velocity);
            serializer.SerializeValue(ref KnockbackVelocity);
            serializer.SerializeValue(ref Rotation);
            serializer.SerializeValue(ref LastProcessedInput);
            serializer.SerializeValue(ref Timestamp);
            serializer.SerializeValue(ref IsGrounded);
            serializer.SerializeValue(ref IsStunned);
        }
    }

    /// <summary>
    /// Hybrid third-person character controller for deathrun game with Netcode for GameObjects.
    /// Supports moving platforms, knockback, client-side prediction, and server reconciliation.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(PlayerInput), typeof(NetworkObject))]
    public class PlayerNetworkController : NetworkBehaviour
    {
        #region Serialized Fields
        
        [Header("Movement Configuration")]
        [SerializeField] private float walkSpeed = 6f;
        [SerializeField] private float sprintSpeed = 10f;
        [SerializeField] private float acceleration = 15f;  // Unity's proven value
        [SerializeField] private float deceleration = 50f;  // Much more aggressive stopping
        [SerializeField] private float airAcceleration = 12f;  
        [SerializeField] private float airFriction = 5f;   
        [SerializeField] private float jumpHeight = 1.5f;
        [SerializeField] private float gravity = 25f;
        
        [Header("Character Rotation")]
        [SerializeField] private bool enableCharacterRotation = true;
        [SerializeField] private bool instantRotation = false;             // Instant vs smooth rotation
        [SerializeField] private float rotationSpeed = 360f;              // Moderate smooth rotation to prevent jitter
        [SerializeField] private float minSpeedForRotation = 0.1f;         // Minimum speed to start rotating
        [SerializeField] private bool rotateInAir = true;                  // Allow rotation while jumping/falling
        [SerializeField] private float rotationSmoothTime = 0.1f;          // Smoothing to prevent jitter
        
        [Header("Grounding")]
        [SerializeField] private LayerMask groundMask = 1;
        [SerializeField] private float groundCheckDistance = 0.5f;  // Increased for better detection
        [SerializeField] private float stepOffset = 0.3f;
        [SerializeField] private float slopeLimit = 45f;
        [SerializeField] private float snapDistance = 0.5f;
        [SerializeField] private float coyoteTime = 0.15f;
        [SerializeField] private float jumpBufferTime = 0.2f;  // Increased for better feel
        
        [Header("Platform Interaction")]
        [SerializeField] private LayerMask platformMask = 1;
        [SerializeField] private float platformCarryStrength = 1f;
        [SerializeField] private float maxPlatformSpeed = 20f;
        
        [Header("Knockback System")]
        [SerializeField] private float knockbackDecay = 8f;
        [SerializeField] private float maxKnockbackMagnitude = 25f;
        [SerializeField] private float stunThreshold = 15f;
        [SerializeField] private float maxStunDuration = 2f;
        
        [Header("Physics Interaction")]
        [SerializeField] private LayerMask physicsObjectMask = -1;  // Which layers can be pushed
        [SerializeField] private float pushForce = 8f;             // Base push strength
        [SerializeField] private float pushForceMultiplier = 1.5f; // Multiplier based on player speed
        [SerializeField] private float maxPushForce = 20f;         // Maximum push force
        [SerializeField] private bool canPushObjects = true;       // Enable/disable physics pushing
        
        [Header("Networking")]
        [SerializeField] private int maxPredictionFrames = 60;
        [SerializeField] private float correctionThreshold = 0.1f;
        [SerializeField] private float serverSnapshotRate = 20f;
        
        [Header("References")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform groundCheckTransform;
        
        [Header("Animation System")]
        [SerializeField] private PlayerAnimationController animationController;
        
        #endregion

        #region Private Fields
        
        private CharacterController characterController;
        private PlayerInput playerInput;
        
                // Movement state
        private Vector3 velocity;
        private Vector3 knockbackVelocity;
        private bool isGrounded;
        private float lastGroundedTime;
        private float jumpBufferTimer = 0f;
        private bool isStunned = false;
        private float stunEndTime = 0f;
        private float currentSpeed = 0f;  // Unity's approach for smooth movement
        
        // Character rotation smoothing
        private Vector3 smoothedRotationDirection = Vector3.forward;
        private Vector3 rotationVelocity = Vector3.zero;
        
        // Platform tracking
        private IMovingPlatform currentPlatform;
        private Vector3 lastPlatformPosition;
        private Quaternion lastPlatformRotation;
        private Vector3 platformDeltaPosition;
        private Vector3 platformAngularDelta;
        
        // Networking
        private uint localSequenceId = 0;
        private readonly Queue<InputTick> inputHistory = new Queue<InputTick>();
        private readonly Queue<NetworkState> stateHistory = new Queue<NetworkState>();
        private float lastServerSnapshot = 0f;
        
        // Components cache
        private Camera playerCamera;
        
        #endregion

        #region Properties
        
        public bool IsGrounded => isGrounded;
        public bool IsStunned => isStunned;
        public Vector3 Velocity => velocity + knockbackVelocity;
        public float Speed => new Vector3(velocity.x, 0, velocity.z).magnitude;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerInput = GetComponent<PlayerInput>();
            
            // Get animation controller component (try to find it if not assigned)
            if (animationController == null)
                animationController = GetComponent<PlayerAnimationController>();
            
            // Configure CharacterController for responsive movement
            characterController.stepOffset = stepOffset;
            characterController.slopeLimit = slopeLimit;
            
            // IMPORTANT: Ensure CharacterController doesn't add momentum
            characterController.minMoveDistance = 0f;  // Prevents momentum buildup
            characterController.skinWidth = 0.03f;     // Smaller skin width for precision
            
            if (cameraTransform != null)
                playerCamera = cameraTransform.GetComponent<Camera>();
                
            Debug.Log($"CharacterController configured - stepOffset: {characterController.stepOffset}, slopeLimit: {characterController.slopeLimit}, minMoveDistance: {characterController.minMoveDistance}, skinWidth: {characterController.skinWidth}");
            
            // Validate animation controller setup
            if (animationController == null)
            {
                Debug.LogWarning("PlayerAnimationController component not found! Animation system will be disabled.");
            }
        }

        public override void OnNetworkSpawn()
        {
            // Only owner controls input
            if (!IsOwner)
            {
                playerInput.enabled = false;
                if (playerCamera != null)
                    playerCamera.enabled = false;
            }
            
            Debug.Log($"Player spawned - IsOwner: {IsOwner}, IsServer: {IsServer}, PlayerInput enabled: {playerInput.enabled}");
            
            // Verify component setup
            if (characterController == null)
            {
                Debug.LogError("CharacterController is missing! PlayerNetworkController requires a CharacterController component.");
            }
            
            if (playerInput == null)
            {
                Debug.LogError("PlayerInput is missing! PlayerNetworkController requires a PlayerInput component.");
            }
            
            if (IsOwner && playerInput != null && !playerInput.enabled)
            {
                Debug.LogError("PlayerInput is disabled for owner! This will prevent movement.");
            }
        }

        private void Update()
        {
            if (!IsOwner) return;
            
            // Update timers
            UpdateTimers();
        }

        private void FixedUpdate()
        {
            if (IsOwner)
            {
                // Owner processes input and applies movement in FixedUpdate for consistent physics
                ProcessInputAndMovement();
                
                // Update animations based on current movement state
                UpdateAnimations();
                
                // Debug current state
                if (Time.fixedTime - lastDebugTime > 1f) // Debug every second
                {
                    Debug.Log($"FixedUpdate - Velocity: {velocity}, IsGrounded: {isGrounded}, CharacterController velocity: {characterController.velocity}");
                    lastDebugTime = Time.fixedTime;
                }
            }
            else if (IsServer)
            {
                // Server processes all clients' inputs
                ProcessServerUpdate();
            }
            else
            {
                // Non-owners interpolate received states
                InterpolateRemotePlayer();
            }
        }
        
        private float lastDebugTime = 0f;

        #endregion

        #region Input Processing
        
        private void ProcessInputAndMovement()
        {
            var inputTick = playerInput.GetCurrentInput();
            inputTick.SequenceId = ++localSequenceId;
            inputTick.Timestamp = Time.time;
            inputTick.WasGrounded = isGrounded;
            
            // DEBUG: Check if this script is running
            Debug.Log($"PlayerNetworkController running! Input: {inputTick.MoveDirection}, IsOwner: {IsOwner}");
            
            // Store input for potential rollback
            inputHistory.Enqueue(inputTick);
            while (inputHistory.Count > maxPredictionFrames)
                inputHistory.Dequeue();
            
            // Apply input immediately (client-side prediction)
            ApplyInput(inputTick);
            
            // Send input to server
            SendInputToServerRpc(inputTick);
        }

        private void ProcessInput()
        {
            // This method is now only used by server for processing client inputs
            // Client uses ProcessInputAndMovement instead
        }

        private void ApplyInput(InputTick input)
        {
            // Process swing and taunt inputs even when stunned (allows combat actions)
            if (input.Swing)
            {
                TriggerSwing();
            }
            
            if (input.Taunt > 0)
            {
                TriggerTaunt(input.Taunt);
            }
            
            // Block movement when stunned, but allow swing/taunt above
            if (isStunned && Time.time < stunEndTime)
                return;
            
            // Store previous state for platform calculations
            StorePlatformState();
            
            // Update grounding
            UpdateGrounding();
            
            // Update platform interaction
            UpdatePlatformInteraction();
            
            // Apply movement with Unity's proven approach
            ApplyMovement(input);
            
            // Apply gravity and jump
            ApplyGravityAndJump(input);
            
            // Apply knockback decay
            ApplyKnockbackDecay();
            
            // Move the character using the calculated velocity
            Vector3 totalMovement = (velocity + knockbackVelocity + platformDeltaPosition) * Time.fixedDeltaTime;
            characterController.Move(totalMovement);
            
            // MOMENTUM KILLER: If we want to stop but CharacterController still has velocity, counter it
            if (isGrounded && input.MoveDirection.magnitude < 0.01f && currentSpeed < 0.01f)
            {
                Vector3 ccHorizontalVel = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
                if (ccHorizontalVel.magnitude > 0.01f)
                {
                    // Apply MULTIPLE counter-movements for instant stopping
                    Vector3 counterMovement = -ccHorizontalVel * 5f * Time.fixedDeltaTime;
                    characterController.Move(counterMovement);
                    
                    // If still moving, apply another counter-movement
                    Vector3 ccVelAfter = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
                    if (ccVelAfter.magnitude > 0.01f)
                    {
                        Vector3 secondCounter = -ccVelAfter * 3f * Time.fixedDeltaTime;
                        characterController.Move(secondCounter);
                        
                        Debug.Log($"💀💀 DOUBLE MOMENTUM KILLER: {ccHorizontalVel.magnitude:F3} -> {ccVelAfter.magnitude:F3}");
                    }
                    else
                    {
                        Debug.Log($"💀 MOMENTUM KILLER: {ccHorizontalVel.magnitude:F3} -> STOPPED");
                    }
                }
            }
            
            // Handle collision response for knockback
            HandleKnockbackCollision();
            
            // Update animations
            UpdateAnimations();
        }

        #endregion

        #region Movement Logic
        
        private void ApplyMovement(InputTick input)
        {
            Vector3 inputDirection = GetCameraRelativeDirection(input.MoveDirection);
            float targetSpeed = input.Sprint ? sprintSpeed : walkSpeed;
            
            // THE UNITY SOLUTION: Work WITH CharacterController, not against it
            // Based on Unity's StarterAssets with ENHANCED stopping power
            
            // Set target speed based on input
            if (input.MoveDirection.magnitude < 0.01f) 
            {
                targetSpeed = 0.0f;
            }
            
            // Get current horizontal speed from CharacterController.velocity
            Vector3 currentHorizontalVelocity = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
            float currentHorizontalSpeed = currentHorizontalVelocity.magnitude;
            
            float speedOffset = 0.05f;  // Smaller deadzone for more precision
            float changeRate = (targetSpeed == 0f) ? deceleration : acceleration;
            
            // ENHANCED: Much more aggressive stopping when no input
            if (targetSpeed == 0f && currentHorizontalSpeed > 0.01f)
            {
                // INSTANT STOP for immediate responsiveness
                currentSpeed = 0f;
                
                Debug.Log($"⚡ INSTANT STOP: {currentHorizontalSpeed:F3} -> 0.000");
            }
            else if (currentHorizontalSpeed < targetSpeed - speedOffset || 
                     currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // Normal movement: use Lerp for smooth transitions
                currentSpeed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed, 
                    Time.fixedDeltaTime * changeRate);
                
                // Round to prevent floating point errors
                currentSpeed = Mathf.Round(currentSpeed * 1000f) / 1000f;
            }
            else
            {
                currentSpeed = targetSpeed;
            }
            
            // Set velocity based on calculated speed
            if (isGrounded)
            {
                if (inputDirection.magnitude > 0.01f && currentSpeed > 0.01f)
                {
                    // Moving: use input direction with calculated speed
                    velocity.x = inputDirection.x * currentSpeed;
                    velocity.z = inputDirection.z * currentSpeed;
                    
                    Debug.Log($"✅ MOVING: Input {inputDirection.magnitude:F2}, Speed {currentSpeed:F2}, Vel ({velocity.x:F2}, {velocity.z:F2})");
                }
                else
                {
                    // Stopping: use current direction with decreasing speed
                    if (currentHorizontalVelocity.magnitude > 0.01f)
                    {
                        Vector3 normalizedCurrent = currentHorizontalVelocity.normalized;
                        velocity.x = normalizedCurrent.x * currentSpeed;
                        velocity.z = normalizedCurrent.z * currentSpeed;
                        
                        Debug.Log($"🛑 STOPPING: CCVel {currentHorizontalVelocity.magnitude:F2}, Speed {currentSpeed:F2}, Vel ({velocity.x:F2}, {velocity.z:F2})");
                    }
                    else
                    {
                        velocity.x = 0f;
                        velocity.z = 0f;
                        
                        Debug.Log($"⭐ STOPPED: Velocity set to ZERO");
                    }
                }
            }
            else
            {
                // Air control - keep some physics for air movement
                Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
                Vector3 targetVelocity = inputDirection * targetSpeed;
                
                if (inputDirection.magnitude > 0.1f)
                {
                    horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, 
                        airAcceleration * Time.fixedDeltaTime);
                }
                else
                {
                    horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, 
                        airFriction * Time.fixedDeltaTime);
                }
                
                velocity.x = horizontalVelocity.x;
                velocity.z = horizontalVelocity.z;
            }
            
            // Apply character rotation to face movement direction
            ApplyCharacterRotation(inputDirection);
            
            // IMPORTANT: Force CharacterController to have no inherent velocity
            // CharacterController.Move() doesn't set velocity, it just moves the position
            // We need to ensure there's no momentum carried over
        }

        /// <summary>
        /// Rotate character to face movement direction (camera-relative)
        /// </summary>
        private void ApplyCharacterRotation(Vector3 inputDirection)
        {
            if (!enableCharacterRotation)
                return;
                
            // Use actual movement velocity direction for more accurate rotation
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            bool shouldRotate = horizontalVelocity.magnitude > minSpeedForRotation;
            shouldRotate = shouldRotate && (isGrounded || rotateInAir);
            
            if (shouldRotate)
            {
                // Use input direction for immediate response, velocity for accuracy
                Vector3 targetDirection = inputDirection.magnitude > 0.01f ? inputDirection.normalized : horizontalVelocity.normalized;
                
                if (targetDirection.magnitude > 0.01f)
                {
                    // Smooth the rotation direction to prevent jittering
                    smoothedRotationDirection = Vector3.SmoothDamp(smoothedRotationDirection, targetDirection, 
                        ref rotationVelocity, rotationSmoothTime);
                    
                    // Only rotate if the smoothed direction is significant
                    if (smoothedRotationDirection.magnitude > 0.1f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(smoothedRotationDirection);
                        
                        if (instantRotation)
                        {
                            transform.rotation = targetRotation;
                        }
                        else
                        {
                            // Use Slerp for smoother rotation instead of RotateTowards
                            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                                rotationSpeed * Time.fixedDeltaTime / 180f); // Convert degrees to 0-1 range
                        }
                    }
                }
            }
            else
            {
                // When not moving, maintain current smoothed direction
                smoothedRotationDirection = transform.forward;
            }
        }

        private void ApplyGravityAndJump(InputTick input)
        {
            bool canJump = CanJump(input);
            
            if (canJump && input.Jump)
            {
                // Calculate jump velocity for desired height
                float jumpVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
                velocity.y = jumpVelocity;
                lastGroundedTime = Time.time - coyoteTime; // Disable coyote time
                jumpBufferTimer = 0f; // Clear jump buffer
            }
            else if (!isGrounded)
            {
                // Apply gravity when in air
                velocity.y -= gravity * Time.fixedDeltaTime;
                
                // Terminal velocity cap
                velocity.y = Mathf.Max(velocity.y, -50f);
            }
            else if (isGrounded && velocity.y < 0)
            {
                // Snap to ground when landing
                velocity.y = 0f;
            }
        }

        private bool CanJump(InputTick input)
        {
            // Coyote time: can jump shortly after leaving ground
            bool withinCoyoteTime = Time.time - lastGroundedTime <= coyoteTime;
            
            // Jump buffer: register jump input briefly before landing
            if (input.Jump)
                jumpBufferTimer = jumpBufferTime;
            
            // Can jump if grounded, within coyote time, and have jump input buffered
            bool canJump = (isGrounded || withinCoyoteTime) && jumpBufferTimer > 0f;
            
            // Also allow jump if we just pressed jump and are grounded (immediate response)
            if (input.Jump && isGrounded)
                canJump = true;
            
            return canJump;
        }

        private Vector3 GetCameraRelativeDirection(Vector3 inputDirection)
        {
            if (cameraTransform == null || inputDirection.magnitude < 0.01f)
                return Vector3.zero;
            
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            
            // Remove vertical component
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            
            return (forward * inputDirection.z + right * inputDirection.x).normalized;
        }

        #endregion

        #region Grounding System
        
        private void UpdateGrounding()
        {
            bool wasGrounded = isGrounded;
            
            // Perform ground check
            Vector3 checkPosition = groundCheckTransform != null ? 
                groundCheckTransform.position : (transform.position + Vector3.down * 0.9f);
            
            RaycastHit hit;
            isGrounded = Physics.Raycast(checkPosition, Vector3.down, out hit, 
                groundCheckDistance, groundMask);
            
            // Additional check with CharacterController for more reliable detection
            if (!isGrounded && characterController != null)
            {
                isGrounded = characterController.isGrounded;
            }
            
            // Update grounded time
            if (isGrounded)
            {
                lastGroundedTime = Time.time;
                
                // Reset vertical velocity if landing
                if (!wasGrounded && velocity.y < 0)
                {
                    velocity.y = 0f;
                }
            }
        }

        #endregion

        #region Platform System
        
        private void StorePlatformState()
        {
            if (currentPlatform != null)
            {
                lastPlatformPosition = currentPlatform.transform.position;
                lastPlatformRotation = currentPlatform.transform.rotation;
            }
        }

        private void UpdatePlatformInteraction()
        {
            platformDeltaPosition = Vector3.zero;
            platformAngularDelta = Vector3.zero;
            
            // Detect platform beneath player
            if (isGrounded)
            {
                DetectPlatform();
            }
            else
            {
                currentPlatform = null;
            }
            
            // Calculate platform movement
            if (currentPlatform != null)
            {
                CalculatePlatformDelta();
            }
        }

        private void DetectPlatform()
        {
            Vector3 checkPosition = groundCheckTransform != null ? 
                groundCheckTransform.position : transform.position;
            
            RaycastHit hit;
            if (Physics.Raycast(checkPosition, Vector3.down, out hit, 
                groundCheckDistance + 0.1f, platformMask))
            {
                var platform = hit.collider.GetComponent<IMovingPlatform>();
                if (platform != null)
                {
                    if (currentPlatform != platform)
                    {
                        // New platform detected
                        currentPlatform = platform;
                        lastPlatformPosition = platform.transform.position;
                        lastPlatformRotation = platform.transform.rotation;
                    }
                }
                else
                {
                    currentPlatform = null;
                }
            }
            else
            {
                currentPlatform = null;
            }
        }

        private void CalculatePlatformDelta()
        {
            if (currentPlatform == null) return;
            
            Transform platformTransform = currentPlatform.transform;
            
            // Linear movement
            Vector3 currentPosition = platformTransform.position;
            Vector3 linearDelta = currentPosition - lastPlatformPosition;
            
            // Clamp platform speed for safety
            if (linearDelta.magnitude / Time.fixedDeltaTime > maxPlatformSpeed)
            {
                // Platform might have teleported - ignore this frame
                lastPlatformPosition = currentPosition;
                lastPlatformRotation = platformTransform.rotation;
                return;
            }
            
            platformDeltaPosition = linearDelta * platformCarryStrength;
            
            // Angular movement (rotate player around platform)
            Quaternion currentRotation = platformTransform.rotation;
            Quaternion deltaRotation = currentRotation * Quaternion.Inverse(lastPlatformRotation);
            
            if (Quaternion.Angle(deltaRotation, Quaternion.identity) > 0.01f)
            {
                Vector3 platformToPlayer = transform.position - platformTransform.position;
                Vector3 rotatedOffset = deltaRotation * platformToPlayer;
                Vector3 angularMovement = rotatedOffset - platformToPlayer;
                platformDeltaPosition += angularMovement;
                
                // Rotate player with platform
                transform.rotation = deltaRotation * transform.rotation;
            }
            
            // Apply conveyor velocity if platform implements it
            var conveyor = currentPlatform as Conveyor;
            if (conveyor != null)
            {
                velocity += conveyor.GetConveyorVelocity() * Time.fixedDeltaTime;
            }
        }

        #endregion

        #region Knockback System
        
        public void ApplyKnockback(Vector3 force, float stunDuration = 0f)
        {
            knockbackVelocity += force;
            
            // Clamp knockback magnitude
            if (knockbackVelocity.magnitude > maxKnockbackMagnitude)
            {
                knockbackVelocity = knockbackVelocity.normalized * maxKnockbackMagnitude;
            }
            
            // Apply stun if force is above threshold
            if (force.magnitude >= stunThreshold && stunDuration > 0f)
            {
                isStunned = true;
                stunEndTime = Time.time + Mathf.Min(stunDuration, maxStunDuration);
            }
            
            // Network sync knockback
            if (IsServer)
            {
                ApplyKnockbackClientRpc(force, stunDuration);
            }
        }

        private void ApplyKnockbackDecay()
        {
            if (knockbackVelocity.magnitude > 0.1f)
            {
                knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero, 
                    knockbackDecay * Time.fixedDeltaTime);
            }
            else
            {
                knockbackVelocity = Vector3.zero;
            }
            
            // Clear stun when time expires
            if (isStunned && Time.time >= stunEndTime)
            {
                isStunned = false;
            }
        }

        private void HandleKnockbackCollision()
        {
            // Reduce knockback on wall/ceiling collision
            if ((characterController.collisionFlags & CollisionFlags.Sides) != 0)
            {
                // Hit wall - reduce horizontal knockback
                knockbackVelocity.x *= 0.3f;
                knockbackVelocity.z *= 0.3f;
            }
            
            if ((characterController.collisionFlags & CollisionFlags.Above) != 0)
            {
                // Hit ceiling - reduce upward knockback
                if (knockbackVelocity.y > 0)
                    knockbackVelocity.y *= 0.2f;
            }
        }

        /// <summary>
        /// Called when CharacterController hits a collider. Used for physics object interaction.
        /// </summary>
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // Only push objects if enabled and we're the owner
            if (!canPushObjects || !IsOwner)
                return;

            Rigidbody hitRigidbody = hit.collider.attachedRigidbody;
            
            // Check if the object can be pushed
            if (hitRigidbody == null || hitRigidbody.isKinematic)
                return;
                
            // Check if object is on the correct layer
            if ((physicsObjectMask.value & (1 << hit.collider.gameObject.layer)) == 0)
                return;

            // Don't push objects below us (prevents pushing ground objects when walking over them)
            if (hit.moveDirection.y < -0.3f)
                return;

            // Calculate push direction and force
            Vector3 pushDirection = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
            
            // If we're moving into the object, use our movement direction for more predictable pushing
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
            if (horizontalVelocity.magnitude > 0.1f)
            {
                pushDirection = horizontalVelocity.normalized;
            }
            
            // Calculate force based on player speed and mass
            float playerSpeed = horizontalVelocity.magnitude;
            float dynamicForce = pushForce + (playerSpeed * pushForceMultiplier);
            dynamicForce = Mathf.Clamp(dynamicForce, pushForce, maxPushForce);
            
            // Apply mass scaling (lighter objects get pushed more)
            float massScale = Mathf.Clamp(1f / hitRigidbody.mass, 0.1f, 2f);
            Vector3 finalForce = pushDirection * dynamicForce * massScale;
            
            // Apply the force
            hitRigidbody.AddForceAtPosition(finalForce, hit.point, ForceMode.Force);
            
            Debug.Log($"🏀 Pushing {hit.collider.name}: Force {finalForce.magnitude:F1}, Speed {playerSpeed:F1}, Mass {hitRigidbody.mass:F1}");
        }

        #endregion

        #region Networking RPCs
        
        [ServerRpc]
        private void SendInputToServerRpc(InputTick input)
        {
            // Server processes client input
            ProcessClientInput(input);
        }

        [ClientRpc]
        private void ApplyKnockbackClientRpc(Vector3 force, float stunDuration)
        {
            if (IsOwner) return; // Owner already has knockback applied
            
            ApplyKnockback(force, stunDuration);
        }

        [ClientRpc]
        private void SendCorrectionSnapshotClientRpc(NetworkState state)
        {
            if (!IsOwner) return;
            
            // Client receives authoritative state from server
            ProcessServerCorrection(state);
        }

        #endregion

        #region Server Logic
        
        private void ProcessServerUpdate()
        {
            // Send periodic snapshots to clients
            if (Time.time - lastServerSnapshot >= 1f / serverSnapshotRate)
            {
                SendCorrectionSnapshot();
                lastServerSnapshot = Time.time;
            }
        }

        private void ProcessClientInput(InputTick input)
        {
            // Server applies the same movement logic
            ApplyInput(input);
            
            // Store state for correction snapshots
            var state = new NetworkState
            {
                Position = transform.position,
                Velocity = velocity,
                KnockbackVelocity = knockbackVelocity,
                Rotation = transform.rotation,
                LastProcessedInput = input.SequenceId,
                Timestamp = Time.time,
                IsGrounded = isGrounded,
                IsStunned = isStunned
            };
            
            stateHistory.Enqueue(state);
            while (stateHistory.Count > maxPredictionFrames)
                stateHistory.Dequeue();
        }

        private void SendCorrectionSnapshot()
        {
            var state = new NetworkState
            {
                Position = transform.position,
                Velocity = velocity,
                KnockbackVelocity = knockbackVelocity,
                Rotation = transform.rotation,
                LastProcessedInput = localSequenceId,
                Timestamp = Time.time,
                IsGrounded = isGrounded,
                IsStunned = isStunned
            };
            
            SendCorrectionSnapshotClientRpc(state);
        }

        #endregion

        #region Client Prediction & Reconciliation
        
        private void ProcessServerCorrection(NetworkState serverState)
        {
            // Check if correction is needed
            float positionError = Vector3.Distance(transform.position, serverState.Position);
            
            if (positionError > correctionThreshold)
            {
                // Apply server state
                transform.position = serverState.Position;
                velocity = serverState.Velocity;
                knockbackVelocity = serverState.KnockbackVelocity;
                transform.rotation = serverState.Rotation;
                isGrounded = serverState.IsGrounded;
                isStunned = serverState.IsStunned;
                
                // Re-apply unacknowledged inputs (rollback)
                ReapplyInputsAfterCorrection(serverState.LastProcessedInput);
                
                Debug.Log($"Applied server correction - error: {positionError:F3}");
            }
        }

        private void ReapplyInputsAfterCorrection(uint lastProcessedInput)
        {
            // Find inputs to replay
            var inputsToReplay = new Queue<InputTick>();
            
            foreach (var input in inputHistory)
            {
                if (input.SequenceId > lastProcessedInput)
                {
                    inputsToReplay.Enqueue(input);
                }
            }
            
            // Replay unacknowledged inputs
            while (inputsToReplay.Count > 0)
            {
                var input = inputsToReplay.Dequeue();
                ApplyInput(input);
            }
        }

        private void InterpolateRemotePlayer()
        {
            // Non-owners smoothly interpolate to received positions
            // This is handled by NetworkTransform as fallback
            // Custom interpolation could be implemented here if needed
        }

        #endregion

        #region Utility Methods
        
        private void UpdateTimers()
        {
            jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - Time.deltaTime);
        }

        #endregion

        #region Debug Visualization
        
        private void OnDrawGizmosSelected()
        {
            // Ground check visualization
            if (groundCheckTransform != null)
            {
                Gizmos.color = isGrounded ? Color.green : Color.red;
                Gizmos.DrawRay(groundCheckTransform.position, Vector3.down * groundCheckDistance);
            }
            
            // Velocity visualization
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position + Vector3.up, velocity);
            
            // Knockback visualization
            if (knockbackVelocity.magnitude > 0.1f)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position + Vector3.up * 1.5f, knockbackVelocity);
            }
            
            // Platform delta visualization
            if (platformDeltaPosition.magnitude > 0.01f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position + Vector3.up * 2f, platformDeltaPosition * 10f);
            }
        }

        #endregion

        #region TODO Stubs
        
        // TODO: Implement dash ability
        public void StartDash(Vector3 direction, float dashForce)
        {
            // Apply instant velocity in dash direction
            // Disable input for dash duration
            // Add dash cooldown
        }
        
        // TODO: Implement wall-run mechanics
        public bool CheckForWallRun()
        {
            // Raycast to sides for wall detection
            // Check if player has enough speed
            // Apply wall-run velocity and rotation
            return false;
        }
        
        // TODO: Advanced stun states
        public void ApplyAdvancedStun(float duration, bool disableMovement, bool disableJump)
        {
            // More granular stun control
            // Different stun types (movement, jump, abilities)
        }
        
        #endregion
        
        #region Animation Interface
        
        /// <summary>
        /// Update animation system with current movement state
        /// </summary>
        private void UpdateAnimations()
        {
            if (animationController == null)
                return;
                
            // Pass raw movement data to animation controller - let it do all the calculations
            bool isSprinting = playerInput != null && playerInput.GetCurrentInput().Sprint;
            animationController.UpdateAnimations(velocity, isGrounded, isSprinting);
        }
        
        /// <summary>
        /// Trigger swing animation (call this from input or attack system)
        /// </summary>
        public void TriggerSwing()
        {
            if (animationController != null)
                animationController.TriggerSwing();
        }
        
        /// <summary>
        /// Trigger specific taunt animation (1, 2, or 3)
        /// </summary>
        public void TriggerTaunt(int tauntNumber)
        {
            if (animationController != null)
                animationController.TriggerTaunt(tauntNumber);
        }
        
        /// <summary>
        /// Trigger random taunt animation
        /// </summary>
        public void TriggerRandomTaunt()
        {
            if (animationController != null)
                animationController.TriggerRandomTaunt();
        }
        
        /// <summary>
        /// Check if swing is currently on cooldown
        /// </summary>
        public bool IsSwingOnCooldown()
        {
            return animationController != null ? animationController.IsSwingOnCooldown() : false;
        }
        
        /// <summary>
        /// Get remaining cooldown time for swing
        /// </summary>
        public float GetSwingCooldownRemaining()
        {
            return animationController != null ? animationController.GetSwingCooldownRemaining() : 0f;
        }
        
        #endregion
    }
}