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

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private Grabbable grabbable;

    private void Awake()
    {
        // Get the Grabbable component
        grabbable = GetComponent<Grabbable>();
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

    /// <summary>
    /// Called when grab/release happens
    /// </summary>
    private void OnGrabEvent(PointerEvent evt)
    {
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
        if (trapObject == null)
        {
            Debug.LogError("[VRTrapActivator] No trap object assigned!");
            return;
        }

        // Call the activation method on the trap
        trapObject.SendMessage(activationMethodName, SendMessageOptions.DontRequireReceiver);

        Log($"✅ Activated trap: {trapObject.name}");
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[VRTrapActivator] {message}");
        }
    }
}
