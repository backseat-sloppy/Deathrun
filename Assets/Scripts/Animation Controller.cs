using Unity.Netcode;
using UnityEngine;

namespace DeathrunGame
{
    /// <summary>
    /// Controls player animations based on PlayerStateStatus.
    /// Translates state changes into Animator parameter updates.
    /// Works for both owner and remote players.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayerStateStatus))]
    public class AnimationController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerStateStatus stateStatus;

        [Header("Animation Settings")]
        [SerializeField] private float blendSpeed = 10f;
        [SerializeField] private float minSpeedThreshold = 0.01f;

        [Header("Speed Thresholds")]
        [Tooltip("Speed value that represents walking (normalized to 0-1 in blend tree)")]
        [SerializeField] private float walkSpeed = 2.5f;
        [Tooltip("Speed value that represents sprinting (normalized to 0-1 in blend tree)")]
        [SerializeField] private float sprintSpeed = 5f;
        [Tooltip("Threshold above walk speed to trigger sprint animation")]
        [SerializeField] private float sprintThreshold = 4f;

        [Header("Taunt Settings")]
        [SerializeField] private KeyCode taunt1Key = KeyCode.Alpha1;
        [SerializeField] private KeyCode taunt2Key = KeyCode.Alpha2;
        [SerializeField] private KeyCode taunt3Key = KeyCode.Alpha3;

        [Header("Attack Settings")]
        [SerializeField] private KeyCode swingKey = KeyCode.Mouse0;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // Animation parameter hashes (cached for performance)
        private int speedHash;
        private int blendHash;
        private int isGroundedHash;
        private int isMovingHash;
        private int isSprintingHash;
        private int isJumpingHash;
        private int isSwingingHash;
        private int isTaunt1Hash;
        private int isTaunt2Hash;
        private int isTaunt3Hash;

        private float currentBlend;
        private bool isPerformingAction;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            if (stateStatus == null)
                stateStatus = GetComponent<PlayerStateStatus>();

