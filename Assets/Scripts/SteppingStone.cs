using System.Collections;
using UnityEngine;

public class SteppingStone : MonoBehaviour
{
    // The final desired Y position (read from its initial position)
    private float targetY; 

    // The submerged Y position (where it rests)
    private float submergedY; 
    
    [Header("Movement Settings")]
    [Tooltip("How far below the surface the stone starts/returns to")]
    public float submergedDepth = 0.5f; 

    [Tooltip("The range for movement duration (lower value is faster)")]
    public Vector2 durationRange = new Vector2(0.8f, 1.5f); // Adjusted duration for faster fall/rise

    private Coroutine movementCoroutine;

    private void Awake()
    {
        // 1. Save the stone's current Y position (where it starts).
        targetY = transform.position.y; 

        // 2. Calculate the submerged position.
        submergedY = targetY - submergedDepth; 
        
        // **IMPORTANT:** The stone remains at targetY (UP) when the game starts.
    }

    /// <summary>
    /// Starts the movement of the stone, moving it DOWN to the water.
    /// This is triggered in sequence by the ButtonActivator.
    /// </summary>
    public void ActivateStoneFall()
    {
        // Stop any previous movement
        if (movementCoroutine != null) StopCoroutine(movementCoroutine); 
        movementCoroutine = StartCoroutine(MoveStone(submergedY));
    }

    /// <summary>
    /// Starts the movement of the stone, moving it UP from the water.
    /// This is triggered by the ButtonActivator for the 5-second reset.
    /// </summary>
    public void ActivateStoneRise()
    {
        // Stop any previous movement
        if (movementCoroutine != null) StopCoroutine(movementCoroutine); 
        movementCoroutine = StartCoroutine(MoveStone(targetY));
    }

    private IEnumerator MoveStone(float endY)
    {
        // Random speed variation still applies
        float duration = Random.Range(durationRange.x, durationRange.y); 
        float elapsedTime = 0f;

        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(startPos.x, endY, startPos.z);

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            // Use an easing function for a smoother look
            float easedT = t * t * (3f - 2f * t); // SmoothStep

            transform.position = Vector3.Lerp(startPos, endPos, easedT); 

            elapsedTime += Time.deltaTime;
            yield return null; 
        }

        transform.position = endPos;
    }
    
    // NOTE: The OnPlayerStep/OnCollisionEnter logic is REMOVED 
    // as the fall is now sequence-based, not player-step-based.
}