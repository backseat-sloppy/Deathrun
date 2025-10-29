using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Handles owner-specific input, camera control, and local movement logic.
/// Only the owner can control this player.
/// </summary>
public class PlayerMovementController : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private CapsuleCollider capsuleCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform cameraTransform;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;

    [Header("Movement Parameters")]
    [SerializeField] private float maxSpeed = 4f; // Reduced for smaller map
    [SerializeField] private float groundAcceleration = 200f; // Keep instant response
    [SerializeField] private float groundDeceleration = 500f; // Keep instant stopping
    [SerializeField] private float airAcceleration = 10f; // Slightly reduced air control
    [SerializeField] private float jumpForce = 8f; // Reduced jump height for smaller map

    [Header("Physics Settings")]
    [SerializeField] private float groundFriction = 10f; // Much higher friction coefficient
    [SerializeField] private float airResistance = 0.1f;
    
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float airRotationSpeed = 15f;
    [SerializeField] private float minSpeedForRotation = 0.1f;
    [SerializeField] private bool rotateToInput = true; // Rotate to input direction instead of velocity

    [Header("Animation")]
    [SerializeField] private float swingCooldown = 1.5f;

    // State
    [SerializeField] private bool _isGrounded;
    private InputPayloadNetwork _networkSync;
    private float _lastSwingTime = -999f;
    private Vector3 _lastInputDirection;

    public float VelocityMagnitude => new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;
    public bool IsGrounded => _isGrounded;
    public Rigidbody Rigidbody => rb;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"🎮 Player spawned! IsOwner: {IsOwner}, IsServer: {IsServer}, IsHost: {IsHost}, ClientId: {OwnerClientId}");
        
        // Configure Rigidbody for deterministic physics
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.useGravity = true;
        rb.linearDamping = 0f; // No built-in damping - we handle friction manually
        rb.angularDamping = 0f;
        rb.isKinematic = false;
        rb.mass = 1f;

        // Get network sync component
        _networkSync = GetComponent<InputPayloadNetwork>();

        if (!IsOwner)
        {
            // Disable camera for remote players
            if (cameraTransform != null)
            {
                cameraTransform.gameObject.SetActive(false);
                Debug.Log("👁️ Disabled camera for remote player");
            }
        }
        else
        {
            Debug.Log($"✅ This is MY player - IsHost: {IsHost}");
        }
    }

    private void Update()
    {
        // Only the owner can provide input
        if (!IsOwner) return;

        // Handle animation inputs
        HandleAnimationInput();

        // Gather input
        InputPayload input = new InputPayload
        {
            Tick = _networkSync.CurrentTick,
            InputVector = GetMovementInput(),
            Jump = Input.GetButtonDown("Jump") && _isGrounded,
            CameraRotation = cameraTransform != null ? cameraTransform.rotation : Quaternion.identity
        };

        // Send input to network synchronization system
        _networkSync.ProcessInput(input);

        // Update animator
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        // Update ground detection
        _isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundDistance, groundMask);
    }

    private void HandleAnimationInput()
    {
        // Swing on mouse click (left mouse button) with cooldown
        if (Input.GetMouseButtonDown(0))
        {
            if (Time.time >= _lastSwingTime + swingCooldown)
            {
                TriggerSwingServerRpc();
                _lastSwingTime = Time.time;
            }
        }

        // Taunts on number keys 1, 2, 3
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TriggerTauntServerRpc(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TriggerTauntServerRpc(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TriggerTauntServerRpc(3);
        }
    }

    [ServerRpc]
    private void TriggerSwingServerRpc()
    {
        // Trigger animation on all clients
        TriggerSwingClientRpc();
    }

    [ClientRpc]
    private void TriggerSwingClientRpc()
    {
        if (animator != null)
        {
            animator.SetTrigger("Swing");
        }
    }

    [ServerRpc]
    private void TriggerTauntServerRpc(int tauntNumber)
    {
        // Trigger animation on all clients
        TriggerTauntClientRpc(tauntNumber);
    }

    [ClientRpc]
    private void TriggerTauntClientRpc(int tauntNumber)
    {
        if (animator != null)
        {
            animator.SetTrigger($"Taunt{tauntNumber}");
        }
    }

    private Vector3 GetMovementInput()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        if (cameraTransform == null) return Vector3.zero;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        return (forward * z + right * x).normalized;
    }

    /// <summary>
    /// Processes player movement using physics-based forces.
    /// Called by the networking system for client-side prediction and server reconciliation.
    /// </summary>
    /// <param name="input">Validated input from the network synchronization system</param>
    public void ProcessMovement(InputPayload input)
    {
        Vector3 inputVector = ClampInputMagnitude(input.InputVector);
        
        // Update rotation direction cache
        if (inputVector.magnitude > 0.01f)
        {
            _lastInputDirection = inputVector;
        }
        
        // Apply movement forces based on grounded state
        if (_isGrounded)
        {
            ApplyGroundMovement(inputVector);
        }
        else
        {
            ApplyAirMovement(inputVector);
        }
        
        // Handle jumping
        if (input.Jump && _isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        
        // Update player rotation
        UpdatePlayerRotation();
    }

    /// <summary>
    /// Applies physics-based ground movement using acceleration and friction forces
    /// </summary>
    private void ApplyGroundMovement(Vector3 inputVector)
    {
        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
        
        if (inputVector.magnitude > 0.01f)
        {
            // Calculate target velocity and required acceleration
            Vector3 targetVelocity = inputVector * maxSpeed;
            Vector3 velocityChange = targetVelocity - horizontalVelocity;
            
            // Apply very strong acceleration force for near-instant response
            Vector3 accelerationForce = velocityChange * groundAcceleration;
            rb.AddForce(accelerationForce, ForceMode.Force);
            
            // Reduce friction when actively moving
            Vector3 frictionForce = -horizontalVelocity * groundFriction * 0.1f;
            rb.AddForce(frictionForce, ForceMode.Force);
        }
        else
        {
            // Apply very strong friction when no input for near-instant stopping
            Vector3 strongFriction = -horizontalVelocity * groundFriction * groundDeceleration;
            rb.AddForce(strongFriction, ForceMode.Force);
        }
    }
    
    /// <summary>
    /// Applies physics-based air movement with limited control
    /// </summary>
    private void ApplyAirMovement(Vector3 inputVector)
    {
        if (inputVector.magnitude > 0.01f)
        {
            Vector3 targetVelocity = inputVector * maxSpeed;
            Vector3 currentHorizontal = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            Vector3 velocityChange = targetVelocity - currentHorizontal;
            
            // Limit air control - only apply force in beneficial directions
            if (Vector3.Dot(velocityChange, inputVector) > 0)
            {
                Vector3 airForce = velocityChange * airAcceleration;
                rb.AddForce(airForce, ForceMode.Force);
            }
        }
        
        // Apply air resistance
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        if (horizontalVel.magnitude > 0.01f)
        {
            Vector3 airResistanceForce = -horizontalVel * airResistance;
            rb.AddForce(airResistanceForce, ForceMode.Force);
        }
    }
    
    /// <summary>
    /// Updates player rotation to face movement direction
    /// </summary>
    private void UpdatePlayerRotation()
    {
        Vector3 rotationDirection = Vector3.zero;
        
        // Prefer input direction, fall back to movement direction
        if (_lastInputDirection.magnitude > 0.1f)
        {
            rotationDirection = _lastInputDirection;
        }
        else
        {
            Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            if (horizontalVel.magnitude > minSpeedForRotation)
            {
                rotationDirection = horizontalVel.normalized;
            }
        }
        
        if (rotationDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(rotationDirection);
            float rotSpeed = _isGrounded ? rotationSpeed : airRotationSpeed;
            
            rb.rotation = Quaternion.RotateTowards(rb.rotation, targetRotation, 
                rotSpeed * 90f * Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// Ensures input vector magnitude doesn't exceed 1.0 to prevent exploitation
    /// </summary>
    private Vector3 ClampInputMagnitude(Vector3 input)
    {
        return input.magnitude > 1f ? input.normalized : input;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        // Use actual velocity for immediate animation response
        animator.SetFloat("Speed", VelocityMagnitude);
        animator.SetBool("isGrounded", _isGrounded);
    }

    private void OnDrawGizmos()
    {
        if (groundCheck == null) return;
        
        // Draw the raycast line (always visible)
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(groundCheck.position, Vector3.down * groundDistance);
        
        // Draw a sphere at the raycast origin
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, 0.1f);
        
        // Draw a sphere at the raycast end point
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position + Vector3.down * groundDistance, 0.05f);
        
        // Draw velocity direction (magenta arrow) - This is where player faces
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        if (horizontalVel.sqrMagnitude > 0.1f)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position + Vector3.up * 1.2f, horizontalVel.normalized * 2f);
            
            // Draw a label showing this is movement direction
            #if UNITY_EDITOR
            UnityEngine.GUIStyle velStyle = new UnityEngine.GUIStyle();
            velStyle.normal.textColor = Color.magenta;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f + horizontalVel.normalized * 2f, "Movement Direction", velStyle);
            #endif
        }
        
        // Draw camera forward direction (blue arrow)
        if (cameraTransform != null)
        {
            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();
            
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position + Vector3.up * 1f, cameraForward * 2f);
            
            // Draw a label
            #if UNITY_EDITOR
            UnityEngine.GUIStyle camStyle = new UnityEngine.GUIStyle();
            camStyle.normal.textColor = Color.blue;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f + cameraForward * 2f, "Camera Forward", camStyle);
            #endif
        }
        
        // Draw player forward direction (cyan arrow) - Where model faces
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position + Vector3.up * 1f, transform.forward * 1.5f);
        
        // Draw status label
        #if UNITY_EDITOR
        UnityEngine.GUIStyle style = new UnityEngine.GUIStyle();
        style.normal.textColor = _isGrounded ? Color.green : Color.red;
        UnityEditor.Handles.Label(groundCheck.position + Vector3.up * 0.5f, 
            _isGrounded ? "GROUNDED" : "AIRBORNE (AIR REDIRECT)", style);
        #endif
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        
        // Draw a transparent cylinder to show the ground check area
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * groundDistance);
    }
}