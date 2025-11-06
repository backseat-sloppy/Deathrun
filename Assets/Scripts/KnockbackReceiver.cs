using UnityEngine;

namespace DeathrunGame
{
    /// <summary>
    /// Receives and applies knockback force for Rigidbody-based movement.
    /// Multiplies existing velocity to create cumulative knockback effects.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class KnockbackReceiver : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Multiplier applied to combined velocity (existing + knockback)")]
        [SerializeField] private float velocityMultiplier = 1.2f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        public void ApplyKnockback(Vector3 knockbackForce)
        {
            if (rb == null) return;

            // Get current velocity
            Vector3 currentVelocity = rb.velocity;

            // Combine current velocity with knockback
            Vector3 newVelocity = (currentVelocity + knockbackForce) * velocityMultiplier;

            // Apply the multiplied velocity
            rb.velocity = newVelocity;

            if (showDebugLogs)
            {
                Debug.Log($"💨 Knockback applied! Current: {currentVelocity.magnitude:F2} | Knockback: {knockbackForce.magnitude:F2} | Result: {newVelocity.magnitude:F2}");
            }
        }
    }
}