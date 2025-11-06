using Unity.Netcode;
using UnityEngine;

namespace DeathrunGame
{
    /// <summary>
    /// Controls player animations based on PlayerStateStatus.
    /// Translates state changes into Animator parameter updates.
    /// Works for both owner and remote players.
    /// Uses ONLY bools for reliable network synchronization.
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

        // Animation parameter hashes (cached for performance)
        private int speedHash;
        private int blendHash;
        private int isGroundedHash;
        private int isMovingHash;
        private int isSprintingHash;
        private int isJumpingHash;
        private int isSwingingHash;
        private int taunt1Hash; // Keep triggers for taunt selection
        private int taunt2Hash;
        private int taunt3Hash;

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
            
            // ✅ Bool parameters (network-synced)
            isGroundedHash = Animator.StringToHash("IsGrounded");
            isMovingHash = Animator.StringToHash("IsMoving");
            isSprintingHash = Animator.StringToHash("IsSprinting");
            isJumpingHash = Animator.StringToHash("IsJumping");
            isSwingingHash = Animator.StringToHash("IsSwinging");
            
            // Triggers for taunt selection (optional)
            taunt1Hash = Animator.StringToHash("Taunt1");
            taunt2Hash = Animator.StringToHash("Taunt2");
            taunt3Hash = Animator.StringToHash("Taunt3");
        }

        public override void OnNetworkSpawn()
        {
            // ✅ Subscribe to events for sound/VFX, NOT animation triggers
            SubscribeToStateChanges();
        }

        public override void OnNetworkDespawn()
        {
            UnsubscribeFromStateChanges();
        }

        private void SubscribeToStateChanges()
        {
            // Subscribe for audio/VFX effects only
            stateStatus.OnLanded += OnLanded;
            stateStatus.OnStartedJumping += OnStartedJumping;
            stateStatus.OnStartedFalling += OnStartedFalling;
            stateStatus.OnSwingStarted += OnSwingStarted;
            stateStatus.OnSwingEnded += OnSwingEnded;
            stateStatus.OnTauntStarted += OnTauntStarted;
            stateStatus.OnTauntEnded += OnTauntEnded;
        }

        private void UnsubscribeFromStateChanges()
        {
            stateStatus.OnLanded -= OnLanded;
            stateStatus.OnStartedJumping -= OnStartedJumping;
            stateStatus.OnStartedFalling -= OnStartedFalling;
            stateStatus.OnSwingStarted -= OnSwingStarted;
            stateStatus.OnSwingEnded -= OnSwingEnded;
            stateStatus.OnTauntStarted -= OnTauntStarted;
            stateStatus.OnTauntEnded -= OnTauntEnded;
        }

        private void Update()
        {
            if (IsOwner)
            {
                HandleTauntInput();
                HandleSwingInput();
            }

            // ✅ Update animations every frame for all players
            UpdateAnimationParameters();
        }

        private void HandleTauntInput()
        {
            if (isPerformingAction) return;
            if (stateStatus.IsDead.Value) return;

            if (Input.GetKeyDown(taunt1Key))
            {
                TriggerTaunt(1);
            }
            else if (Input.GetKeyDown(taunt2Key))
            {
                TriggerTaunt(2);
            }
            else if (Input.GetKeyDown(taunt3Key))
            {
                TriggerTaunt(3);
            }
        }

        private void HandleSwingInput()
        {
            if (isPerformingAction) return;
            if (stateStatus.IsDead.Value) return;

            if (Input.GetKeyDown(swingKey))
            {
                TriggerSwing();
            }
        }

        private void TriggerTaunt(int tauntNumber)
        {
            stateStatus.SetTaunting(true);

            // Use triggers to select which taunt to play
            switch (tauntNumber)
            {
                case 1:
                    animator.SetTrigger(taunt1Hash);
                    break;
                case 2:
                    animator.SetTrigger(taunt2Hash);
                    break;
                case 3:
                    animator.SetTrigger(taunt3Hash);
                    break;
            }

            isPerformingAction = true;
        }

        private void TriggerSwing()
        {
            stateStatus.SetSwinging(true);
            isPerformingAction = true;
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
            // ✅ ALL BOOLS - NetworkAnimator syncs these automatically
            animator.SetBool(isGroundedHash, stateStatus.IsGrounded.Value);
            animator.SetBool(isMovingHash, stateStatus.IsMoving.Value);
            animator.SetBool(isJumpingHash, stateStatus.IsJumping.Value);
            animator.SetBool(isSwingingHash, stateStatus.IsSwinging.Value);

            bool isSprinting = stateStatus.CurrentSpeed.Value >= sprintThreshold;
            animator.SetBool(isSprintingHash, isSprinting);
        }

        // ✅ Event handlers for sound/VFX ONLY - NOT animation triggers
        private void OnLanded()
        {
            Debug.Log("🟢 Landed - play landing sound");
            // TODO: Play landing sound/particle effect
        }

        private void OnStartedJumping()
        {
            Debug.Log("⬆️ Jump - play jump sound");
            // TODO: Play jump sound
        }

        private void OnStartedFalling()
        {
            Debug.Log("⬇️ Falling");
            // TODO: Optional falling wind sound
        }

        private void OnSwingStarted()
        {
            isPerformingAction = true;
            Debug.Log("⚾ Swing started - play whoosh sound");
            // TODO: Play swing whoosh sound
        }

        private void OnSwingEnded()
        {
            isPerformingAction = false;
        }

        private void OnTauntStarted()
        {
            isPerformingAction = true;
            Debug.Log("🎭 Taunt started");
            // TODO: Play taunt voice line
        }

        private void OnTauntEnded()
        {
            isPerformingAction = false;
        }

        // ✅ Animation event callbacks (called from animation clips)
        public void OnSwingAnimationEnd()
        {
            if (IsOwner)
            {
                stateStatus.SetSwinging(false);
            }
        }

        public void OnTauntAnimationEnd()
        {
            if (IsOwner)
            {
                stateStatus.SetTaunting(false);
            }
        }

        public void OnSwingHitFrame()
        {
            if (IsOwner)
            {
                Debug.Log("⚾ BAT HIT FRAME!");
                // TODO: Perform sphere cast or trigger check for hits
            }
        }
    }
}