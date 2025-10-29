using UnityEngine;

namespace DeathrunGame
{
    /// <summary>
    /// Input aggregation system that produces compact InputTick structs for networking.
    /// Handles WASD movement, sprint, jump with buffering and caching for smooth input processing.
    /// </summary>
    public class PlayerInput : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Input Configuration")]
        [SerializeField] private string horizontalAxisName = "Horizontal";
        [SerializeField] private string verticalAxisName = "Vertical";
        [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;
        [SerializeField] private KeyCode swingKey = KeyCode.Mouse0;  // Left mouse button
        [SerializeField] private KeyCode taunt1Key = KeyCode.Alpha1;
        [SerializeField] private KeyCode taunt2Key = KeyCode.Alpha2;
        [SerializeField] private KeyCode taunt3Key = KeyCode.Alpha3;
        [SerializeField] private KeyCode randomTauntKey = KeyCode.T;
        
        [Header("Input Processing")]
        [SerializeField] private float inputDeadzone = 0.1f;
        [SerializeField] private bool normalizeInput = true;
        [SerializeField] private float inputSmoothTime = 0f;    // Disabled for instant response
        
        [Header("Jump Buffering")]
        [SerializeField] private float jumpBufferDuration = 0.1f;
        [SerializeField] private bool enableJumpBuffer = true;
        
        #endregion

        #region Private Fields
        
        // Raw input values
        private Vector2 rawMoveInput = Vector2.zero;
        private Vector2 smoothedMoveInput = Vector2.zero;
        private Vector2 inputVelocity = Vector2.zero;
        
        // Button states
        private bool isSprintPressed = false;
        private bool wasSprintPressed = false;
        private bool isJumpPressed = false;
        private bool wasJumpPressed = false;
        private bool isSwingPressed = false;
        private bool wasSwingPressed = false;
        private int currentTauntInput = 0;  // 0 = no taunt, 1-3 = taunt number
        
        // Jump buffering
        private float jumpBufferTimer = 0f;
        private bool jumpBuffered = false;
        
        // Frame tracking
        private bool inputProcessedThisFrame = false;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Current smoothed movement input as Vector2 (x = horizontal, y = vertical)
        /// </summary>
        public Vector2 MovementInput => smoothedMoveInput;
        
        /// <summary>
        /// Raw unfiltered movement input
        /// </summary>
        public Vector2 RawMovementInput => rawMoveInput;
        
        /// <summary>
        /// Whether sprint is currently being held
        /// </summary>
        public bool IsSprinting => isSprintPressed;
        
        /// <summary>
        /// Whether sprint was just pressed this frame
        /// </summary>
        public bool SprintPressed => isSprintPressed && !wasSprintPressed;
        
        /// <summary>
        /// Whether sprint was just released this frame
        /// </summary>
        public bool SprintReleased => !isSprintPressed && wasSprintPressed;
        
        /// <summary>
        /// Whether jump was pressed this frame or is buffered
        /// </summary>
        public bool JumpPressed => (isJumpPressed && !wasJumpPressed) || jumpBuffered;
        
        /// <summary>
        /// Whether jump is currently being held
        /// </summary>
        public bool IsJumpHeld => isJumpPressed;
        
        /// <summary>
        /// Movement input as Vector3 for world space (x, 0, z)
        /// </summary>
        public Vector3 MovementVector3 => new Vector3(smoothedMoveInput.x, 0f, smoothedMoveInput.y);
        
        #endregion

        #region Unity Lifecycle
        
        private void Update()
        {
            // Store previous frame state
            wasSprintPressed = isSprintPressed;
            wasJumpPressed = isJumpPressed;
            wasSwingPressed = isSwingPressed;
            
            // Gather raw input
            GatherRawInput();
            
            // Process movement input
            ProcessMovementInput();
            
            // Handle jump buffering
            ProcessJumpBuffer();
            
            // Reset frame flag
            inputProcessedThisFrame = false;
        }

        #endregion

        #region Input Processing
        
        private void GatherRawInput()
        {
            // Movement input - Use GetAxisRaw for instant response (no smoothing)
            float horizontal = Input.GetAxisRaw(horizontalAxisName);
            float vertical = Input.GetAxisRaw(verticalAxisName);
            rawMoveInput = new Vector2(horizontal, vertical);
            
            // Button input
            isSprintPressed = Input.GetKey(sprintKey);
            isJumpPressed = Input.GetKey(jumpKey);
            isSwingPressed = Input.GetKeyDown(swingKey); // Use GetKeyDown for single press
            
            // Taunt input - Check for specific taunt keys
            currentTauntInput = 0; // Reset each frame
            if (Input.GetKeyDown(taunt1Key))
                currentTauntInput = 1;
            else if (Input.GetKeyDown(taunt2Key))
                currentTauntInput = 2;
            else if (Input.GetKeyDown(taunt3Key))
                currentTauntInput = 3;
            else if (Input.GetKeyDown(randomTauntKey))
                currentTauntInput = UnityEngine.Random.Range(1, 4); // Random 1-3
            
            // Jump buffering
            if (Input.GetKeyDown(jumpKey) && enableJumpBuffer)
            {
                jumpBufferTimer = jumpBufferDuration;
                jumpBuffered = true;
            }
        }

        private void ProcessMovementInput()
        {
            Vector2 targetInput = rawMoveInput;
            
            // Apply deadzone
            if (targetInput.magnitude < inputDeadzone)
            {
                targetInput = Vector2.zero;
            }
            
            // Normalize if enabled
            if (normalizeInput && targetInput.magnitude > 1f)
            {
                targetInput = targetInput.normalized;
            }
            
            // Smooth input if smoothing is enabled
            if (inputSmoothTime > 0f)
            {
                smoothedMoveInput = Vector2.SmoothDamp(smoothedMoveInput, targetInput, 
                    ref inputVelocity, inputSmoothTime);
            }
            else
            {
                smoothedMoveInput = targetInput;
            }
        }

        private void ProcessJumpBuffer()
        {
            // Update jump buffer timer
            if (jumpBufferTimer > 0f)
            {
                jumpBufferTimer -= Time.deltaTime;
                
                if (jumpBufferTimer <= 0f)
                {
                    jumpBuffered = false;
                }
            }
        }

        #endregion

        #region Public Interface
        
        /// <summary>
        /// Gets the current input state as an InputTick struct for networking.
        /// Should be called once per frame by the PlayerNetworkController.
        /// </summary>
        /// <returns>Complete input state for this frame</returns>
        public InputTick GetCurrentInput()
        {
            // Prevent multiple calls per frame
            if (inputProcessedThisFrame)
            {
                Debug.LogWarning("GetCurrentInput() called multiple times in one frame!");
            }
            
            var input = new InputTick
            {
                MoveDirection = MovementVector3,
                Jump = ConsumeJumpInput(),
                Sprint = IsSprinting,
                Swing = ConsumeSwingInput(),
                Taunt = ConsumeTauntInput(),
                // SequenceId and Timestamp will be set by PlayerNetworkController
                // WasGrounded will be set by PlayerNetworkController
            };
            
            inputProcessedThisFrame = true;
            return input;
        }

        /// <summary>
        /// Consumes buffered jump input. Returns true if jump should be processed.
        /// This ensures jump input is only consumed once per buffer.
        /// </summary>
        /// <returns>True if jump should be processed this frame</returns>
        public bool ConsumeJumpInput()
        {
            bool shouldJump = JumpPressed;
            
            if (shouldJump && jumpBuffered)
            {
                // Consume the buffered jump
                jumpBuffered = false;
                jumpBufferTimer = 0f;
            }
            
            return shouldJump;
        }

        /// <summary>
        /// Manually clear the jump buffer. Useful for when jump is consumed elsewhere.
        /// </summary>
        public void ClearJumpBuffer()
        {
            jumpBuffered = false;
            jumpBufferTimer = 0f;
        }

        /// <summary>
        /// Consumes swing input. Returns true if swing should be processed.
        /// This ensures swing input is only consumed once per press.
        /// </summary>
        /// <returns>True if swing should be processed this frame</returns>
        public bool ConsumeSwingInput()
        {
            bool shouldSwing = isSwingPressed;
            isSwingPressed = false; // Consume the input
            return shouldSwing;
        }

        /// <summary>
        /// Consumes taunt input. Returns the taunt number (1-3) or 0 if no taunt.
        /// This ensures taunt input is only consumed once per press.
        /// </summary>
        /// <returns>Taunt number (1-3) or 0 if no taunt</returns>
        public int ConsumeTauntInput()
        {
            int tauntNumber = currentTauntInput;
            currentTauntInput = 0; // Consume the input
            return tauntNumber;
        }

        /// <summary>
        /// Check if there's a valid movement input (above deadzone)
        /// </summary>
        /// <returns>True if player is providing movement input</returns>
        public bool HasMovementInput()
        {
            return smoothedMoveInput.magnitude > inputDeadzone;
        }

        /// <summary>
        /// Get movement input magnitude (0-1 range)
        /// </summary>
        /// <returns>Input magnitude</returns>
        public float GetMovementMagnitude()
        {
            return smoothedMoveInput.magnitude;
        }

        /// <summary>
        /// Get normalized movement direction
        /// </summary>
        /// <returns>Normalized movement direction or Vector2.zero if no input</returns>
        public Vector2 GetMovementDirection()
        {
            return smoothedMoveInput.normalized;
        }

        #endregion

        #region Configuration
        
        /// <summary>
        /// Set input axis names at runtime (useful for input remapping)
        /// </summary>
        /// <param name="horizontal">Horizontal axis name</param>
        /// <param name="vertical">Vertical axis name</param>
        public void SetAxisNames(string horizontal, string vertical)
        {
            horizontalAxisName = horizontal;
            verticalAxisName = vertical;
        }

        /// <summary>
        /// Set key bindings at runtime (useful for input remapping)
        /// </summary>
        /// <param name="sprint">Sprint key</param>
        /// <param name="jump">Jump key</param>
        public void SetKeyBindings(KeyCode sprint, KeyCode jump)
        {
            sprintKey = sprint;
            jumpKey = jump;
        }

        /// <summary>
        /// Configure input deadzone
        /// </summary>
        /// <param name="deadzone">New deadzone value (0-1)</param>
        public void SetDeadzone(float deadzone)
        {
            inputDeadzone = Mathf.Clamp01(deadzone);
        }

        /// <summary>
        /// Configure input smoothing
        /// </summary>
        /// <param name="smoothTime">Smooth time in seconds (0 = no smoothing)</param>
        public void SetInputSmoothing(float smoothTime)
        {
            inputSmoothTime = Mathf.Max(0f, smoothTime);
        }

        #endregion

        #region Debug Visualization
        
        private void OnGUI()
        {
            if (!Application.isEditor || !enabled) return;
            
            // Show input debug info in editor
            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("Box");
            
            GUILayout.Label("Player Input Debug", EditorGUIStyle.boldLabel);
            GUILayout.Space(5);
            
            GUILayout.Label($"Raw Input: ({rawMoveInput.x:F2}, {rawMoveInput.y:F2})");
            GUILayout.Label($"Smoothed: ({smoothedMoveInput.x:F2}, {smoothedMoveInput.y:F2})");
            GUILayout.Label($"Magnitude: {smoothedMoveInput.magnitude:F2}");
            GUILayout.Space(5);
            
            GUILayout.Label($"Sprint: {(IsSprinting ? "HELD" : "---")}");
            GUILayout.Label($"Jump: {(IsJumpHeld ? "HELD" : "---")}");
            GUILayout.Label($"Jump Buffered: {(jumpBuffered ? "YES" : "NO")}");
            GUILayout.Label($"Buffer Timer: {jumpBufferTimer:F2}s");
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        #endregion

        #region Helper Styles
        
        private static class EditorGUIStyle
        {
            private static GUIStyle _boldLabel;
            public static GUIStyle boldLabel
            {
                get
                {
                    if (_boldLabel == null)
                    {
                        _boldLabel = new GUIStyle(GUI.skin.label);
                        _boldLabel.fontStyle = FontStyle.Bold;
                    }
                    return _boldLabel;
                }
            }
        }

        #endregion
    }
}