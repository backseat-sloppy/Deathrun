using UnityEngine;
using System.Collections;

public class FallingStone : MonoBehaviour
{
    [Header("Stone Movement Settings")]
    [SerializeField] private Transform stoneTransform;
    [SerializeField] private float fallDistance = 5f; // How far the stone falls
    [SerializeField] private float fallSpeed = 10f; // Speed when falling (fast)
    [SerializeField] private float riseSpeed = 2f; // Speed when rising (slower)

    [Header("Activation Settings")]
    [SerializeField] private bool startActive = false; // Start falling immediately?
    [SerializeField] private float groundDuration = 3f; // How long stone stays on ground
    [SerializeField] private bool oneTimeUse = true; // Can only be triggered once

    [Header("Physics Settings")]
    [SerializeField] private bool useGravity = true; // Use realistic gravity fall?
    [SerializeField] private float gravityMultiplier = 2f; // Gravity strength multiplier

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool hasBeenUsed = false;
    private Rigidbody rb;

    private void Start()
    {
        if (stoneTransform == null)
        {
            stoneTransform = transform;
        }

        // Check if using rigidbody
        rb = stoneTransform.GetComponent<Rigidbody>();
        if (rb != null && useGravity)
        {
            rb.isKinematic = true; // Start kinematic, enable when falling
        }

        startPosition = stoneTransform.localPosition;
        targetPosition = startPosition + Vector3.down * fallDistance;

        if (startActive)
        {
            ActivateTrap();
        }
    }

    /// <summary>
    /// Activates the falling stone trap
    /// </summary>
    public void ActivateTrap()
    {
        if (hasBeenUsed && oneTimeUse)
        {
            Log("🪨 FallingStone already used - cannot trigger again!");
            return;
        }

        hasBeenUsed = true;
        StartCoroutine(ActivationSequence());
        Log("🪨 FallingStone activated - stone falling!");
    }

    /// <summary>
    /// Deactivates the falling stone trap
    /// </summary>
    public void DeactivateTrap()
    {
        StopAllCoroutines();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
        }
        StartCoroutine(MoveStoneSmoothly(startPosition, riseSpeed));
        Log("🪨 FallingStone deactivated - stone rising!");
    }

    private IEnumerator ActivationSequence()
    {
        // Fall stone (fast)
        if (useGravity && rb != null)
        {
            yield return StartCoroutine(FallWithGravity());
        }
        else
        {
            yield return StartCoroutine(MoveStoneSmoothly(targetPosition, fallSpeed));
        }

        Log("🪨 FallingStone hit ground - staying down!");

        // Wait on ground
        yield return new WaitForSeconds(groundDuration);

        // Rise stone back up (slower)
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
        }
        yield return StartCoroutine(MoveStoneSmoothly(startPosition, riseSpeed));

        Log("🪨 FallingStone finished - stone returned to start!");
    }

    private IEnumerator FallWithGravity()
    {
        // Enable physics
        rb.isKinematic = false;
        rb.useGravity = true;

        // Apply extra downward force for dramatic fall
        rb.AddForce(Vector3.down * gravityMultiplier, ForceMode.VelocityChange);

        // Wait until stone reaches target height
        Vector3 targetWorldPos = stoneTransform.parent 
            ? stoneTransform.parent.TransformPoint(targetPosition) 
            : targetPosition;

        while (stoneTransform.position.y > targetWorldPos.y + 0.1f)
        {
            yield return null;
        }

        // Stop physics
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        stoneTransform.localPosition = targetPosition;
    }

    private IEnumerator MoveStoneSmoothly(Vector3 target, float speed)
    {
        while (Vector3.Distance(stoneTransform.localPosition, target) > 0.01f)
        {
            stoneTransform.localPosition = Vector3.MoveTowards(
                stoneTransform.localPosition,
                target,
                speed * Time.deltaTime
            );
            yield return null; // Wait one frame
        }

        // Snap to final position
        stoneTransform.localPosition = target;
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[FallingStone] {message}");
        }
    }

    // Visualize the fall path in the editor
    private void OnDrawGizmosSelected()
    {
        if (stoneTransform == null)
            stoneTransform = transform;

        Vector3 start = Application.isPlaying ? startPosition : stoneTransform.localPosition;
        Vector3 end = start + Vector3.down * fallDistance;

        // Draw the fall path
        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            stoneTransform.parent ? stoneTransform.parent.TransformPoint(start) : start,
            stoneTransform.parent ? stoneTransform.parent.TransformPoint(end) : end
        );

        // Draw the ground position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            stoneTransform.parent ? stoneTransform.parent.TransformPoint(end) : end, 
            stoneTransform.localScale
        );
    }
}
