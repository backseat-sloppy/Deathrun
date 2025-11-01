using UnityEngine;

public class Eyes : MonoBehaviour
{
    public Transform mask;           // Assign your mask object
    public float maxDistance = 0.2f; // How far eyes can move in the socket
    public float followSpeed = 5f;   // How smoothly the eyes follow

    private Vector3 startLocalPos;
    private Camera mainCamera;

    void Start()
    {
        startLocalPos = transform.localPosition;
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (mask == null || mainCamera == null) return;

        // Get mouse position in screen space
        Vector3 mouseScreenPos = Input.mousePosition;
        
        // Set a fixed z distance for the camera to world space conversion
        mouseScreenPos.z = Mathf.Abs(mainCamera.transform.position.z - mask.position.z);
        
        // Convert to world space
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        
        // Get direction from mask to mouse in world space
        Vector3 direction = mouseWorldPos - mask.position;
        
        // Convert direction to local space of the mask
        Vector3 localDirection = mask.InverseTransformDirection(direction);
        
        // Keep the z position consistent
        localDirection.z = 0;
        
        // Clamp the movement within max distance
        if (localDirection.magnitude > maxDistance)
        {
            localDirection = localDirection.normalized * maxDistance;
        }
        
        // Calculate target position and maintain z position
        Vector3 targetLocalPos = startLocalPos + localDirection;
        targetLocalPos.z = startLocalPos.z;
        
        // Smooth movement using Lerp
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * followSpeed);
    }
}