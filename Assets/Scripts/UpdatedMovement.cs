using UnityEngine;
using Unity.Netcode;
public class UpdatedMovement : NetworkBehaviour
{

    [SerializeField] private LayerMask groundMask;
    [SerializeField] private CharacterController controller;
    [SerializeField] private Animator animator;

    // Movement parameters
    [SerializeField] private float jumpForce;
    public float BaseSpeed = 6f;
    private float MoveSpeed = 0f;
    [SerializeField] private float gravity = -9.81f;

    // movement Modifying parameters
    [SerializeField] private float airControlMultiplier = 0.5f;
    [SerializeField] private float airStrafeMultiplier = 0.5f;
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [SerializeField] private float friction = 0.1f;
    [SerializeField] private float airFriction = 0.05f;
    [SerializeField] private float airStrafeBase = 0.02f;

    [SerializeField] private Vector3 velocity;
    [SerializeField] private bool isGrounded;


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
        // Jumping
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -1f; // Small negative value to keep grounded
            velocity.x -= friction;
            velocity.z -= friction;
            
        }
        
        if (isGrounded)
        {
            airStrafeMultiplier = 0.5f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        // Calculate direction relative to camera
        Vector3 moveDirection = cameraTransform.right * x + cameraTransform.forward * z;
        moveDirection.y = 0; // Keep movement horizontal
        moveDirection.Normalize();
        // Adjust speed based on state
        float currentSpeed = MoveSpeed;
        if (!isGrounded)
        {

            if ((x > 0 && Vector3.Dot(cameraTransform.right, moveDirection) > 0) || (x < 0 && Vector3.Dot(-cameraTransform.right, moveDirection) > 0))
            {
                airStrafeMultiplier += airStrafeBase;
            }


            moveDirection.x *= airStrafeMultiplier;
            moveDirection.z *= airStrafeMultiplier;
        }

        // Apply movement
        Vector3 desiredVelocity = moveDirection * currentSpeed;
        velocity.x = Mathf.Lerp(velocity.x, desiredVelocity.x, 1);
        velocity.z = Mathf.Lerp(velocity.z, desiredVelocity.z, 1);

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        // Move the character
        controller.Move(velocity * Time.deltaTime);
        // Update animator parameters
        animator.SetFloat("Speed", new Vector3(velocity.x, 0, velocity.z).magnitude);
        animator.SetBool("isGrounded", isGrounded);
    }
}




