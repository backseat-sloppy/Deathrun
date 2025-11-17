using UnityEngine;
using System.Collections;

public class WallOfDeath : MonoBehaviour
{
    [Header("Wall Movement Settings")]
    [SerializeField] private Transform wallTransform;
    [SerializeField] private Vector3 extendDirection = Vector3.forward; // Direction to extend
    [SerializeField] private float extendDistance = 5f; // How far to extend
    [SerializeField] private float extendSpeed = 3f; // Speed of extension/retraction

    [Header("Activation Settings")]
    [SerializeField] private bool startActive = false; // Start extended immediately?
    [SerializeField] private float activeDuration = 3f; // How long wall stays extended
    [SerializeField] private bool oneTimeUse = true; // Can only be triggered once

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool hasBeenUsed = false;

    private void Start()
    {
        if (wallTransform == null)
        {
            wallTransform = transform;
        }

        startPosition = wallTransform.localPosition;
        targetPosition = startPosition + extendDirection.normalized * extendDistance;

        if (startActive)
        {
            ActivateTrap();
        }
    }

    /// <summary>
    /// Activates the wall of death trap
    /// </summary>
    public void ActivateTrap()
    {
        if (hasBeenUsed && oneTimeUse)
        {
            Log("🧱 WallOfDeath already used - cannot trigger again!");
            return;
        }

        hasBeenUsed = true;
        StartCoroutine(ActivationSequence());
        Log("🧱 WallOfDeath activated - extending for " + activeDuration + " seconds!");
    }

    /// <summary>
    /// Deactivates the wall of death trap
    /// </summary>
    public void DeactivateTrap()
    {
        StopAllCoroutines();
        StartCoroutine(MoveWallSmoothly(startPosition));
        Log("🧱 WallOfDeath deactivated - retracting!");
    }

    private IEnumerator ActivationSequence()
    {
        // Extend wall smoothly
        yield return StartCoroutine(MoveWallSmoothly(targetPosition));

        Log("🧱 WallOfDeath fully extended - holding position!");

        // Wait for duration
        yield return new WaitForSeconds(activeDuration);

        // Retract wall smoothly
        yield return StartCoroutine(MoveWallSmoothly(startPosition));

        Log("🧱 WallOfDeath finished - wall retracted!");
    }

    private IEnumerator MoveWallSmoothly(Vector3 target)
    {
        while (Vector3.Distance(wallTransform.localPosition, target) > 0.01f)
        {
            wallTransform.localPosition = Vector3.MoveTowards(
                wallTransform.localPosition,
                target,
                extendSpeed * Time.deltaTime
            );
            yield return null; // Wait one frame
        }

        // Snap to final position
        wallTransform.localPosition = target;
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[WallOfDeath] {message}");
        }
    }

    // Visualize the wall movement in the editor
    private void OnDrawGizmosSelected()
    {
        if (wallTransform == null)
            wallTransform = transform;

        Vector3 start = Application.isPlaying ? startPosition : wallTransform.localPosition;
        Vector3 end = start + extendDirection.normalized * extendDistance;

        // Draw the extend direction
        Gizmos.color = Color.red;
        Gizmos.DrawLine(wallTransform.parent ? wallTransform.parent.TransformPoint(start) : start,
                        wallTransform.parent ? wallTransform.parent.TransformPoint(end) : end);

        // Draw the extended position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(wallTransform.parent ? wallTransform.parent.TransformPoint(end) : end, 
                           wallTransform.localScale);
    }
}
