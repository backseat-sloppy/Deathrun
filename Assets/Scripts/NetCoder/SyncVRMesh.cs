using UnityEngine;
using Unity.Netcode;

namespace DeathrunGame
{
    /// <summary>
    /// Synchronizes independent VR mesh GameObjects to follow the VR player's movements.
    /// The meshes exist as separate objects in the world and are positioned via RPC.
    /// This is a "hacked" solution that syncs only the visual meshes, not the Camera Rig itself.
    /// The VR player (host) sends position updates, and the meshes mirror the movements for all clients.
    /// </summary>
    public class SyncVRMesh : NetworkBehaviour
    {
        [Header("VR Transform References (What to Track)")]
        [Tooltip("The head/camera transform to track from the VR rig")]
        [SerializeField] private Transform headTransform;
        
        [Tooltip("The left hand controller transform to track from the VR rig")]
        [SerializeField] private Transform leftHandTransform;
        
        [Tooltip("The right hand controller transform to track from the VR rig")]
        [SerializeField] private Transform rightHandTransform;

        [Header("World Mesh GameObjects (What to Move)")]
        [Tooltip("The independent head mesh GameObject in the world (NOT a child of Camera Rig)")]
        [SerializeField] private GameObject headMesh;
        
        [Tooltip("The independent left hand mesh GameObject in the world")]  
        [SerializeField] private GameObject leftHandMesh;
        
        [Tooltip("The independent right hand mesh GameObject in the world")]
        [SerializeField] private GameObject rightHandMesh;

        [Header("Settings")]
        [Tooltip("How often to send transform updates per second")]
        [SerializeField] private float updatesPerSecond = 30f;
        
        [Tooltip("Enable smooth interpolation on clients")]
        [SerializeField] private bool smoothMovement = true;
        
        [Tooltip("Interpolation speed multiplier")]
        [SerializeField] private float interpolationSpeed = 20f;

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
            
            if (showDebugLogs)
            {
                Debug.Log($"[SyncVRMesh] OnNetworkSpawn - IsHost: {IsHost}, IsOwner: {IsOwner}");
            }

            if (IsHost)
            {
                // Host: Hide the meshes locally (VR player doesn't see their own mesh)
                SetMeshesVisibility(false);
                
                if (showDebugLogs)
                {
                    Debug.Log("[SyncVRMesh] Host - Meshes hidden locally");
                }
            }
            else
            {
                // Clients: Show the meshes and initialize interpolation targets
                SetMeshesVisibility(true);
                
                if (headMesh != null)
                {
                    targetHeadPos = headMesh.transform.position;
                    targetHeadRot = headMesh.transform.rotation;
                }
                if (leftHandMesh != null)
                {
                    targetLeftHandPos = leftHandMesh.transform.position;
                    targetLeftHandRot = leftHandMesh.transform.rotation;
                }
                if (rightHandMesh != null)
                {
                    targetRightHandPos = rightHandMesh.transform.position;
                    targetRightHandRot = rightHandMesh.transform.rotation;
                }
                
                if (showDebugLogs)
                {
                    Debug.Log("[SyncVRMesh] Client - Meshes visible, interpolation initialized");
                }
            }
        }

        private void Update()
        {
            if (!IsSpawned) return;

            if (IsHost)
            {
                // Host: Read VR tracking and send to all clients
                SendVRTransformsToClients();
            }
            else
            {
                // Clients: Apply received tracking data with interpolation
                ApplyInterpolation();
            }
        }

