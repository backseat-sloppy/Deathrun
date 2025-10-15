using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target; // Reference to the player character (target to follow)
    public Vector3 offset; // Offset position from the target
    public float rotationSpeed = 5f; // Speed of camera rotation
    public float minYAngle = -35f; // Minimum vertical angle
    public float maxYAngle = 60f;  // Maximum vertical angle

    private float currentYaw = 0f; // Horizontal rotation (yaw)
    private float currentPitch = 0f; // Vertical rotation (pitch)

    void LateUpdate()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

        // Update yaw and pitch based on mouse input
        currentYaw += mouseX;
        currentPitch -= mouseY;

        // Clamp the pitch to prevent flipping
        currentPitch = Mathf.Clamp(currentPitch, minYAngle, maxYAngle);

        // Calculate camera's new rotation
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

        // Position the camera at the target's position plus the offset
        Vector3 targetPosition = target.position + rotation * offset;

        // Update the camera position
        transform.position = targetPosition;

        // Always look at the target's position (e.g., slightly above the feet)
        Vector3 lookAtPosition = target.position + Vector3.up * 1.5f; // Adjust the '1.5f' value for height
        transform.LookAt(lookAtPosition);
    }
}


