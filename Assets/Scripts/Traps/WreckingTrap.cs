using UnityEngine;

public class WreckingTrap : MonoBehaviour
{
    [Header("Swing Settings")]
    public float swingAngle = 45f;
    public float swingSpeed = 2f;

    [Header("Activation")]
    [SerializeField] private bool startActive = false; // Start swinging immediately?

    private Quaternion centerRotation; // The center point of the swing
    private bool isActive = false;
    private float timeOffset = 0f; // Offset to start from current angle

    void Start()
    {
        // The initial scene rotation IS the peak/end of the swing
        // We need to calculate what the CENTER rotation should be
        
        Quaternion startRotation = transform.rotation;
        Vector3 startEuler = startRotation.eulerAngles;
        
        // Normalize the starting Z angle to -180 to 180
        float startZ = startEuler.z;
        if (startZ > 180f) startZ -= 360f;
        
        // The center rotation is the starting rotation minus the starting angle
        // Example: If object is at 45°, and that's the peak, then center is at 0°
        centerRotation = Quaternion.Euler(
            startEuler.x,
            startEuler.y,
            startEuler.z - startZ
        );
        
        isActive = startActive;
        
        // Calculate initial time offset based on starting angle
        if (startActive)
        {
            CalculateTimeOffset();
        }
    }

    void Update()
    {
        // Only swing if active
        if (isActive)
        {
            // Use cosine to start at peak (1.0) instead of sine which starts at middle (0.0)
            // Cosine starts at maximum value, perfect for starting at the peak
            float angle = swingAngle * Mathf.Cos((Time.time - timeOffset) * swingSpeed);
            transform.rotation = centerRotation * Quaternion.Euler(0, 0, angle);
        }
    }

    /// <summary>
    /// Activates the wrecking ball swing
    /// </summary>
    public void ActivateTrap()
    {
        if (!isActive)
        {
            // Calculate time offset to start from current angle
            CalculateTimeOffset();
            isActive = true;
            Debug.Log("🏗️ WreckingTrap activated - swinging started!");
        }
    }
    
    /// <summary>
    /// Deactivates the wrecking ball swing
    /// </summary>
    public void DeactivateTrap()
    {
        isActive = false;
        Debug.Log("🏗️ WreckingTrap deactivated!");
    }
    
    /// <summary>
    /// Calculates the time offset to make the swing start from the current angle
    /// </summary>
    private void CalculateTimeOffset()
    {
        // Get the current angle relative to center rotation
        Quaternion deltaRotation = Quaternion.Inverse(centerRotation) * transform.rotation;
        float currentAngle = deltaRotation.eulerAngles.z;
        
        // Normalize angle to -180 to 180 range
        if (currentAngle > 180f)
        {
            currentAngle -= 360f;
        }
        
        // Calculate what time value would produce this angle using COSINE
        // angle = swingAngle * Cos(time * swingSpeed)
        // Cos(time * swingSpeed) = currentAngle / swingAngle
        // time * swingSpeed = Acos(currentAngle / swingAngle)
        
        float normalizedAngle = Mathf.Clamp(currentAngle / swingAngle, -1f, 1f);
        float timeValue = Mathf.Acos(normalizedAngle) / swingSpeed;
        
        // Set offset so that (Time.time - offset) produces the correct starting angle
        timeOffset = Time.time - timeValue;
    }
}
