using UnityEngine;
using System.Collections;

/// <summary>
/// Controls boat movement along waypoints.
/// Waits for all players to board before starting journey.
/// Smoothly moves between waypoints while BoatFloat handles buoyancy.
/// </summary>
public class BoatController : MonoBehaviour
{
    [Header("Waypoint Navigation")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private int startWaypointIndex = 0;
    [SerializeField] private int endWaypointIndex = -1; // -1 means last waypoint
    
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float waypointReachThreshold = 1f;
    
    [Header("Auto-Start")]
    [SerializeField] private bool waitForPlayers = true;
    [SerializeField] private bool autoStartOnLoad = false;
    
    [Header("References")]
    [SerializeField] private PassengerTracker passengerTracker;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = true;
    [SerializeField] private bool showWaypointGizmos = true;
    
    private Rigidbody rb;
    private int currentWaypointIndex;
    private bool isMoving = false;
    private bool journeyStarted = false;
    
    public bool IsMoving => isMoving;
    public int CurrentWaypointIndex => currentWaypointIndex;
    public float Progress => CalculateJourneyProgress();
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        if (passengerTracker == null)
        {
            passengerTracker = GetComponentInChildren<PassengerTracker>();
        }
        
        if (endWaypointIndex < 0 && waypoints != null && waypoints.Length > 0)
        {
            endWaypointIndex = waypoints.Length - 1;
        }
    }
    
    private void Start()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogError("[BoatController] No waypoints assigned!");
            enabled = false;
            return;
        }
        
        // Set initial position to start waypoint
        if (waypoints[startWaypointIndex] != null)
        {
            transform.position = waypoints[startWaypointIndex].position;
            transform.rotation = waypoints[startWaypointIndex].rotation;
        }
        
        currentWaypointIndex = startWaypointIndex;
        
        // Subscribe to passenger tracker events
        if (passengerTracker != null && waitForPlayers)
        {
            passengerTracker.OnAllPlayersBoarded += StartJourney;
            Log("🚢 Waiting for all players to board...");
        }
        else if (autoStartOnLoad)
        {
            StartJourney();
        }
    }
    
    private void FixedUpdate()
    {
        if (!isMoving) return;
        
        if (currentWaypointIndex > endWaypointIndex)
        {
            // Journey complete
            StopJourney();
            Log("🏁 Journey complete!");
            return;
        }
        
        Transform targetWaypoint = waypoints[currentWaypointIndex];
        
        if (targetWaypoint == null)
        {
            Log($"⚠️ Waypoint {currentWaypointIndex} is null!");
            currentWaypointIndex++;
            return;
        }
        
        // Move towards waypoint
        Vector3 targetPosition = targetWaypoint.position;
        Vector3 direction = (targetPosition - transform.position).normalized;
        
        // Apply force towards target
        Vector3 targetVelocity = direction * moveSpeed;
        Vector3 velocityChange = targetVelocity - rb.linearVelocity;
        rb.AddForce(velocityChange, ForceMode.Acceleration);
        
        // Rotate towards movement direction
        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            Quaternion newRotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRotation);
        }
        
        // Check if reached waypoint
        float distanceToWaypoint = Vector3.Distance(transform.position, targetPosition);
        if (distanceToWaypoint < waypointReachThreshold)
        {
            Log($"✅ Reached waypoint {currentWaypointIndex}");
            currentWaypointIndex++;
        }
    }
    
    public void StartJourney()
    {
        if (journeyStarted)
        {
            Log("⚠️ Journey already started!");
            return;
        }
        
        journeyStarted = true;
        isMoving = true;
        currentWaypointIndex = startWaypointIndex + 1; // Start moving to next waypoint
        
        Log($"🚢 Journey started! Moving to waypoint {currentWaypointIndex}");
    }
    
    public void StopJourney()
    {
        isMoving = false;
        
        // Gradually slow down
        if (rb != null)
        {
            rb.linearVelocity *= 0.5f;
        }
        
        Log("⏸️ Journey stopped");
    }
    
    public void ResetJourney()
    {
        StopJourney();
        journeyStarted = false;
        currentWaypointIndex = startWaypointIndex;
        
        if (waypoints[startWaypointIndex] != null)
        {
            transform.position = waypoints[startWaypointIndex].position;
            transform.rotation = waypoints[startWaypointIndex].rotation;
            
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        
        Log("🔄 Journey reset to start");
    }
    
    private float CalculateJourneyProgress()
    {
        if (waypoints == null || waypoints.Length == 0) return 0f;
        
        int totalWaypoints = endWaypointIndex - startWaypointIndex;
        int completedWaypoints = currentWaypointIndex - startWaypointIndex - 1;
        
        return Mathf.Clamp01((float)completedWaypoints / totalWaypoints);
    }
    
    private void Log(string message)
    {
        if (showDebugMessages)
        {
            Debug.Log($"[BoatController] {message}");
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!showWaypointGizmos || waypoints == null) return;
        
        // Draw waypoint path
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] == null || waypoints[i + 1] == null) continue;
            
            bool isActive = i >= startWaypointIndex && i <= endWaypointIndex;
            Gizmos.color = isActive ? Color.cyan : Color.gray;
            
            Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            Gizmos.DrawWireSphere(waypoints[i].position, 0.5f);
        }
        
        // Draw final waypoint
        if (waypoints[waypoints.Length - 1] != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(waypoints[waypoints.Length - 1].position, 0.5f);
        }
        
        // Draw start waypoint
        if (waypoints[startWaypointIndex] != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(waypoints[startWaypointIndex].position, 0.7f);
        }
    }
}
