using UnityEngine;

/// <summary>
/// Attach this to foot GameObjects (with colliders) to detect ground contact.
/// When the foot touches the ground, it triggers a footstep sound via PlayerAudioEvents.
/// This script should be placed on both left and right foot bones/objects.
/// </summary>
[RequireComponent(typeof(Collider))]
public class FootstepTrigger : MonoBehaviour
{
    #region Serialized Fields

    [Header("Footstep Settings")]
    [Tooltip("Identifier for this foot (e.g., 'LeftFoot' or 'RightFoot')")]
    [SerializeField] private string footId = "LeftFoot";

    [Tooltip("Tag that ground objects should have to trigger footsteps. Leave empty to trigger on all collisions.")]
    [SerializeField] private string groundTag = "Ground";

    [Tooltip("Minimum time between footstep sounds (in seconds) to prevent spam")]
    [SerializeField] private float footstepCooldown = 0.2f;

    #endregion

    #region Private Variables

    private PlayerAudioEvents playerAudioEvents;
    private float lastFootstepTime;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Find the PlayerAudioEvents component in parent hierarchy
        playerAudioEvents = GetComponentInParent<PlayerAudioEvents>();

        if (playerAudioEvents == null)
        {
            Debug.LogWarning($"[FootstepTrigger] No PlayerAudioEvents found in parent hierarchy for {gameObject.name}. Footstep sounds will not play.");
        }

        // Ensure the collider is set to trigger mode
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"[FootstepTrigger] Collider on {gameObject.name} is not set as trigger. Consider setting isTrigger = true for better performance.");
        }
    }

    #endregion

    #region Collision Detection

    /// <summary>
    /// Called when this foot's trigger collider enters another collider.
    /// Use this if your foot collider is set as a trigger.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        HandleFootContact(other.gameObject);
    }

    /// <summary>
    /// Called when this foot's collider collides with another collider.
    /// Use this if your foot collider is NOT set as a trigger (physical collision).
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        HandleFootContact(collision.gameObject);
    }

    #endregion

    #region Footstep Logic

    /// <summary>
    /// Handles the logic when the foot makes contact with a surface.
    /// Checks tags, cooldown, and triggers the footstep sound.
    /// </summary>
    private void HandleFootContact(GameObject contactedObject)
    {
        // Check if we should play a footstep sound
        if (!ShouldPlayFootstep(contactedObject))
        {
            return;
        }

        // Check cooldown to prevent too many footsteps in quick succession
        if (Time.time - lastFootstepTime < footstepCooldown)
        {
            return;
        }

        // Play the footstep sound via PlayerAudioEvents
        if (playerAudioEvents != null)
        {
            playerAudioEvents.PlayFootstep(footId);
            lastFootstepTime = Time.time;
        }
    }

    /// <summary>
    /// Determines if a footstep sound should be played based on the contacted object.
    /// </summary>
    private bool ShouldPlayFootstep(GameObject contactedObject)
    {
        // If no ground tag is specified, trigger on any contact
        if (string.IsNullOrEmpty(groundTag))
        {
            return true;
        }

        // Check if the contacted object has the correct ground tag
        return contactedObject.CompareTag(groundTag);
    }

    #endregion

    #region Editor Helpers

#if UNITY_EDITOR
    /// <summary>
    /// Draws a gizmo to visualize which GameObject has the FootstepTrigger.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.05f);
    }
#endif

    #endregion
}
