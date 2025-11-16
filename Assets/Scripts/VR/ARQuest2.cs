using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

/// <summary>
/// Ultra-simple AR placement for Quest 2.
/// Spawns prefab directly at right hand position on trigger press.
/// </summary>
public class ARQuest2 : MonoBehaviour
{
    [Header("Prefab Settings")]
    [Tooltip("The prefab to place in AR")]
    [SerializeField] private GameObject prefabToPlace;
    
    [Tooltip("Scale multiplier for the prefab (1 = original size)")]
    [SerializeField] private float prefabScaleMultiplier = 1f;
    
    [Header("Player Scale Settings")]
    [Tooltip("Scale the player/room to be this many times larger than normal")]
    [SerializeField] private float playerScaleMultiplier = 10f;
    
    [Tooltip("Reference to XR Origin (auto-found if null)")]
    [SerializeField] private Transform xrOrigin;
    
    [Tooltip("Reference to TrackingSpace (auto-found if null)")]
    [SerializeField] private Transform trackingSpace;
    
    [Tooltip("Apply player scale immediately on Start (vs on placement)")]
    [SerializeField] private bool scalePlayerOnStart = true;
    
    [Header("Spawn Settings")]
    [Tooltip("Spawn offset from controller (local space)")]
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;
    
    [Header("Input")]
    [Tooltip("Use right controller (uncheck for left)")]
    [SerializeField] private bool useRightController = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    // Internal state
    private GameObject placedObject;
    private GameObject previewObject;
    private bool hasPlaced = false;
    private Vector3 originalTrackingSpaceScale;
    
    // Controller tracking
    private Vector3 controllerPosition;
    private Quaternion controllerRotation;
    private bool triggerPressed = false;
    private bool triggerWasPressed = false;
    
    private void Start()
    {
        Log("🎮 Quest 2 AR Placement initialized");
        
        // Validate prefab
        if (prefabToPlace == null)
        {
            Debug.LogError("[ARQuest2] No prefab assigned!");
            enabled = false;
            return;
        }
        
        // Find XR Origin if not assigned
        if (xrOrigin == null)
        {
            // Try common names
            GameObject xrRig = GameObject.Find("XR Origin") ?? GameObject.Find("XR Rig") ?? GameObject.Find("OVRCameraRig") ?? GameObject.Find("AR Session Origin");
            if (xrRig != null)
            {
                xrOrigin = xrRig.transform;
                Log($"Found XR Origin: {xrRig.name}");
            }
            else
            {
                // Last resort: find object with this script's parent
                xrOrigin = transform.parent;
                if (xrOrigin != null)
                {
                    Log($"Using parent as XR Origin: {xrOrigin.name}");
                }
                else
                {
                    Debug.LogWarning("[ARQuest2] XR Origin not found. Player scale will not be adjusted.");
                }
            }
        }
        
        // Find TrackingSpace under XR Origin
        if (trackingSpace == null && xrOrigin != null)
        {
            // Look for TrackingSpace child (case-insensitive search)
            foreach (Transform child in xrOrigin)
            {
                if (child.name.ToLower().Contains("tracking"))
                {
                    trackingSpace = child;
                    Log($"Found TrackingSpace: {child.name}");
                    break;
                }
            }
            
            // If not found, warn user
            if (trackingSpace == null)
            {
                Debug.LogWarning("[ARQuest2] TrackingSpace not found under XR Origin! Please assign it manually in the Inspector.");
                Debug.LogWarning("[ARQuest2] Expected hierarchy: XR Origin > TrackingSpace");
            }
        }
        
        // Store original scale
        if (trackingSpace != null)
        {
            originalTrackingSpaceScale = trackingSpace.localScale;
            
            // Apply player scale immediately if requested
            if (scalePlayerOnStart)
            {
                ScalePlayer();
            }
        }
        else
        {
            Debug.LogError("[ARQuest2] TrackingSpace is required for room-scale adjustment!");
        }
        
        // Create preview
        CreatePreview();
        
        Log($"✅ Ready! Press trigger to spawn at hand position. Player scale: {playerScaleMultiplier}x, Prefab scale: {prefabScaleMultiplier}x");
    }
    
    private void Update()
    {
        // Update controller
        UpdateController();
        
        // If already placed, hide preview and stop
        if (hasPlaced)
        {
            if (previewObject != null)
            {
                previewObject.SetActive(false);
            }
            return;
        }
        
        // Calculate spawn position (directly at controller with optional offset)
        Vector3 spawnPosition = controllerPosition + (controllerRotation * spawnOffset);
        
        // Update preview position to show where it will spawn
        if (previewObject != null)
        {
            previewObject.SetActive(true);
            previewObject.transform.position = spawnPosition;
            previewObject.transform.rotation = Quaternion.identity; // Always upright
        }
        
        // Check for trigger press (detect button down, not hold)
        if (triggerPressed && !triggerWasPressed)
        {
            SpawnPrefab(spawnPosition);
        }
        
        triggerWasPressed = triggerPressed;
    }
    
