using System.Collections;
using UnityEngine;

public class SteppingStone : MonoBehaviour
{
    // The final desired Y position (read from its initial position)
    private float targetY; 

    // The starting Y position (submerged in the water)
    private float startY; 
    
    [Header("Movement Settings")]
    [Tooltip("How far below the surface the stone starts/returns to")]
    public float submergedDepth = 0.5f; 

    [Tooltip("The range for rise duration (lower value is faster)")]
    public Vector2 durationRange = new Vector2(1.5f, 3.0f); 

    [Tooltip("Time in seconds the stone remains up before falling.")]
    public float activeTime = 1.0f; 

    private Coroutine movementCoroutine;

    private void Awake()
    {
        // 1. Save the stone's current Y position as its target final position.
        targetY = transform.position.y; 

        // 2. Calculate the submerged starting/resting position.
        startY = targetY - submergedDepth; 

        // 3. Set the stone to its initial submerged position immediately.
        Vector3 startPosition = transform.position;
        startPosition.y = startY;
        transform.position = startPosition;
    }

    /// <summary>
    /// Called when the player steps on the stone.
    /// </summary>
    public void OnPlayerStep()
    {
        // If the player steps on it, we start the timer for it to fall.
        // First, stop any existing fall timer.
        StopCoroutine(nameof(StartFallTimer)); 
        StartCoroutine(nameof(StartFallTimer));
    }

    /// <summary>
    /// Starts the movement of the stone from the water to its target height.
    /// </summary>
    public void ActivateStoneRise()
    {
        // Stop any previous movement (rising or falling)
        if (movementCoroutine != null) StopCoroutine(movementCoroutine); 

        // Stop the fall timer if it was running (e.g., if the button is pressed quickly again)
        StopCoroutine(nameof(StartFallTimer)); 

        movementCoroutine = StartCoroutine(MoveStone(targetY, true));
    }

    /// <summary>
    /// Initiates the stone falling back into the water.
    /// </summary>
    public void FallBackToWater()
    {
        // Stop any current rising movement
        if (movementCoroutine != null) StopCoroutine(movementCoroutine); 
        
        movementCoroutine = StartCoroutine(MoveStone(startY, false));
    }

    private IEnumerator StartFallTimer()
    {
        // Wait for the defined active time
        yield return new WaitForSeconds(activeTime); 
        
        // After the time is up, trigger the fall
        FallBackToWater();
    }

    private IEnumerator MoveStone(float endY, bool isRising)
    {
        // If rising, choose a random duration. If falling, you might want a fixed duration 
        // or a different random range for consistency. Here, we'll use the same range.
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
    
   
    private void OnCollisionEnter(Collision collision)
    {
        // Check if the collision object is your Player (e.g., by Tag or Layer)
        if (collision.gameObject.CompareTag("Player"))
        {
            // Check if the player is landing on top of the stone (optional but recommended)
            if (collision.contacts.Length > 0 && Vector3.Dot(collision.contacts[0].normal, Vector3.up) < -0.9f)
            {
                // Only start the fall timer if the stone is currently above the water
                if (transform.position.y > startY + 0.05f) 
                {
                    OnPlayerStep();
                }
            }
        }
    }
}