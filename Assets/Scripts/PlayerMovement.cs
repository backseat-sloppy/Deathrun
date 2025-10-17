using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
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

    [Header("Taunt System")]
    [SerializeField] private string[] Taunts;
    [SerializeField] private float tauntCooldown = 1f;

    private float lastTauntTime;

    // Called when player spawns on network
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"🎮 Player spawned! IsOwner: {IsOwner}, ClientId: {OwnerClientId}");

        if (!IsOwner)
        {
            if (cameraTransform != null)
            {
                cameraTransform.gameObject.SetActive(false);
                Debug.Log("👁️ Disabled camera for remote player");
            }
        }
        else
        {
            Debug.Log("✅ This is MY player - camera active");
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        // Null safety check
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // ========================================
        // SWING ANIMATION (Trigger - Auto Synced by NetworkAnimator)
        // ========================================
        if (Input.GetMouseButtonDown(0))
        {
            if (animator != null)
            {
                // This trigger is automatically synced to all clients by NetworkAnimator
                animator.SetTrigger("Swing");
                Debug.Log("⚔️ Swing animation triggered");

                // Note: Hitbox enabling is handled separately below
            }
        }

        // ========================================
        // TAUNT ANIMATIONS (Triggers - Auto Synced by NetworkAnimator)
        // ========================================
        HandleTauntInput();

        // ========================================
        // WALKING ANIMATION (Float - Auto Synced by NetworkAnimator)
        // ========================================
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        if (cameraTransform != null)
        {
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;

            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 move = cameraForward * z + cameraRight * x;

            if (move.magnitude > 0f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(move);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

                if (controller != null)
                {
                    controller.Move(move * speed * Time.deltaTime);
                }

                if (animator != null)
                {
                    // This float parameter is automatically synced by NetworkAnimator
                    // Remote players will see the walking animation based on this value
                    animator.SetFloat("Speed", move.magnitude);
                }
            }
            else
            {
                if (animator != null)
                {
                    // Setting Speed to 0 triggers idle animation
                    // This is also automatically synced
                    animator.SetFloat("Speed", 0f);
                }
            }
        }

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;

        if (controller != null)
        {
            controller.Move(velocity * Time.deltaTime);
        }

        // ========================================
        // SWORD HITBOX (Manual Sync Required)
        // This needs manual syncing because it's a gameplay event,
        // not just a visual animation
        // ========================================
        HandleSwordHitbox();
    }

    private void HandleTauntInput()
    {
        // Performance optimization: only check if any key is pressed
        if (!Input.anyKeyDown) return;

        // Check cooldown
        if (Time.time - lastTauntTime < tauntCooldown) return;

        for (int i = 0; i < Mathf.Min(Taunts.Length, 9); i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                if (!string.IsNullOrEmpty(Taunts[i]))
                {
                    // Trigger is automatically synced by NetworkAnimator
                    // All clients will see this taunt animation
                    animator.SetTrigger(Taunts[i]);
                    lastTauntTime = Time.time;
                    Debug.Log($"🎭 Taunt triggered: {Taunts[i]}");
                    return;
                }
            }
        }
    }

    private void HandleSwordHitbox()
    {
        // This logic runs on ALL clients (owner and remote)
        // because NetworkAnimator syncs the animation state
        if (animator == null || swordHitbox == null) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // Check if swing animation is playing
        if (stateInfo.IsName("Armature|Swing") &&
            stateInfo.normalizedTime >= 0.1f &&
            stateInfo.normalizedTime <= 0.9f)
        {
            // Only enable hitbox on the OWNER
            // Remote players just see the animation
            if (IsOwner && !swordHitbox.IsHitboxActive)
            {
                swordHitbox.EnableHitbox();
                Debug.Log("⚔️ Sword hitbox ENABLED (Owner only)");
            }
        }
        else
        {
            // Disable hitbox when swing animation ends
            if (IsOwner && swordHitbox.IsHitboxActive)
            {
                swordHitbox.DisableHitbox();
                Debug.Log("⚔️ Sword hitbox DISABLED");
            }
        }
    }
}











