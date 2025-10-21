using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Handles player input and physics-based movement
/// ONLY executes for the owner - completely secure
/// </summary>
public class PlayerController : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody rb;
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
    [SerializeField] private float groundDrag = 6f;
    [SerializeField] private float airDrag = 0.5f;

    // Public state accessors for NetworkedPlayerState
    public bool IsGrounded { get; private set; }
    public Vector3 Velocity => rb.linearVelocity;
    public float HorizontalSpeed => new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;

    // Input cache
    private Vector2 _moveInput;
    private bool _jumpPressed;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (rb == null) rb = GetComponent<Rigidbody>();

        // Configure physics
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.useGravity = true;
        rb.linearDamping = 0f;
        rb.isKinematic = false;

        // Security: Disable camera for non-owners
        if (!IsOwner && cameraTransform != null)
        {
            cameraTransform.gameObject.SetActive(false);
            Debug.Log("??? Camera disabled for remote player");
        }

        // Disable this component entirely for non-owners
        if (!IsOwner)
        {
            enabled = false;
        }
    }

    private void Update()
    {
        // Security: Double-check ownership (should never be reached due to enabled = false)
        if (!IsOwner) return;

        // Gather input (only owner can do this)
        _moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        _jumpPressed = Input.GetButtonDown("Jump");
    }

    private void FixedUpdate()
    {
        // Security: Owner-only check
        if (!IsOwner) return;

        // Update ground state
        IsGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundDistance, groundMask);
        rb.linearDamping = IsGrounded ? groundDrag : airDrag;

        // Calculate camera-relative movement direction
        Vector3 moveDirection = GetCameraRelativeDirection();

        // Apply movement
        ApplyMovement(moveDirection);

        // Apply jump
        if (_jumpPressed && IsGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
        }

        // Apply rotation
        ApplyRotation(moveDirection);

        // Update animator
        UpdateAnimator();

        // Reset jump input
        _jumpPressed = false;
    }

    /// <summary>
    /// Calculate movement direction relative to camera
    /// </summary>
    private Vector3 GetCameraRelativeDirection()
    {
        if (cameraTransform == null) return Vector3.zero;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        return (forward * _moveInput.y + right * _moveInput.x).normalized;
    }

    /// <summary>
    /// Apply physics-based movement
    /// </summary>
    private void ApplyMovement(Vector3 moveDirection)
    {
        Vector3 currentVel = rb.linearVelocity;
        Vector3 targetVel = moveDirection * moveSpeed;

        float accel = IsGrounded ? acceleration : airAcceleration;

        Vector3 horizontalVel = new Vector3(currentVel.x, 0, currentVel.z);
        Vector3 targetHorizontalVel = new Vector3(targetVel.x, 0, targetVel.z);

        Vector3 velocityChange = (targetHorizontalVel - horizontalVel) * accel * Time.fixedDeltaTime;

        if (!IsGrounded)
        {
            velocityChange *= airStrafeMultiplier;
        }

        Vector3 newHorizontalVel = horizontalVel + velocityChange;
        if (newHorizontalVel.magnitude > maxSpeed)
        {
            newHorizontalVel = newHorizontalVel.normalized * maxSpeed;
        }

        rb.linearVelocity = new Vector3(newHorizontalVel.x, currentVel.y, newHorizontalVel.z);
    }

    /// <summary>
    /// Rotate player toward movement direction
    /// </summary>
    private void ApplyRotation(Vector3 moveDirection)
    {
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 10f);
        }
    }

    /// <summary>
    /// Update animator parameters
    /// </summary>
    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetFloat("Speed", HorizontalSpeed);
        animator.SetBool("isGrounded", IsGrounded);
    }

    /// <summary>
    /// Public method for NetworkedPlayerState to apply external state (reconciliation)
    /// </summary>
    public void ApplyState(Vector3 position, Vector3 velocity, Quaternion rotation)
    {
        transform.position = position;
        rb.linearVelocity = velocity;
        rb.rotation = rotation;
    }

    /// <summary>
    /// Public method for NetworkedPlayerState to get input for serialization
    /// </summary>
    public Vector3 GetCurrentMoveDirection()
    {
        return GetCameraRelativeDirection();
    }

    /// <summary>
    /// Public method to check if jump was pressed this frame
    /// </summary>
    public bool WasJumpPressed()
    {
        return _jumpPressed;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(groundCheck.position, Vector3.down * groundDistance);
    }
}