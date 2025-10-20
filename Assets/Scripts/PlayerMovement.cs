using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    public CharacterController controller;
    public Animator animator;
    public float speed = 6f;
    public float gravity = -9.81f;
    public float jumpForce = 5f;

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

        // IMPROVED: Raycast ground detection (more efficient than CheckSphere)
        isGrounded = Physics.Raycast(groundCheck.position, Vector3.down, groundDistance, groundMask);

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
                animator.SetTrigger("Swing");
                Debug.Log("⚔️ Swing animation triggered");
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
                    animator.SetFloat("Speed", move.magnitude);
                }
            }
            else
            {
                if (animator != null)
                {
                    animator.SetFloat("Speed", 0f);
                }
            }
        }

        // ========================================
        // JUMPING SYSTEM (Trigger - Auto Synced by NetworkAnimator)
        // ========================================
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            velocity.y = jumpForce;
            
            if (animator != null)
            {
                animator.SetTrigger("Jump");
                Debug.Log("🦘 Jump animation triggered");
            }
        }

        velocity.y += gravity * Time.deltaTime;
        
        if (controller != null)
        {
            controller.Move(velocity * Time.deltaTime);
        }

        HandleSwordHitbox();
    }

    private void HandleTauntInput()
    {
        if (!Input.anyKeyDown) return;
        if (Time.time - lastTauntTime < tauntCooldown) return;

        for (int i = 0; i < Mathf.Min(Taunts.Length, 9); i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                if (!string.IsNullOrEmpty(Taunts[i]))
                {
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
        if (animator == null || swordHitbox == null) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName("Armature|Swing") &&
            stateInfo.normalizedTime >= 0.1f &&
            stateInfo.normalizedTime <= 0.9f)
        {
            if (IsOwner && !swordHitbox.IsHitboxActive)
            {
                swordHitbox.EnableHitbox();
                Debug.Log("⚔️ Sword hitbox ENABLED (Owner only)");
            }
        }
        else
        {
            if (IsOwner && swordHitbox.IsHitboxActive)
            {
                swordHitbox.DisableHitbox();
                Debug.Log("⚔️ Sword hitbox DISABLED");
            }
        }
    }

    // Optional: Visualize raycast in Scene view
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawRay(groundCheck.position, Vector3.down * groundDistance);
        }
    }
}