            CacheAnimationHashes();
        }

        private void CacheAnimationHashes()
        {
            // Float parameters
            speedHash = Animator.StringToHash("Speed");
            blendHash = Animator.StringToHash("Blend");

            // Bool parameters (network-synced)
            isGroundedHash = Animator.StringToHash("IsGrounded");
            isMovingHash = Animator.StringToHash("IsMoving");
            isSprintingHash = Animator.StringToHash("IsSprinting");
            isJumpingHash = Animator.StringToHash("IsJumping");
            isSwingingHash = Animator.StringToHash("IsSwinging");

            // ✅ FIXED: Match the actual Animator parameter names
            isTaunt1Hash = Animator.StringToHash("Taunt1"); // Changed from "IsTaunt1"
            isTaunt2Hash = Animator.StringToHash("Taunt2"); // Changed from "IsTaunt2"
            isTaunt3Hash = Animator.StringToHash("Taunt3"); // Changed from "IsTaunt3"

            if (showDebugLogs)
            {
                Debug.Log("🎬 Animation hashes cached");
            }
        }

        public override void OnNetworkSpawn()
        {
            SubscribeToStateChanges();
            
            if (showDebugLogs && IsOwner)
            {
                Debug.Log($"🎮 AnimationController spawned. IsOwner: {IsOwner}");
            }
        }

        public override void OnNetworkDespawn()
        {
            UnsubscribeFromStateChanges();
        }

        private void SubscribeToStateChanges()
        {
            stateStatus.OnLanded += OnLanded;
            stateStatus.OnStartedJumping += OnStartedJumping;
            stateStatus.OnStartedFalling += OnStartedFalling;
            stateStatus.OnSwingStarted += OnSwingStarted;
            stateStatus.OnSwingEnded += OnSwingEnded;
            stateStatus.OnTaunt1Started += OnTaunt1Started;
            stateStatus.OnTaunt1Ended += OnTaunt1Ended;
            stateStatus.OnTaunt2Started += OnTaunt2Started;
            stateStatus.OnTaunt2Ended += OnTaunt2Ended;
            stateStatus.OnTaunt3Started += OnTaunt3Started;
            stateStatus.OnTaunt3Ended += OnTaunt3Ended;
        }

        private void UnsubscribeFromStateChanges()
        {
            stateStatus.OnLanded -= OnLanded;
            stateStatus.OnStartedJumping -= OnStartedJumping;
            stateStatus.OnStartedFalling -= OnStartedFalling;
            stateStatus.OnSwingStarted -= OnSwingStarted;
            stateStatus.OnSwingEnded -= OnSwingEnded;
            stateStatus.OnTaunt1Started -= OnTaunt1Started;
            stateStatus.OnTaunt1Ended -= OnTaunt1Ended;
            stateStatus.OnTaunt2Started -= OnTaunt2Started;
            stateStatus.OnTaunt2Ended -= OnTaunt2Ended;
            stateStatus.OnTaunt3Started -= OnTaunt3Started;
            stateStatus.OnTaunt3Ended -= OnTaunt3Ended;
        }

        private void Update()
        {
            if (IsOwner)
            {
                HandleTauntInput();
                HandleSwingInput();
            }

            UpdateAnimationParameters();
        }

        private void HandleTauntInput()
        {
            // ✅ DEBUGGING: Check why taunts might not trigger
            if (Input.GetKeyDown(taunt1Key))
            {
                if (showDebugLogs)
                {
                    Debug.Log($"🎮 Taunt1 key pressed! isPerformingAction: {isPerformingAction}, IsDead: {stateStatus.IsDead.Value}, IsOwner: {IsOwner}");
                }

                if (isPerformingAction)
                {
                    if (showDebugLogs) Debug.LogWarning("❌ Taunt1 blocked: isPerformingAction is true");
                    return;
                }
                if (stateStatus.IsDead.Value)
                {
                    if (showDebugLogs) Debug.LogWarning("❌ Taunt1 blocked: Player is dead");
                    return;
                }

                stateStatus.SetTaunt1(true);
                isPerformingAction = true;
                if (showDebugLogs) Debug.Log("✅ Taunt1 triggered!");
                return; // Prevent multiple taunts at once
            }

            if (Input.GetKeyDown(taunt2Key))
            {
                if (showDebugLogs)
                {
                    Debug.Log($"🎮 Taunt2 key pressed! isPerformingAction: {isPerformingAction}, IsDead: {stateStatus.IsDead.Value}");
                }

                if (isPerformingAction) return;
                if (stateStatus.IsDead.Value) return;

                stateStatus.SetTaunt2(true);
                isPerformingAction = true;
                if (showDebugLogs) Debug.Log("✅ Taunt2 triggered!");
                return;
            }

            if (Input.GetKeyDown(taunt3Key))
            {
                if (showDebugLogs)
                {
                    Debug.Log($"🎮 Taunt3 key pressed! isPerformingAction: {isPerformingAction}, IsDead: {stateStatus.IsDead.Value}");
                }

                if (isPerformingAction) return;
                if (stateStatus.IsDead.Value) return;

                stateStatus.SetTaunt3(true);
                isPerformingAction = true;
                if (showDebugLogs) Debug.Log("✅ Taunt3 triggered!");
            }
        }

        private void HandleSwingInput()
        {
            if (isPerformingAction) return;
            if (stateStatus.IsDead.Value) return;

            if (Input.GetKeyDown(swingKey))
            {
                stateStatus.SetSwinging(true);
                isPerformingAction = true;
                if (showDebugLogs) Debug.Log("✅ Swing triggered!");
            }
        }

        private void UpdateAnimationParameters()
        {
            UpdateBlendParameter();
            UpdateBooleanParameters();
        }

        private void UpdateBlendParameter()
        {
            float rawSpeed = stateStatus.CurrentSpeed.Value;

            if (rawSpeed < minSpeedThreshold)
                rawSpeed = 0f;

            float normalizedSpeed = 0f;
            if (rawSpeed > minSpeedThreshold)
            {
                normalizedSpeed = Mathf.Clamp01(rawSpeed / sprintSpeed);
            }

            currentBlend = Mathf.Lerp(currentBlend, normalizedSpeed, Time.deltaTime * blendSpeed);

            animator.SetFloat(blendHash, currentBlend);
            animator.SetFloat(speedHash, rawSpeed);
        }

        private void UpdateBooleanParameters()
        {
            // ALL BOOLS - NetworkAnimator syncs these automatically
            animator.SetBool(isGroundedHash, stateStatus.IsGrounded.Value);
            animator.SetBool(isMovingHash, stateStatus.IsMoving.Value);
            animator.SetBool(isJumpingHash, stateStatus.IsJumping.Value);
            animator.SetBool(isSwingingHash, stateStatus.IsSwinging.Value);

            // Three separate taunt bools - simple and reliable
            animator.SetBool(isTaunt1Hash, stateStatus.IsTaunt1.Value);
            animator.SetBool(isTaunt2Hash, stateStatus.IsTaunt2.Value);
            animator.SetBool(isTaunt3Hash, stateStatus.IsTaunt3.Value);

            bool isSprinting = stateStatus.CurrentSpeed.Value >= sprintThreshold;
            animator.SetBool(isSprintingHash, isSprinting);
        }

        // Event handlers for sound/VFX
        private void OnLanded()
        {
            if (showDebugLogs) Debug.Log("🟢 Landed - play landing sound");
        }

        private void OnStartedJumping()
        {
            if (showDebugLogs) Debug.Log("⬆️ Jump - play jump sound");
        }

        private void OnStartedFalling()
        {
            if (showDebugLogs) Debug.Log("⬇️ Falling");
        }

        private void OnSwingStarted()
        {
            isPerformingAction = true;
            if (showDebugLogs) Debug.Log("⚾ Swing started - play whoosh sound");
        }

        private void OnSwingEnded()
        {
            isPerformingAction = false;
            if (showDebugLogs) Debug.Log("⚾ Swing ended - isPerformingAction reset");
        }

        private void OnTaunt1Started()
        {
            isPerformingAction = true;
            if (showDebugLogs) Debug.Log("🎭 Taunt1 animation started");
        }

        private void OnTaunt1Ended()
        {
            isPerformingAction = false;
            if (showDebugLogs) Debug.Log("🎭 Taunt1 ended - isPerformingAction reset");
        }

        private void OnTaunt2Started()
        {
            isPerformingAction = true;
            if (showDebugLogs) Debug.Log("🎭 Taunt2 animation started");
        }

        private void OnTaunt2Ended()
        {
            isPerformingAction = false;
            if (showDebugLogs) Debug.Log("🎭 Taunt2 ended - isPerformingAction reset");
        }

        private void OnTaunt3Started()
        {
            isPerformingAction = true;
            if (showDebugLogs) Debug.Log("🎭 Taunt3 animation started");
        }

        private void OnTaunt3Ended()
        {
            isPerformingAction = false;
            if (showDebugLogs) Debug.Log("🎭 Taunt3 ended - isPerformingAction reset");
        }

        // Animation event callbacks
        public void OnSwingAnimationEnd()
        {
            if (IsOwner)
            {
                stateStatus.SetSwinging(false);
            }
        }

        public void OnSwingHitFrame()
        {
            if (IsOwner)
            {
                if (showDebugLogs) Debug.Log("⚾ BAT HIT FRAME!");
            }
        }
    }
}