using Unity.Netcode;
using UnityEngine;

namespace DeathrunGame
{
    /// <summary>
    /// Provides knockback to other players when bat swing hits them.
    /// Only active during swing animations.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class KnockbackProvider : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerStateStatus stateStatus;
        [SerializeField] private new Collider collider; // The bat's mesh collider

        [Header("Knockback Settings")]
        [Tooltip("Base knockback force applied horizontally")]
        [SerializeField] private float knockbackForce = 15f;
        
        [Tooltip("Upward force component for better feel")]
        [SerializeField] private float upwardForce = 5f;

        [Header("Cooldown")]
        [Tooltip("Cooldown between hits to prevent multiple knockbacks per swing")]
        [SerializeField] private float hitCooldown = 0.2f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private bool showGizmos = true;

        private float lastHitTime;
        private bool isColliderActive;

        private void Awake()
        {
            // Auto-assign if not set
            if (collider == null)
                collider = GetComponent<Collider>();

            if (stateStatus == null)
                stateStatus = GetComponentInParent<PlayerStateStatus>();

            // Ensure it's a trigger
            if (collider != null)
            {
                collider.isTrigger = true;
                collider.enabled = false; // Start disabled
            }
        }

        public override void OnNetworkSpawn()
        {
            if (stateStatus != null)
            {
                // Listen for swing state changes
                stateStatus.OnSwingStarted += OnSwingStarted;
                stateStatus.OnSwingEnded += OnSwingEnded;
            }

            if (showDebugLogs)
            {
                Debug.Log($"⚾ KnockbackProvider spawned. IsOwner: {IsOwner}");
            }
        }

        public override void OnNetworkDespawn()
        {
            if (stateStatus != null)
            {
                stateStatus.OnSwingStarted -= OnSwingStarted;
                stateStatus.OnSwingEnded -= OnSwingEnded;
            }
        }

        private void OnSwingStarted()
        {
            // Only owner activates the collider
            if (!IsOwner) return;

            EnableCollider();
            lastHitTime = -hitCooldown; // Reset cooldown

            if (showDebugLogs)
                Debug.Log("⚾ Bat collider ENABLED - ready to hit!");
        }

        private void OnSwingEnded()
        {
            // Only owner deactivates
            if (!IsOwner) return;

            DisableCollider();

            if (showDebugLogs)
                Debug.Log("⚾ Bat collider DISABLED");
        }

        private void EnableCollider()
        {
            if (collider != null)
            {
                collider.enabled = true;
                isColliderActive = true;
            }
        }

        private void DisableCollider()
        {
            if (collider != null)
            {
                collider.enabled = false;
                isColliderActive = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Only process on owner
            if (!IsOwner) return;
            if (!isColliderActive) return;

            // Cooldown check
            if (Time.time - lastHitTime < hitCooldown)
                return;

            // Check if we hit another player
            var hitPlayer = other.GetComponentInParent<NetworkObject>();
            if (hitPlayer == null) return;

            // Don't hit yourself
            if (hitPlayer.NetworkObjectId == NetworkObject.NetworkObjectId)
                return;

            // Check if target has required components
            var hitPlayerStateStatus = hitPlayer.GetComponent<PlayerStateStatus>();
            var knockbackReceiver = hitPlayer.GetComponent<KnockbackReceiver>();

            if (knockbackReceiver == null)
            {
                if (showDebugLogs)
                    Debug.LogWarning($"❌ Hit player has no KnockbackReceiver!");
                return;
            }

            // Don't hit dead players
            if (hitPlayerStateStatus != null && hitPlayerStateStatus.IsDead.Value)
                return;

            // Calculate knockback direction (no bonus from behind)
            Vector3 knockbackDirection = CalculateKnockbackDirection(hitPlayer.transform);

            // Apply knockback via RPC
            ApplyKnockbackServerRpc(hitPlayer.NetworkObjectId, knockbackDirection);

            lastHitTime = Time.time;

            if (showDebugLogs)
                Debug.Log($"💥 HIT PLAYER {hitPlayer.NetworkObjectId}! Knockback: {knockbackDirection}");
        }

        private Vector3 CalculateKnockbackDirection(Transform hitTransform)
        {
            // Direction from attacker to victim (horizontal)
            Vector3 horizontalDir = (hitTransform.position - transform.root.position);
            horizontalDir.y = 0f;
            horizontalDir.Normalize();

            // Combine horizontal and upward force
            Vector3 knockback = (horizontalDir * knockbackForce) + (Vector3.up * upwardForce);

            return knockback;
        }

        [ServerRpc]
        private void ApplyKnockbackServerRpc(ulong targetNetworkObjectId, Vector3 knockbackForce)
        {
            // Server validates and applies knockback
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject targetObject))
            {
                Debug.LogWarning($"⚠️ Target player {targetNetworkObjectId} not found on server!");
                return;
            }

            // Apply to all clients
            ApplyKnockbackClientRpc(targetNetworkObjectId, knockbackForce);
        }

        [ClientRpc]
        private void ApplyKnockbackClientRpc(ulong targetNetworkObjectId, Vector3 knockbackForce)
        {
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject targetObject))
                return;

            // Apply knockback via KnockbackReceiver
            var knockbackReceiver = targetObject.GetComponent<KnockbackReceiver>();
            if (knockbackReceiver != null)
            {
                knockbackReceiver.ApplyKnockback(knockbackForce);
            }
            else if (showDebugLogs)
            {
                Debug.LogWarning($"⚠️ Player {targetNetworkObjectId} has no KnockbackReceiver!");
            }
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos || collider == null) return;

            // Show the collider bounds in red when active, yellow when inactive
            Gizmos.color = isColliderActive ? Color.red : Color.yellow;
            
            if (collider is MeshCollider meshCollider && meshCollider.sharedMesh != null)
            {
                Gizmos.DrawWireMesh(meshCollider.sharedMesh, transform.position, transform.rotation, transform.lossyScale);
            }
            else
            {
                Gizmos.DrawWireCube(transform.position, transform.lossyScale);
            }
        }
    }
}
