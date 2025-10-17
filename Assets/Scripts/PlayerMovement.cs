using UnityEngine;
using Unity.Netcode;
using NUnit.Framework;
using System.Collections.Generic;

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


    [SerializeField] private string[] Taunts;

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

        if (Input.GetMouseButtonDown(0))
        {
            if (animator != null)
            {
                animator.SetTrigger("Swing");
            }
        }

        int i = 0;
            while (i < Taunts.Length && i < 9)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                if (animator != null)
                {
                    animator.SetTrigger(Taunts[i]);
                }
                else 
                {
                    Debug.LogWarning("Animator not found");
                }
            }
            i++;
        }
        


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

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        
        if (controller != null)
        {
            controller.Move(velocity * Time.deltaTime);
        }

        // Sword hitbox with null checks
        if (animator != null && swordHitbox != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.IsName("Armature|Swing") && stateInfo.normalizedTime >= 0.1f && stateInfo.normalizedTime <= 0.9f)
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
}











