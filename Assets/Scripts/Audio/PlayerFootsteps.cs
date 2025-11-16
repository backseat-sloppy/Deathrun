using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Handles footstep audio for the player character.
/// Attach this to the same GameObject as PlayerAudioEvents.
/// Uses raycasting to detect when feet hit the ground and determines surface type for Wwise.
/// </summary>
[RequireComponent(typeof(PlayerAudioEvents))]
public class PlayerFootsteps : MonoBehaviour
{
    #region Serialized Fields

    [Header("Foot Transforms")]
    [Tooltip("Transform of the left foot bone")]
    [SerializeField] private Transform leftFootTransform;

    [Tooltip("Transform of the right foot bone")]
    [SerializeField] private Transform rightFootTransform;

    [Header("Detection Settings")]
    [Tooltip("Distance to raycast downward from each foot")]
    [SerializeField] private float raycastDistance = 0.5f;

    [Tooltip("Offset above foot transform to start the raycast (useful if foot bone is at ankle)")]
    [SerializeField] private float raycastStartOffset = 0.2f;

    [Tooltip("Layer mask for ground detection")]
    [SerializeField] private LayerMask groundLayerMask = ~0; // Everything by default

    [Tooltip("Minimum vertical velocity to trigger footstep on landing")]
    [SerializeField] private float landingVelocityThreshold = 0.5f;

    [Header("Timing Settings")]
    [Tooltip("Minimum time between footstep sounds (prevents spam)")]
    [SerializeField] private float footstepCooldown = 0.2f;

    [Tooltip("How low the foot needs to be (Y position relative to root) to potentially trigger")]
    [SerializeField] private float footHeightThreshold = 1.0f;

    [Header("Wwise Surface Detection")]
    [Tooltip("Name of the Wwise RTPC for surface type")]
    [SerializeField] private string surfaceRtpcName = "SurfaceType";

    #endregion

    #region Private Variables

    private PlayerAudioEvents playerAudioEvents;
    private CharacterController characterController;
    private Rigidbody rb;
    
    // Tracking foot states
    private bool leftFootWasGrounded;
    private bool rightFootWasGrounded;
    private float lastLeftFootstepTime;
    private float lastRightFootstepTime;
    private float lastLeftFootHeight;
    private float lastRightFootHeight;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        playerAudioEvents = GetComponent<PlayerAudioEvents>();
        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();

        if (playerAudioEvents == null)
        {
            Debug.LogError($"[PlayerFootsteps] No PlayerAudioEvents component found on {gameObject.name}");
            enabled = false;
        }
    }

    private void Update()
    {
        if (leftFootTransform != null)
        {
            CheckFootstep(leftFootTransform, "LeftFoot", ref leftFootWasGrounded, ref lastLeftFootstepTime, ref lastLeftFootHeight);
        }

        if (rightFootTransform != null)
        {
            CheckFootstep(rightFootTransform, "RightFoot", ref rightFootWasGrounded, ref lastRightFootstepTime, ref lastRightFootHeight);
        }
    }

    #endregion

    #region Footstep Detection

    /// <summary>
    /// Checks if a foot should trigger a footstep sound.
    /// </summary>
    private void CheckFootstep(Transform footTransform, string footId, ref bool wasGrounded, ref float lastStepTime, ref float lastFootHeight)
    {
        // Check cooldown
        if (Time.time - lastStepTime < footstepCooldown)
        {
            return;
        }

        // Check if player is moving
        if (!IsPlayerMoving())
        {
            wasGrounded = false;
            return;
        }

        // Get current foot height relative to player root
        float currentFootHeight = footTransform.position.y - transform.position.y;

        // Raycast down from foot to detect ground (start slightly above the foot bone)
        Vector3 rayStart = footTransform.position + Vector3.up * raycastStartOffset;
        RaycastHit hitInfo;
        bool isGroundedNow = Physics.Raycast(
            rayStart,
            Vector3.down,
            out hitInfo,
            raycastDistance + raycastStartOffset,
            groundLayerMask,
            QueryTriggerInteraction.Ignore
        );

        // Check if foot just hit the ground (wasn't grounded before, is now)
        if (isGroundedNow && !wasGrounded)
        {
            // Check if foot was moving down (landing motion)
            float footMovement = lastFootHeight - currentFootHeight;

            // Only require the foot to be moving down, not the height threshold (more reliable)
            if (footMovement > 0.005f || wasGrounded == false)
            {
                // Determine surface type and set Wwise RTPC
                SetSurfaceType(hitInfo);

                // Play footstep
                playerAudioEvents.PlayFootstep(footId);
                lastStepTime = Time.time;
            }
        }

        // Update states
        wasGrounded = isGroundedNow;
        lastFootHeight = currentFootHeight;
    }

    /// <summary>
    /// Determines the surface type from the hit object and sets the Wwise RTPC.
    /// </summary>
    private void SetSurfaceType(RaycastHit hitInfo)
    {
        float surfaceValue = 0f; // Default: Concrete

        // Check if the hit object has a SurfaceType component
        SurfaceType surfaceType = hitInfo.collider.GetComponent<SurfaceType>();
        
        if (surfaceType != null)
        {
            surfaceValue = surfaceType.GetSurfaceValue();
        }
        else
        {
            // Fallback: Try to determine by tag
            switch (hitInfo.collider.tag)
            {
                case "Concrete":
                    surfaceValue = 0f;
                    break;
                case "Dirt":
                    surfaceValue = 1f;
                    break;
                case "Metal":
                    surfaceValue = 2f;
                    break;
                case "Water":
                    surfaceValue = 3f;
                    break;
                case "Wood":
                    surfaceValue = 4f;
                    break;
                default:
                    surfaceValue = 0f; // Default to concrete
                    break;
            }
        }

        // Set the Wwise RTPC on this game object
        AkSoundEngine.SetRTPCValue(surfaceRtpcName, surfaceValue, gameObject);
    }

    /// <summary>
    /// Checks if the player is moving (to avoid footsteps when standing still).
    /// </summary>
    private bool IsPlayerMoving()
    {
        // Check CharacterController velocity
        if (characterController != null)
        {
            return characterController.velocity.sqrMagnitude > 0.1f;
        }

        // Check Rigidbody velocity
        if (rb != null)
        {
            return rb.linearVelocity.sqrMagnitude > 0.1f;
        }

        // If no movement component, always allow footsteps
        return true;
    }

    #endregion

    #region Debug Visualization

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (leftFootTransform != null)
        {
            DrawFootGizmo(leftFootTransform, Color.red);
        }

        if (rightFootTransform != null)
        {
            DrawFootGizmo(rightFootTransform, Color.blue);
        }
    }

    private void DrawFootGizmo(Transform footTransform, Color color)
    {
        Gizmos.color = color;
        Vector3 rayStart = footTransform.position + Vector3.up * raycastStartOffset;
        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * (raycastDistance + raycastStartOffset));
        Gizmos.DrawWireSphere(rayStart + Vector3.down * (raycastDistance + raycastStartOffset), 0.02f);
        Gizmos.DrawWireSphere(rayStart, 0.03f);
    }
#endif

    #endregion
}
