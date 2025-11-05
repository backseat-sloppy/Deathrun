using UnityEngine;

namespace DeathrunGame
{
    /// <summary>
    /// Handles all animation logic for the player character
    /// Manages animator parameters, triggers, and speed synchronization
    /// Separated from PlayerNetworkController for better code organization
    /// </summary>
    public class PlayerAnimationController : MonoBehaviour
    {
        #region Animation Settings
        
        [Header("Animation Components")]
        [SerializeField] private Animator animator;
        [SerializeField] private bool enableAnimations = true;
        [SerializeField] private float animationSmoothTime = 0.1f;
        [SerializeField] private float minSpeedForMovement = 0.1f;
        
        [Header("Animation Speed Sync")]
        [SerializeField] private bool useAnimatorSpeed = true;        // Use Animator.speed for sync
        [SerializeField] private bool useBlendTreeThresholds = false; // Use proper blend tree setup instead
        
        [Header("Animation Speed Calibration")]
        [SerializeField] private float walkAnimationSpeed = 1.0f;     // How fast walk animation should play (calibrate this!)
        [SerializeField] private float sprintAnimationSpeed = 1.0f;   // How fast sprint animation should play (calibrate this!)
        [SerializeField] private float jumpAnimationSpeed = 1.0f;     // How fast jump animation should play
        [SerializeField] private bool autoCalculateJumpSpeed = true;   // Auto-sync jump animation with physics
        [SerializeField] private bool showCalibrationInfo = true;     // Debug info for calibration
        
        [Header("Animation Parameters")]
        [SerializeField] private string blendParameterName = "Blend";
        [SerializeField] private string speedParameterName = "Speed";
        [SerializeField] private string jumpBoolName = "IsJumping";     // Bool parameter for jump state
        [SerializeField] private string groundedBoolName = "IsGrounded"; // Bool parameter for grounded state
        [SerializeField] private string swingBoolName = "IsSwinging";    // Bool parameter for swing state
        [SerializeField] private string taunt1TriggerName = "Taunt1";
        [SerializeField] private string taunt2TriggerName = "Taunt2";
        [SerializeField] private string taunt3TriggerName = "Taunt3";
        
        // Optional parameters (add these to your animator if needed)
        [SerializeField] private string isGroundedParameterName = "IsGrounded";
        [SerializeField] private string isMovingParameterName = "IsMoving";
        [SerializeField] private string isSprintingParameterName = "IsSprinting";
        
        [Header("Combat Settings")]
        [SerializeField] private float swingCooldown = 1.5f; // Cooldown in seconds between swings
        
        #endregion
        
        #region Private Fields
        
        // Animation state tracking
        private bool wasGrounded = true;
        private bool wasMoving = false;
        private bool wasSprinting = false;
        
        // Combat state tracking
        private float lastSwingTime = -10f; // Initialize to allow immediate first swing
        private bool isSwinging = false;    // Track current swing state
        
        // Animation smoothing
        private float currentBlendValue = 0f;
        private float currentSpeedValue = 0f;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Auto-find animator if not assigned
            if (animator == null)
                animator = GetComponent<Animator>();
                
            ValidateAnimatorParameters();
        }
        
