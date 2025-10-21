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
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float airAcceleration = 25f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float maxSpeed = 8f;

    [Header("Air Control")]
    [SerializeField] private float airStrafeMultiplier = 0.3f;
    [SerializeField] private float airRedirectSpeed = 5f; // How fast velocity direction changes in air
    [SerializeField] private float groundDrag = 0f;
    [SerializeField] private float airDrag = 0.5f;
    
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float airRotationSpeed = 15f;
    [SerializeField] private float minSpeedForRotation = 0.1f; // Minimum speed before rotating

    // State
    [SerializeField] private bool _isGrounded;
    private InputPayloadNetwork _networkSync;

    public float VelocityMagnitude => new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;
    public bool IsGrounded => _isGrounded;
    public Rigidbody Rigidbody => rb;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"🎮 Player spawned! IsOwner: {IsOwner}, IsServer: {IsServer}, IsHost: {IsHost}, ClientId: {OwnerClientId}");
        
        // Configure Rigidbody
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.useGravity = true;
        rb.linearDamping = 0f;
        rb.isKinematic = false;

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

        // Ground check
        _isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundDistance, groundMask);
        
        // Apply drag
        rb.linearDamping = _isGrounded ? groundDrag : airDrag;
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
    /// Processes movement based on input. Called by InputPayloadNetwork for prediction and reconciliation.
    /// </summary>
    public void ProcessMovement(InputPayload input)
    {
        Vector3 currentVelocity = rb.linearVelocity;
        
        if (_isGrounded)
        {
            // GROUNDED: Full control over movement direction
            Vector3 targetVelocity = input.InputVector * moveSpeed;
            
            // Calculate horizontal velocity change
            Vector3 horizontalVel = new Vector3(currentVelocity.x, 0, currentVelocity.z);
            Vector3 targetHorizontalVel = new Vector3(targetVelocity.x, 0, targetVelocity.z);
            
            // Apply acceleration
            Vector3 velocityChange = (targetHorizontalVel - horizontalVel) * acceleration * Time.fixedDeltaTime;
            
            // Clamp to max speed (horizontal only)
            Vector3 newHorizontalVel = horizontalVel + velocityChange;
            if (newHorizontalVel.magnitude > maxSpeed)
            {
                newHorizontalVel = newHorizontalVel.normalized * maxSpeed;
            }

            // Apply velocity
            rb.linearVelocity = new Vector3(newHorizontalVel.x, currentVelocity.y, newHorizontalVel.z);
        }
        else
        {
            // AIRBORNE: Redirect velocity toward camera direction while maintaining speed
            if (input.CameraRotation != Quaternion.identity)
            {
                // Get camera's forward direction (flattened to horizontal plane)
                Vector3 cameraForward = input.CameraRotation * Vector3.forward;
                cameraForward.y = 0;
                cameraForward.Normalize();
                
                // Get current horizontal velocity
                Vector3 horizontalVel = new Vector3(currentVelocity.x, 0, currentVelocity.z);
                float currentSpeed = horizontalVel.magnitude;
                
                if (currentSpeed > 0.1f && cameraForward.sqrMagnitude > 0.01f)
                {
                    // Calculate target direction (camera forward)
                    Vector3 targetDirection = cameraForward;
                    
                    // Current direction
                    Vector3 currentDirection = horizontalVel.normalized;
                    
                    // Smoothly rotate velocity direction toward camera direction
                    Vector3 newDirection = Vector3.Slerp(
                        currentDirection, 
                        targetDirection, 
                        Time.fixedDeltaTime * airRedirectSpeed
                    );
                    
                    // Apply new direction while maintaining speed
                    Vector3 newHorizontalVel = newDirection * currentSpeed;
                    rb.linearVelocity = new Vector3(newHorizontalVel.x, currentVelocity.y, newHorizontalVel.z);
                }
            }
        }

        // Jumping
        if (input.Jump && _isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
        }

        // CHANGED: Player ALWAYS rotates to face movement direction (velocity)
        Vector3 movementDirection = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        
        // Only rotate if moving fast enough (prevents jittering when idle)
        if (movementDirection.sqrMagnitude > minSpeedForRotation * minSpeedForRotation)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movementDirection.normalized);
            
            // Use different rotation speeds for ground vs air
            float rotSpeed = _isGrounded ? rotationSpeed : airRotationSpeed;
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * rotSpeed);
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

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