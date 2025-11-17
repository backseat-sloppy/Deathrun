using UnityEngine;
using Oculus.Interaction;

/// <summary>
/// Simple script that activates a trap when a VR object is grabbed.
/// Attach this to any GameObject with a Grabbable component.
/// </summary>
[RequireComponent(typeof(Grabbable))]
public class VRTrapActivator : MonoBehaviour
{
    [Header("Trap Settings")]
    [Tooltip("The trap GameObject to activate")]
    [SerializeField] private GameObject trapObject;

    [Tooltip("Name of the method to call on the trap (e.g., 'ActivateTrap')")]
    [SerializeField] private string activationMethodName = "ActivateTrap";

    [Header("Trigger Options")]
    [Tooltip("Activate when grabbed?")]
    [SerializeField] private bool activateOnGrab = true;

    [Tooltip("Activate when released?")]
    [SerializeField] private bool activateOnRelease = false;

    [Header("Visual Feedback")]
    [Tooltip("Change color to red when activated?")]
    [SerializeField] private bool changeColorOnActivation = true;

    [Tooltip("Color to change to when activated")]
    [SerializeField] private Color activatedColor = Color.red;

    [Header("Movement Lock")]
    [Tooltip("Prevent object from being moved after activation?")]
    [SerializeField] private bool lockMovementOnActivation = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    [Tooltip("Press this button in Inspector to test trap activation")]
    [SerializeField] private bool debugActivateTrap = false;

    private Grabbable grabbable;
    private Rigidbody rb;
    private bool hasBeenActivated = false;
    private Renderer objectRenderer;
    private Material originalMaterial;
    private Material activatedMaterial;

    private void Awake()
    {
        // Get the Grabbable component
        grabbable = GetComponent<Grabbable>();

        // Get Rigidbody (if exists) for locking movement
        rb = GetComponent<Rigidbody>();

        // Get renderer for color change
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer == null)
        {
            objectRenderer = GetComponentInChildren<Renderer>();
        }

        // Store original material
        if (objectRenderer != null && changeColorOnActivation)
        {
            originalMaterial = objectRenderer.material;
            
            // Create a copy of the material for activation state
            activatedMaterial = new Material(originalMaterial);
            
            // Set the activated color
            if (activatedMaterial.HasProperty("_Color"))
            {
                activatedMaterial.color = activatedColor;
            }
            else if (activatedMaterial.HasProperty("_BaseColor"))
            {
                activatedMaterial.SetColor("_BaseColor", activatedColor);
            }
        }
    }

    private void OnEnable()
    {
        // Subscribe to grab events
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised += OnGrabEvent;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised -= OnGrabEvent;
        }
    }

    private void OnValidate()
    {
        // Debug button trigger in Inspector
        if (debugActivateTrap)
        {
            debugActivateTrap = false;
            
            // Only activate in Play mode
            if (Application.isPlaying)
            {
                ActivateTrap();
            }
            else
            {
                Debug.LogWarning("[VRTrapActivator] Debug activation only works in Play mode!");
            }
        }
    }

    /// <summary>
    /// Called when grab/release happens
    /// </summary>
    private void OnGrabEvent(PointerEvent evt)
    {
        // Prevent interaction if already activated and locked
        if (hasBeenActivated && lockMovementOnActivation)
        {
            Log("🔒 Object is locked - cannot be grabbed again!");
            return;
        }

        // Check if object was grabbed
        if (evt.Type == PointerEventType.Select && activateOnGrab)
        {
            Log("🖐️ Object grabbed!");
            ActivateTrap();
        }

        // Check if object was released
        if (evt.Type == PointerEventType.Unselect && activateOnRelease)
        {
            Log("👋 Object released!");
            ActivateTrap();
        }
    }

    /// <summary>
    /// Activates the trap
    /// </summary>
    private void ActivateTrap()
    {
        if (hasBeenActivated)
        {
            Log("⚠️ Trap already activated!");
            return;
        }

        if (trapObject == null)
        {
            Debug.LogError("[VRTrapActivator] No trap object assigned!");
            return;
        }

        // Mark as activated
        hasBeenActivated = true;

        // Call the activation method on the trap
        trapObject.SendMessage(activationMethodName, SendMessageOptions.DontRequireReceiver);

        // Lock movement
        if (lockMovementOnActivation)
        {
            LockMovement();
        }

        // Change color
        if (changeColorOnActivation && objectRenderer != null && activatedMaterial != null)
        {
            objectRenderer.material = activatedMaterial;
            Log("🎨 Changed color to red!");
        }

        Log($"✅ Activated trap: {trapObject.name}");
    }

    /// <summary>
    /// Locks the object in place
    /// </summary>
    private void LockMovement()
    {
        // Disable the grabbable component
        if (grabbable != null)
        {
            grabbable.enabled = false;
            Log("🔒 Grabbable disabled - object locked!");
        }

        // Lock the Rigidbody
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Log("🔒 Rigidbody locked!");
        }
    }
    
    /// <summary>
    /// Public method to activate trap from other scripts or Unity Events
    /// </summary>
    public void ManualActivate()
    {
        Log("🔧 Manual activation triggered!");
        ActivateTrap();
    }

    /// <summary>
    /// Reset the activator (for testing)
    /// </summary>
    public void ResetActivator()
    {
        hasBeenActivated = false;

        // Re-enable grabbable
        if (grabbable != null)
        {
            grabbable.enabled = true;
        }

        // Unlock rigidbody
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        // Restore original color
        if (objectRenderer != null && originalMaterial != null)
        {
            objectRenderer.material = originalMaterial;
        }

        Log("🔄 Activator reset!");
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[VRTrapActivator] {message}");
        }
    }

    private void OnDestroy()
    {
        // Clean up materials to prevent memory leaks
        if (activatedMaterial != null)
        {
            Destroy(activatedMaterial);
        }
    }
}
