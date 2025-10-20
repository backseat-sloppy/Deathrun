using UnityEngine;
using Unity.Netcode;

public class UpdatedMovement : NetworkBehaviour
{
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private CharacterController controller;
    [SerializeField] private Animator animator;
     private Vector3 carriedMomentum;

    // Movement parameters
    [SerializeField] private float jumpForce;
    public float BaseSpeed = 6f;
    private float MoveSpeed;
    [SerializeField] private float gravity = -9.81f;

    // Movement Modifying parameters
    [SerializeField] private float airStrafeMultiplier = 0.5f;
    [SerializeField] private float airStrafeBase = 0.02f;
    [SerializeField] private float friction = 0.1f;

    public Vector3 velocity;       
    private bool isGrounded;
   [SerializeField] public float VelocityMagnitude => new Vector3(velocity.x, 0, velocity.z).magnitude;
    // Ground check parameters
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;

    // Camera reference for directing which way the velocity should take the player
    [SerializeField] private Transform cameraTransform;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"🎮 Player spawned! IsOwner: {IsOwner}, ClientId: {OwnerClientId}");
        if (!IsOwner)
        {
            if (cameraTransform != null)
            {
                cameraTransform.gameObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Ground check using Raycast for efficiency
        isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundDistance, groundMask);

        // Apply friction when grounded
        if (isGrounded)
        {
            ApplyFriction();
            MoveSpeed = BaseSpeed; // Reset base movement speed
            airStrafeMultiplier = 0.5f; // Reset air strafe multiplier
        }

        // Jumping
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        // Get input
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Calculate direction relative to camera
        Vector3 moveDirection = Vector3.zero;
        if (isGrounded)
        {
            moveDirection = cameraTransform.right * x + cameraTransform.forward * z;
            moveDirection.y = 0; // Keep movement horizontal
            moveDirection.Normalize();
            carriedMomentum = velocity; // Store current velocity as carried momentum
        }
        else
        {
            HandleAirMovement();
        }

        // Apply movement
        Vector3 desiredVelocity = moveDirection * MoveSpeed;
        velocity.x = desiredVelocity.x;
        velocity.z = desiredVelocity.z;

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;

        // Move the character
        controller.Move(velocity * Time.deltaTime);

        // Update animator parameters
        animator.SetFloat("Speed", new Vector3(velocity.x, 0, velocity.z).magnitude);
        animator.SetBool("isGrounded", isGrounded);
    }

    private void ApplyFriction()
    {
        // Apply friction to horizontal velocity
        velocity.x = Mathf.Lerp(velocity.x, 0, friction);
        velocity.z = Mathf.Lerp(velocity.z, 0, friction);
    }

    private void HandleAirMovement()
    {
        if (velocity.y < 0)
        {
            // Apply carried momentum when falling
            velocity.x += carriedMomentum.x;
            velocity.z += carriedMomentum.z;
            carriedMomentum = Vector3.zero; // Reset carried momentum after applying
        }

        // Use camera direction for air movement
        Vector3 cameraDirection = cameraTransform.forward;
        cameraDirection.y = 0; // Ignore vertical component
        cameraDirection.Normalize();

        // Apply air strafing mechanics
        velocity.x += cameraDirection.x * airStrafeMultiplier;
        velocity.z += cameraDirection.z * airStrafeMultiplier;

        // Increase air strafe multiplier for bunny hopping
        airStrafeMultiplier += airStrafeBase;
    }
}




