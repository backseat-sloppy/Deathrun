using UnityEngine;
using Unity.Netcode; // Add this

public class PlayerMovement : NetworkBehaviour // Change from MonoBehaviour
{
    public CharacterController controller;
    public Animator animator;
    public float speed = 6f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;

    private Vector3 velocity;
    private bool isGrounded;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    public Transform cameraTransform;
    public float rotationSpeed = 10f;

    public SwordHitbox swordHitbox;
    private void Start()
    {
        Debug.Log("I am now DJ");
    }
    void Update()
    {
        if (!IsOwner) return; // Add this line - only owner controls input

        // Check if the player is grounded
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // Reset downward velocity when grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Handle sword swing animation trigger
        if (Input.GetMouseButtonDown(0)) // Left-click
        {
            animator.SetTrigger("Swing");
           
        }

        // Handle movement input (horizontal and vertical axes)
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        // Normalize the direction vectors to avoid unequal movement speeds
        cameraForward.Normalize();
        cameraRight.Normalize();

        // Calculate the movement direction relative to the camera orientation
        Vector3 move = cameraForward * z + cameraRight * x;

        // If there is movement input, rotate the player to face that direction
        if (move.magnitude > 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            controller.Move(move * speed * Time.deltaTime);
            animator.SetFloat("Speed", move.magnitude);
        }
        else
        {
            animator.SetFloat("Speed", 0f);
        }

        // Handle jumping logic (check if grounded and jump if pressed)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Handle sword hitbox enabling/disabling based on the current animation state
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0); // Layer 0

        // Debugging the current state and normalizedTime
      
        // Check if the Swing animation is playing
        if (stateInfo.IsName("Armature|Swing") && stateInfo.normalizedTime >= 0.1f && stateInfo.normalizedTime <= 0.9f) // Adjust time as needed
        {
            if (!swordHitbox.IsHitboxActive)
            {
                swordHitbox.EnableHitbox();
            }
        }
        else
        {
            if (swordHitbox.IsHitboxActive)
            {
                swordHitbox.DisableHitbox();
            }
        }
    }
}











