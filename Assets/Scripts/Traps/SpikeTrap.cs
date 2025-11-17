using UnityEngine;
using System.Collections;

public class SpikeTrap : MonoBehaviour
{
    [Header("Spike Movement Settings")]
    [SerializeField] private Transform spikesTransform;
    [SerializeField] private float moveDistance = 0.4f;
    [SerializeField] private float moveSpeed = 2f;

    [Header("Activation Settings")]
    [SerializeField] private bool startActive = false; // Start activated immediately?
    [SerializeField] private float activeDuration = 3f; // How long spikes stay up
    [SerializeField] private bool oneTimeUse = true; // Can only be triggered once

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool hasBeenUsed = false;

    private void Start()
    {
        if (spikesTransform == null)
        {
            spikesTransform = transform;
        }

        startPosition = spikesTransform.localPosition;
        targetPosition = startPosition + Vector3.up * moveDistance;

        if (startActive)
        {
            ActivateTrap();
        }
    }

    /// <summary>
    /// Activates the spike trap
    /// </summary>
    public void ActivateTrap()
    {
        if (hasBeenUsed && oneTimeUse)
        {
            Debug.Log("🔺 SpikeTrap already used - cannot trigger again!");
            return;
        }

        hasBeenUsed = true;
        StartCoroutine(ActivationSequence());
        Debug.Log("🔺 SpikeTrap activated - spikes rising for " + activeDuration + " seconds!");
    }

    /// <summary>
    /// Deactivates the spike trap
    /// </summary>
    public void DeactivateTrap()
    {
        StopAllCoroutines();
        StartCoroutine(MoveSpikesSmoothly(startPosition));
        Debug.Log("🔺 SpikeTrap deactivated - spikes retracting!");
    }

    private IEnumerator ActivationSequence()
    {
        // Raise spikes smoothly
        yield return StartCoroutine(MoveSpikesSmoothly(targetPosition));

        // Wait for duration
        yield return new WaitForSeconds(activeDuration);

        // Lower spikes smoothly
        yield return StartCoroutine(MoveSpikesSmoothly(startPosition));

        Debug.Log("🔺 SpikeTrap finished - spikes retracted!");
    }

    private IEnumerator MoveSpikesSmoothly(Vector3 target)
    {
        while (Vector3.Distance(spikesTransform.localPosition, target) > 0.01f)
        {
            spikesTransform.localPosition = Vector3.MoveTowards(
                spikesTransform.localPosition,
                target,
                moveSpeed * Time.deltaTime
            );
            yield return null; // Wait one frame
        }

        // Snap to final position
        spikesTransform.localPosition = target;
    }
}
