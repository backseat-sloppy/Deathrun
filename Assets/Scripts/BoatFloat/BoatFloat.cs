using UnityEngine;

/// <summary>
/// Physics-based water floating system for boats.
/// Keeps the boat upright using buoyancy forces while allowing realistic tilt.
/// Uses multiple floating points for stability.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BoatFloat : MonoBehaviour
{
    [Header("Water Settings")]
    [SerializeField] private float waterLevel = -3.66f;
    [SerializeField] private float waterDensity = 1000f;
    
    [Header("Buoyancy Points")]
    [Tooltip("Transform points where buoyancy forces are applied (place at boat corners)")]
    [SerializeField] private Transform[] floatingPoints;
    
    [Header("Buoyancy Forces")]
    [SerializeField] private float buoyancyForce = 500f;
    [SerializeField] private float underwaterDrag = 3f;
    [SerializeField] private float underwaterAngularDrag = 1f;
    
    [Header("Stabilization")]
    [SerializeField] private float uprightTorque = 50f;
    [SerializeField] private float maxTiltAngle = 30f;
    [SerializeField] private bool preventCapsizing = true;
    
    [Header("Wave Simulation")]
    [SerializeField] private bool enableWaves = true;
    [SerializeField] private float waveHeight = 0.5f;
    [SerializeField] private float waveFrequency = 0.5f;
    [SerializeField] private float waveSpeed = 1f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    
    private Rigidbody rb;
    private float defaultDrag;
    private float defaultAngularDrag;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Store default drag values
        defaultDrag = rb.linearDamping;
        defaultAngularDrag = rb.angularDamping;
        
        // Configure rigidbody for boat physics
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        
        // Auto-generate floating points if none assigned
        if (floatingPoints == null || floatingPoints.Length == 0)
        {
            GenerateFloatingPoints();
        }
    }
    
    private void FixedUpdate()
    {
        ApplyBuoyancy();
        ApplyUprightTorque();
        
        if (preventCapsizing)
        {
            PreventTipping();
        }
    }
    
    private void ApplyBuoyancy()
    {
        int submergedPoints = 0;
        
        foreach (Transform point in floatingPoints)
        {
            if (point == null) continue;
            
            float pointHeight = point.position.y;
            float currentWaterLevel = GetWaterLevelAtPosition(point.position);
            
            // Check if floating point is underwater
            if (pointHeight < currentWaterLevel)
            {
                float submersionDepth = currentWaterLevel - pointHeight;
                
                // Calculate buoyancy force based on depth
                float buoyancy = buoyancyForce * submersionDepth;
                
                // Apply upward force at this point
                Vector3 force = Vector3.up * buoyancy;
                rb.AddForceAtPosition(force, point.position, ForceMode.Force);
                
                submergedPoints++;
            }
        }
        
        // Adjust drag based on submersion
        float submersionRatio = (float)submergedPoints / floatingPoints.Length;
        rb.linearDamping = Mathf.Lerp(defaultDrag, underwaterDrag, submersionRatio);
        rb.angularDamping = Mathf.Lerp(defaultAngularDrag, underwaterAngularDrag, submersionRatio);
    }
    
    private void ApplyUprightTorque()
    {
        // Calculate how tilted the boat is
        Vector3 currentUp = transform.up;
        Vector3 targetUp = Vector3.up;
        
        // Calculate the torque needed to right the boat
        Vector3 torque = Vector3.Cross(currentUp, targetUp) * uprightTorque;
        
        // Apply torque to keep boat upright
        rb.AddTorque(torque, ForceMode.Force);
    }
    
    private void PreventTipping()
    {
        // Get current tilt angles
        float tiltX = Mathf.Abs(transform.eulerAngles.x);
        float tiltZ = Mathf.Abs(transform.eulerAngles.z);
        
        // Normalize angles to 0-180 range
        if (tiltX > 180f) tiltX = 360f - tiltX;
        if (tiltZ > 180f) tiltZ = 360f - tiltZ;
        
        // If tilting too much, apply counter-torque
        if (tiltX > maxTiltAngle || tiltZ > maxTiltAngle)
        {
            // Strong righting force to prevent capsizing
            Vector3 rightingTorque = Vector3.Cross(transform.up, Vector3.up) * uprightTorque * 5f;
            rb.AddTorque(rightingTorque, ForceMode.Acceleration);
        }
    }
    
    private float GetWaterLevelAtPosition(Vector3 position)
    {
        if (!enableWaves)
        {
            return waterLevel;
        }
        
        // Simple sine wave simulation
        float waveX = Mathf.Sin(position.x * waveFrequency + Time.time * waveSpeed) * waveHeight;
        float waveZ = Mathf.Cos(position.z * waveFrequency + Time.time * waveSpeed) * waveHeight;
        
        return waterLevel + (waveX + waveZ) * 0.5f;
    }
    
    private void GenerateFloatingPoints()
    {
        Debug.Log("[BoatFloat] Auto-generating floating points...");
        
        // Get boat bounds
        Bounds bounds = GetComponentInChildren<Renderer>()?.bounds ?? new Bounds(transform.position, Vector3.one * 5f);
        
        // Create 4 corner points
        GameObject pointsParent = new GameObject("FloatingPoints");
        pointsParent.transform.SetParent(transform);
        pointsParent.transform.localPosition = Vector3.zero;
        
        Vector3 localSize = transform.InverseTransformVector(bounds.size);
        float width = localSize.x * 0.4f;
        float length = localSize.z * 0.4f;
        float height = localSize.y * -0.3f;
        
        floatingPoints = new Transform[4];
        Vector3[] positions = new Vector3[]
        {
            new Vector3(width, height, length),    // Front-right
            new Vector3(-width, height, length),   // Front-left
            new Vector3(-width, height, -length),  // Back-left
            new Vector3(width, height, -length)    // Back-right
        };
        
        for (int i = 0; i < positions.Length; i++)
        {
            GameObject point = new GameObject($"FloatPoint_{i}");
            point.transform.SetParent(pointsParent.transform);
            point.transform.localPosition = positions[i];
            floatingPoints[i] = point.transform;
        }
        
        Debug.Log($"[BoatFloat] Generated {floatingPoints.Length} floating points");
    }
    
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || floatingPoints == null) return;
        
        // Draw water plane
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Gizmos.DrawCube(new Vector3(transform.position.x, waterLevel, transform.position.z), new Vector3(20f, 0.1f, 20f));
        
        // Draw floating points
        foreach (Transform point in floatingPoints)
        {
            if (point == null) continue;
            
            float currentWaterLevel = GetWaterLevelAtPosition(point.position);
            bool isUnderwater = point.position.y < currentWaterLevel;
            
            Gizmos.color = isUnderwater ? Color.green : Color.red;
            Gizmos.DrawSphere(point.position, 0.2f);
            
            if (isUnderwater)
            {
                Gizmos.DrawLine(point.position, point.position + Vector3.up * 2f);
            }
        }
    }
}