    /// <summary>
    /// Updates controller position and rotation
    /// </summary>
    private void UpdateController()
    {
        var devices = new List<InputDevice>();
        InputDeviceCharacteristics hand = useRightController 
            ? InputDeviceCharacteristics.Right 
            : InputDeviceCharacteristics.Left;
        
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Controller | hand, 
            devices
        );
        
        if (devices.Count > 0)
        {
            // Get controller position
            if (devices[0].TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
            {
                controllerPosition = pos;
            }
            
            // Get controller rotation
            if (devices[0].TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
            {
                controllerRotation = rot;
            }
            
            // Get trigger state
            devices[0].TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed);
        }
        else
        {
            // Fallback to camera
            Camera cam = Camera.main;
            if (cam != null)
            {
                controllerPosition = cam.transform.position;
                controllerRotation = cam.transform.rotation;
                triggerPressed = Input.GetKey(KeyCode.Space); // Spacebar for testing
            }
        }
    }
    
    /// <summary>
    /// Spawns the prefab directly at controller position
    /// </summary>
    private void SpawnPrefab(Vector3 position)
    {
        if (hasPlaced) return;
        
        // Create the object at 1:1 scale directly at hand position
        placedObject = Instantiate(prefabToPlace, position, Quaternion.identity);
        placedObject.transform.localScale = Vector3.one * prefabScaleMultiplier;
        
        hasPlaced = true;
        
        // Scale player if not already done
        if (!scalePlayerOnStart && trackingSpace != null)
        {
            ScalePlayer();
        }
        
        Log($"✅ Spawned at hand position {position} with scale {prefabScaleMultiplier}x");
        
        // Hide preview
        if (previewObject != null)
        {
            previewObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Scales the TrackingSpace to make player larger AND scale room-scale tracking
    /// </summary>
    private void ScalePlayer()
    {
        if (trackingSpace == null)
        {
            Debug.LogError("[ARQuest2] Cannot scale player - TrackingSpace is null!");
            return;
        }
        
        // Scale the TrackingSpace to affect both visual scale AND physical tracking
        trackingSpace.localScale = originalTrackingSpaceScale * playerScaleMultiplier;
        
        Log($"🔍 Scaled TrackingSpace to {playerScaleMultiplier}x (Scale: {trackingSpace.localScale})");
        Log($"   Your physical movements are now {playerScaleMultiplier}x larger in the virtual world!");
    }
    
    /// <summary>
    /// Creates preview object at 1:1 scale
    /// </summary>
    private void CreatePreview()
    {
        previewObject = Instantiate(prefabToPlace);
        previewObject.name = "PREVIEW";
        previewObject.transform.localScale = Vector3.one * prefabScaleMultiplier;
        
        // Make semi-transparent
        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            Material[] mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material mat = new Material(mats[i]);
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = 0.5f;
                    mat.color = c;
                }
                mat.renderQueue = 3000;
                mats[i] = mat;
            }
            r.materials = mats;
        }
        
        // Disable colliders
        Collider[] colliders = previewObject.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        
        // Disable scripts
        MonoBehaviour[] scripts = previewObject.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            script.enabled = false;
        }
        
        previewObject.SetActive(false);
    }
    
    /// <summary>
    /// Reset placement for testing
    /// </summary>
    public void ResetPlacement()
    {
        if (placedObject != null)
        {
            Destroy(placedObject);
        }
        
        // Reset TrackingSpace scale
        if (trackingSpace != null)
        {
            trackingSpace.localScale = originalTrackingSpaceScale;
        }
        
        hasPlaced = false;
        
        // Re-apply player scale if needed
        if (scalePlayerOnStart && trackingSpace != null)
        {
            ScalePlayer();
        }
        
        Log("🔄 Reset - press trigger to spawn at hand");
    }
    
    private void Log(string msg)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[ARQuest2] {msg}");
        }
    }
    
    private void OnDestroy()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
        }
        
        // Restore original TrackingSpace scale
        if (trackingSpace != null)
        {
            trackingSpace.localScale = originalTrackingSpaceScale;
        }
    }
    
    // Simple on-screen feedback
    private void OnGUI()
    {
        if (!hasPlaced && showDebugLogs)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 30;
            style.normal.textColor = Color.green;
            
            GUI.Label(
                new Rect(Screen.width / 2 - 200, 50, 400, 50), 
                "Press TRIGGER to spawn at hand", 
                style
            );
        }
    }
}
