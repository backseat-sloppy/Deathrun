using UnityEngine;

public class WreckingTrap : MonoBehaviour
{
    [Header("Swing Settings")]
    public float swingAngle = 45f;
    public float swingSpeed = 2f;

    [Header("Activation")]
    [SerializeField] private bool startActive = false; // Start swinging immediately?

    private Quaternion startRotation; // The initial rotation from the scene
    private bool isActive = false;

    void Start()
    {
        // Store the initial rotation as the starting point
        startRotation = transform.rotation;
        
        isActive = startActive;
    }

    void Update()
    {
        // Only swing if active
        if (isActive)
        {
            // Swing from 0 to swingAngle and back
            // Using (1 - Cos) / 2 maps cosine wave from [0 to swingAngle] instead of [-swingAngle to +swingAngle]
            // This makes it swing from startRotation to startRotation + swingAngle
            float angle = swingAngle * (1f - Mathf.Cos(Time.time * swingSpeed)) * 0.5f;
            transform.rotation = startRotation * Quaternion.Euler(0, 0, angle);
        }
    }

    /// <summary>
    /// Activates the wrecking ball swing
    /// </summary>
    public void ActivateTrap()
    {
        if (!isActive)
        {
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
}
