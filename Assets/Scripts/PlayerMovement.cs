using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller; // Reference to the Character Controller component
    public Animator animator; // Reference to the Animator component
    public float speed = 6f; // Movement speed
    public float gravity = -9.81f; // Gravity force
    public float jumpHeight = 2f; // Jump height

    private Vector3 velocity; // Stores current velocity
    private bool isGrounded; // Checks if the player is on the ground

    public Transform groundCheck; // Position to check for ground
    public float groundDistance = 0.4f; // Radius of the ground check sphere
    public LayerMask groundMask; // Layer mask for the ground

    public Transform cameraTransform; // Reference to the camera transform
    public float rotationSpeed = 10f; // Speed for rotation smoothing

    public SwordHitbox swordHitbox; // Reference to the SwordHitbox component (attached to the sword)

    void Update()
    {
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
            Debug.Log("Swing Triggered");  // Debug log to confirm the trigger is set
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
        Debug.Log("Current Animator State: " + stateInfo.fullPathHash);
        Debug.Log("Normalized Time: " + stateInfo.normalizedTime);

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