        private void Start()
        {
            if (animator == null)
            {
                Debug.LogError("PlayerAnimationController: No Animator component found!");
                enableAnimations = false;
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Update all animation parameters based on current movement state
        /// Call this from your movement controller each frame
        /// </summary>
        public void UpdateAnimations(Vector3 velocity, bool isGrounded, bool isSprinting)
        {
            if (!enableAnimations || animator == null)
                return;
                
            // Calculate movement values from velocity (PlayerNetworkController no longer does this)
            float speed = new Vector3(velocity.x, 0, velocity.z).magnitude;
            bool isMoving = speed > minSpeedForMovement;
            bool isJumping = !isGrounded && velocity.y > 0f;
                
            // Update continuous parameters
            UpdateMovementParameters(speed, isMoving, isSprinting);
            UpdateStateParameters(isGrounded, isMoving, isSprinting, isJumping);
            UpdateSwingState();
            
            // Store current state for next frame comparison
            wasGrounded = isGrounded;
            wasMoving = isMoving;
            wasSprinting = isSprinting;
        }
        
        /// <summary>
        /// Trigger swing animation with cooldown check
        /// </summary>
        public bool TriggerSwing()
        {
            if (!enableAnimations || animator == null)
                return false;
                
            // Check cooldown
            float timeSinceLastSwing = Time.time - lastSwingTime;
            if (timeSinceLastSwing < swingCooldown)
            {
                return false;
            }
                
            // Start swing animation
            isSwinging = true;
            lastSwingTime = Time.time;
            animator.SetBool(swingBoolName, true);
            return true;
        }
        
        /// <summary>
        /// Trigger taunt animation by number (1-3)
        /// </summary>
        public void TriggerTaunt(int tauntNumber)
        {
            if (!enableAnimations || animator == null)
                return;
                
            string triggerName = tauntNumber switch
            {
                1 => taunt1TriggerName,
                2 => taunt2TriggerName,
                3 => taunt3TriggerName,
                _ => null
            };
            
            if (triggerName != null)
            {
                animator.SetTrigger(triggerName);
            }
        }
        
        /// <summary>
        /// Trigger random taunt animation
        /// </summary>
        public void TriggerRandomTaunt()
        {
            int randomTaunt = Random.Range(1, 4); // 1, 2, or 3
            TriggerTaunt(randomTaunt);
        }
        
        /// <summary>
        /// Check if swing is currently on cooldown
        /// </summary>
        public bool IsSwingOnCooldown()
        {
            return (Time.time - lastSwingTime) < swingCooldown;
        }
        
        /// <summary>
        /// Get remaining cooldown time for swing
        /// </summary>
        public float GetSwingCooldownRemaining()
        {
            float remaining = swingCooldown - (Time.time - lastSwingTime);
            return Mathf.Max(0f, remaining);
        }
        
        #endregion
        
        #region Private Methods
        
        private void UpdateMovementParameters(float speed, bool isMoving, bool isSprinting)
        {
            // Calculate target values
            float targetBlend = 0f;
            float targetSpeed = 0f;
            
            if (isMoving && speed > minSpeedForMovement)
            {
                if (isSprinting)
                {
                    targetBlend = 1f; // Sprint blend
                    targetSpeed = useAnimatorSpeed ? sprintAnimationSpeed : speed;
                }
                else
                {
                    targetBlend = 0.5f; // Walk blend  
                    targetSpeed = useAnimatorSpeed ? walkAnimationSpeed : speed;
                }
            }
            
            // Smooth the values
            currentBlendValue = Mathf.Lerp(currentBlendValue, targetBlend, Time.deltaTime / animationSmoothTime);
            currentSpeedValue = Mathf.Lerp(currentSpeedValue, targetSpeed, Time.deltaTime / animationSmoothTime);
            
            // Apply to animator
            animator.SetFloat(blendParameterName, currentBlendValue);
            animator.SetFloat(speedParameterName, currentSpeedValue);
        }
        
        private void UpdateStateParameters(bool isGrounded, bool isMoving, bool isSprinting, bool isJumping)
        {
            // Update boolean parameters
            animator.SetBool(groundedBoolName, isGrounded);
            animator.SetBool(jumpBoolName, isJumping);
            
            // Update optional parameters if they exist
            if (HasParameter(isGroundedParameterName))
                animator.SetBool(isGroundedParameterName, isGrounded);
                
            if (HasParameter(isMovingParameterName))
                animator.SetBool(isMovingParameterName, isMoving);
                
            if (HasParameter(isSprintingParameterName))
                animator.SetBool(isSprintingParameterName, isSprinting);
        }
        
        private void UpdateSwingState()
        {
            if (!isSwinging) return;
            
            // Auto-stop swing after a reasonable duration (adjust as needed)
            float swingDuration = 0.8f; // Adjust this to match your swing animation length
            if (Time.time - lastSwingTime > swingDuration)
            {
                isSwinging = false;
                animator.SetBool(swingBoolName, false);
            }
        }
        
        private bool HasParameter(string paramName)
        {
            if (animator == null || string.IsNullOrEmpty(paramName))
                return false;
                
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == paramName)
                    return true;
            }
            return false;
        }
        
        private void ValidateAnimatorParameters()
        {
            if (animator == null) return;
            
            Debug.Log("🎭 PlayerAnimationController: Validating animator parameters...");
            
            // Check required parameters
            string[] requiredParams = { blendParameterName, speedParameterName, groundedBoolName, jumpBoolName, swingBoolName };
            foreach (string param in requiredParams)
            {
                if (!HasParameter(param))
                {
                    Debug.LogWarning($"🎭 Missing required animator parameter: {param}");
                }
            }
            
            // Check trigger parameters (only taunts now - swing converted to bool)
            string[] triggerParams = { taunt1TriggerName, taunt2TriggerName, taunt3TriggerName };
            foreach (string param in triggerParams)
            {
                if (!HasParameter(param))
                {
                    Debug.LogWarning($"🎭 Missing trigger parameter: {param}");
                }
            }
        }
        
        #endregion
    }
}
