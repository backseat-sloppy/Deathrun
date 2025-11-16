using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

/// <summary>
/// Ultra-simple AR placement for Quest 2.
/// Point anywhere and press trigger to place. No surface detection needed.
/// </summary>
public class ARQuest2 : MonoBehaviour
{
    [Header("Prefab Settings")]
    [Tooltip("The prefab to place in AR")]
    [SerializeField] private GameObject prefabToPlace;
    
    [Tooltip("Scale of the placed prefab")]
    [SerializeField] private Vector3 prefabScale = new Vector3(0.1f, 0.1f, 0.1f);
    
    [Header("Placement Settings")]
    [Tooltip("Distance from controller to place the prefab")]
    [SerializeField] private float placementDistance = 2f;
    
    [Header("Input")]
    [Tooltip("Use right controller (uncheck for left)")]
    [SerializeField] private bool useRightController = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    // Internal state
    private GameObject placedObject;
    private GameObject previewObject;
    private bool hasPlaced = false;
    
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
        
        // Create preview
        CreatePreview();
        
        Log("✅ Ready! Point and press trigger to place.");
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
        
        // Calculate placement position (always in front of controller)
        Vector3 placePosition = controllerPosition + (controllerRotation * Vector3.forward) * placementDistance;
        
        // Update preview position
        if (previewObject != null)
        {
            previewObject.SetActive(true);
            previewObject.transform.position = placePosition;
            previewObject.transform.rotation = Quaternion.identity; // Always upright
        }
        
        // Check for trigger press (detect button down, not hold)
        if (triggerPressed && !triggerWasPressed)
        {
            PlacePrefab(placePosition);
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
    /// Places the prefab
    /// </summary>
    private void PlacePrefab(Vector3 position)
    {
        if (hasPlaced) return;
        
        // Create the object
        placedObject = Instantiate(prefabToPlace, position, Quaternion.identity);
        placedObject.transform.localScale = prefabScale;
        
        hasPlaced = true;
        
        Log($"✅ Placed at {position}");
        
        // Hide preview
        if (previewObject != null)
        {
            previewObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Creates preview object
    /// </summary>
    private void CreatePreview()
    {
        previewObject = Instantiate(prefabToPlace);
        previewObject.name = "PREVIEW";
        previewObject.transform.localScale = prefabScale;
        
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
        
        hasPlaced = false;
        Log("🔄 Reset - point and press trigger to place");
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
                "Point and PRESS TRIGGER to place", 
                style
            );
        }
    }
}