        /// <summary>
        /// Host: Read local VR tracking and send world positions to all clients via ClientRpc
        /// </summary>
        private void SendVRTransformsToClients()
        {
            updateTimer += Time.deltaTime;
            
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;

                // Get current WORLD-SPACE transforms from the VR rig
                Vector3 headPos = headTransform != null ? headTransform.position : Vector3.zero;
                Quaternion headRot = headTransform != null ? headTransform.rotation : Quaternion.identity;
                
                Vector3 leftHandPos = leftHandTransform != null ? leftHandTransform.position : Vector3.zero;
                Quaternion leftHandRot = leftHandTransform != null ? leftHandTransform.rotation : Quaternion.identity;
                
                Vector3 rightHandPos = rightHandTransform != null ? rightHandTransform.position : Vector3.zero;
                Quaternion rightHandRot = rightHandTransform != null ? rightHandTransform.rotation : Quaternion.identity;

                // Send to all clients (RPCs are sent to everyone including host, but we'll ignore on host)
                UpdateVRMeshTransformsClientRpc(
                    headPos, headRot,
                    leftHandPos, leftHandRot,
                    rightHandPos, rightHandRot
                );

                if (showDebugLogs && Time.frameCount % 180 == 0) // Log every 180 frames (3 seconds at 60fps)
                {
                    Debug.Log($"[SyncVRMesh] Host sending - Head: {headPos:F2}, LeftHand: {leftHandPos:F2}, RightHand: {rightHandPos:F2}");
                }
            }
        }

        /// <summary>
        /// ClientRpc: Receive transform data from host and update mesh positions
        /// </summary>
        [ClientRpc]
        private void UpdateVRMeshTransformsClientRpc(
            Vector3 headPos, Quaternion headRot,
            Vector3 leftHandPos, Quaternion leftHandRot,
            Vector3 rightHandPos, Quaternion rightHandRot)
        {
            // Don't apply on host (they have their own tracking and meshes are hidden)
            if (IsHost) return;

            // Update interpolation targets (meshes will lerp to these positions)
            targetHeadPos = headPos;
            targetHeadRot = headRot;
            targetLeftHandPos = leftHandPos;
            targetLeftHandRot = leftHandRot;
            targetRightHandPos = rightHandPos;
            targetRightHandRot = rightHandRot;

            if (showDebugLogs && Time.frameCount % 180 == 0)
            {
                Debug.Log($"[SyncVRMesh] Client received - Head: {headPos:F2}");
            }
        }

        /// <summary>
        /// Clients: Smoothly move the world meshes to match the VR player's tracked positions
        /// </summary>
        private void ApplyInterpolation()
        {
            float t = smoothMovement ? Time.deltaTime * interpolationSpeed : 1f;

            // Move the HEAD MESH in the world to match the VR player's head position
            if (headMesh != null)
            {
                headMesh.transform.position = Vector3.Lerp(headMesh.transform.position, targetHeadPos, t);
                headMesh.transform.rotation = Quaternion.Slerp(headMesh.transform.rotation, targetHeadRot, t);
            }

            // Move the LEFT HAND MESH in the world to match the VR player's left hand position
            if (leftHandMesh != null)
            {
                leftHandMesh.transform.position = Vector3.Lerp(leftHandMesh.transform.position, targetLeftHandPos, t);
                leftHandMesh.transform.rotation = Quaternion.Slerp(leftHandMesh.transform.rotation, targetLeftHandRot, t);
            }

            // Move the RIGHT HAND MESH in the world to match the VR player's right hand position
            if (rightHandMesh != null)
            {
                rightHandMesh.transform.position = Vector3.Lerp(rightHandMesh.transform.position, targetRightHandPos, t);
                rightHandMesh.transform.rotation = Quaternion.Slerp(rightHandMesh.transform.rotation, targetRightHandRot, t);
            }
        }

        /// <summary>
        /// Show or hide the world meshes
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

        /// <summary>
        /// Optional: Manually trigger a sync update (for testing)
        /// </summary>
        [ContextMenu("Force Sync Now")]
        public void ForceSyncNow()
        {
            if (IsHost)
            {
                updateTimer = updateInterval; // Force next update
                Debug.Log("[SyncVRMesh] Forced sync triggered");
            }
        }
    }
}
