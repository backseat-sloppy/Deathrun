using UnityEngine;
using Unity.Netcode;

namespace DeathrunGame
{
    /// <summary>
    /// Synchronizes VR player head and hand transforms across the network.
    /// Manually syncs transforms because Meta XR tracking updates aren't detected by NetworkTransform.
    /// The VR player (host) actively sends their tracking data to all clients.
    /// Also handles mesh visibility (hides meshes for the local VR player).
    /// </summary>
    public class SyncVRMesh : NetworkBehaviour
    {
        [Header("VR Transform References")]
        [Tooltip("The head/camera transform to track")]
        [SerializeField] private Transform headTransform;
        
        [Tooltip("The left hand controller transform to track")]
        [SerializeField] private Transform leftHandTransform;
        
        [Tooltip("The right hand controller transform to track")]
        [SerializeField] private Transform rightHandTransform;

        [Header("VR Mesh References")]
        [Tooltip("Assign the head mesh GameObject")]
        [SerializeField] private GameObject headMesh;
        
        [Tooltip("Assign the left hand mesh GameObject")]  
        [SerializeField] private GameObject leftHandMesh;
        
        [Tooltip("Assign the right hand mesh GameObject")]
        [SerializeField] private GameObject rightHandMesh;

        [Header("Settings")]
        [Tooltip("How often to send transform updates per second")]
        [SerializeField] private float updatesPerSecond = 30f;
        
        [Tooltip("Enable smooth interpolation on clients")]
        [SerializeField] private bool smoothMovement = true;
        
        [Tooltip("Interpolation speed multiplier")]
        [SerializeField] private float interpolationSpeed = 15f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // Client-side interpolation targets
        private Vector3 targetHeadPos;
        private Quaternion targetHeadRot;
        private Vector3 targetLeftHandPos;
        private Quaternion targetLeftHandRot;
        private Vector3 targetRightHandPos;
        private Quaternion targetRightHandRot;

        private float updateTimer = 0f;
        private float updateInterval;

        private void Awake()
        {
            updateInterval = 1f / updatesPerSecond;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // Hide meshes for the local VR player (host), show for remote players
            bool shouldShowMeshes = !IsHost;
            SetMeshesVisibility(shouldShowMeshes);
            
            if (showDebugLogs)
            {
                Debug.Log($"[SyncVRMesh] OnNetworkSpawn - IsHost: {IsHost}, IsOwner: {IsOwner}, ShowMeshes: {shouldShowMeshes}");
            }

            // Initialize interpolation targets for clients
            if (!IsHost && headMesh != null)
            {
                targetHeadPos = headMesh.transform.position;
                targetHeadRot = headMesh.transform.rotation;
            }
            if (!IsHost && leftHandMesh != null)
            {
                targetLeftHandPos = leftHandMesh.transform.position;
                targetLeftHandRot = leftHandMesh.transform.rotation;
            }
            if (!IsHost && rightHandMesh != null)
            {
                targetRightHandPos = rightHandMesh.transform.position;
                targetRightHandRot = rightHandMesh.transform.rotation;
            }
        }

        private void Update()
        {
            if (!IsSpawned) return;

            if (IsHost)
            {
                // Host: Send VR tracking data to all clients
                SendVRTransformsToClients();
            }
            else
            {
                // Clients: Apply received tracking data with interpolation
                ApplyInterpolation();
            }
        }

        /// <summary>
        /// Host: Read local VR tracking and send to all clients via ClientRpc
        /// </summary>
        private void SendVRTransformsToClients()
        {
            updateTimer += Time.deltaTime;
            
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;

                // Get current world-space transforms
                Vector3 headPos = headTransform != null ? headTransform.position : Vector3.zero;
                Quaternion headRot = headTransform != null ? headTransform.rotation : Quaternion.identity;
                
                Vector3 leftHandPos = leftHandTransform != null ? leftHandTransform.position : Vector3.zero;
                Quaternion leftHandRot = leftHandTransform != null ? leftHandTransform.rotation : Quaternion.identity;
                
                Vector3 rightHandPos = rightHandTransform != null ? rightHandTransform.position : Vector3.zero;
                Quaternion rightHandRot = rightHandTransform != null ? rightHandTransform.rotation : Quaternion.identity;

                // Send to all clients
                UpdateVRTransformsClientRpc(
                    headPos, headRot,
                    leftHandPos, leftHandRot,
                    rightHandPos, rightHandRot
                );

                if (showDebugLogs && Time.frameCount % 60 == 0) // Log every 60 frames to avoid spam
                {
                    Debug.Log($"[SyncVRMesh] Host sending - Head: {headPos}, LeftHand: {leftHandPos}, RightHand: {rightHandPos}");
                }
            }
        }

        /// <summary>
        /// ClientRpc: Receive transform data from host
        /// </summary>
        [ClientRpc]
        private void UpdateVRTransformsClientRpc(
            Vector3 headPos, Quaternion headRot,
            Vector3 leftHandPos, Quaternion leftHandRot,
            Vector3 rightHandPos, Quaternion rightHandRot)
        {
            // Don't apply on host (they have their own tracking)
            if (IsHost) return;

            // Update interpolation targets
            targetHeadPos = headPos;
            targetHeadRot = headRot;
            targetLeftHandPos = leftHandPos;
            targetLeftHandRot = leftHandRot;
            targetRightHandPos = rightHandPos;
            targetRightHandRot = rightHandRot;

            if (showDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[SyncVRMesh] Client received - Head: {headPos}");
            }
        }

        /// <summary>
        /// Clients: Smoothly interpolate meshes to target positions
        /// </summary>
        private void ApplyInterpolation()
        {
            float t = smoothMovement ? Time.deltaTime * interpolationSpeed : 1f;

            if (headMesh != null)
            {
                headMesh.transform.position = Vector3.Lerp(headMesh.transform.position, targetHeadPos, t);
                headMesh.transform.rotation = Quaternion.Slerp(headMesh.transform.rotation, targetHeadRot, t);
            }

            if (leftHandMesh != null)
            {
                leftHandMesh.transform.position = Vector3.Lerp(leftHandMesh.transform.position, targetLeftHandPos, t);
                leftHandMesh.transform.rotation = Quaternion.Slerp(leftHandMesh.transform.rotation, targetLeftHandRot, t);
            }

            if (rightHandMesh != null)
            {
                rightHandMesh.transform.position = Vector3.Lerp(rightHandMesh.transform.position, targetRightHandPos, t);
                rightHandMesh.transform.rotation = Quaternion.Slerp(rightHandMesh.transform.rotation, targetRightHandRot, t);
            }
        }

        /// <summary>
        /// Sets the visibility of all VR meshes.
        /// </summary>
        private void SetMeshesVisibility(bool visible)
        {
            if (headMesh != null)
            {
                headMesh.SetActive(visible);
            }

            if (leftHandMesh != null)
            {
                leftHandMesh.SetActive(visible);
            }

            if (rightHandMesh != null)
            {
                rightHandMesh.SetActive(visible);
            }

            if (showDebugLogs)
            {
                Debug.Log($"[SyncVRMesh] Meshes visibility set to: {visible}");
            }
        }
    }
}
