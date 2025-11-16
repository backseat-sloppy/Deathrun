using UnityEngine;

public class WreckingTrap : MonoBehaviour
{
    [Header("Swing Settings")]
    public float swingAngle = 45f;
    public float swingSpeed = 2f;

    [Header("Activation")]
    [SerializeField] private bool startActive = false; // Start swinging immediately?

    private Quaternion initialRotation;
    private bool isActive = false;


    void Start()
    {
        initialRotation = transform.rotation;
        isActive = startActive;
    }

    void Update()
    {
        // Only swing if active
        if (isActive)
        {
            float angle = swingAngle * Mathf.Sin(Time.time * swingSpeed);
            transform.rotation = initialRotation * Quaternion.Euler(0, 0, angle);
        }
    }

    /// <summary>
    /// Activates the wrecking ball swing
    /// </summary>
    public void ActivateTrap()
    {
        isActive = true;
        Debug.Log("🏗️ WreckingTrap activated - swinging started!");
    }
}
