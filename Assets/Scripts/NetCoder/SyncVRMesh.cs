using UnityEngine;
using Unity.Netcode;

namespace DeathrunGame
{
    /// <summary>
    /// Handles VR mesh visibility across the network.
    /// Hides meshes for the local VR player (host) but shows them to remote players.
    /// The actual transform syncing is handled by NetworkTransform components on the mesh GameObjects.
    /// Attach this script to the VR player's root GameObject with the NetworkObject.
    /// </summary>
    public class SyncVRMesh : NetworkBehaviour
    {
        [Header("VR Mesh References")]
        [Tooltip("Assign the head mesh GameObject (should have NetworkTransform)")]
        [SerializeField] private GameObject headMesh;
        
        [Tooltip("Assign the left hand mesh GameObject (should have NetworkTransform)")]  
        [SerializeField] private GameObject leftHandMesh;
        
        [Tooltip("Assign the right hand mesh GameObject (should have NetworkTransform)")]
        [SerializeField] private GameObject rightHandMesh;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // Hide meshes for the local VR player (host), show for remote players
            bool shouldShowMeshes = !IsHost;
            SetMeshesVisibility(shouldShowMeshes);
            
            if (showDebugLogs)
            {
                Debug.Log($"[SyncVRMesh] OnNetworkSpawn - IsHost: {IsHost}, ShowMeshes: {shouldShowMeshes}");
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
                if (showDebugLogs)
                    Debug.Log($"[SyncVRMesh] Head mesh visibility: {visible}");
            }

            if (leftHandMesh != null)
            {
                leftHandMesh.SetActive(visible);
                if (showDebugLogs)
                    Debug.Log($"[SyncVRMesh] Left hand mesh visibility: {visible}");
            }

            if (rightHandMesh != null)
            {
                rightHandMesh.SetActive(visible);
                if (showDebugLogs)
                    Debug.Log($"[SyncVRMesh] Right hand mesh visibility: {visible}");
            }
        }
    }
}
