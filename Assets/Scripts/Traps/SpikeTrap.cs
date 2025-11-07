using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    [Header("Spike Movement Settings")]
    [SerializeField] private Transform spikesTransform;
    [SerializeField] private float moveDistance = 0.4f;
    [SerializeField] private float moveSpeed = 2f;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool isPlayerInside = false;
    private bool isMovingUp = false;

    private void Start()
    {
        if (spikesTransform == null)
        {
            spikesTransform = transform;
        }

        startPosition = spikesTransform.localPosition;
        targetPosition = startPosition + Vector3.up * moveDistance;
    }

    private void Update()
    {
        if (isPlayerInside)
        {
            MoveSpikes();
        }
        else
        {
            // Return to start position when player exits
            spikesTransform.localPosition = Vector3.MoveTowards(
                spikesTransform.localPosition,
                startPosition,
                moveSpeed * Time.deltaTime
            );
        }
    }

    private void MoveSpikes()
    {
        Vector3 currentTarget = isMovingUp ? targetPosition : startPosition;

        spikesTransform.localPosition = Vector3.MoveTowards(
            spikesTransform.localPosition,
            currentTarget,
            moveSpeed * Time.deltaTime
        );

        // Check if reached target and switch direction
        if (Vector3.Distance(spikesTransform.localPosition, currentTarget) < 0.01f)
        {
            isMovingUp = !isMovingUp;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Spike Trap Activated!");
            isPlayerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Spike Trap Deactivated!");
            isPlayerInside = false;
            isMovingUp = false;
        }
    }
}
