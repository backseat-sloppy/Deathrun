using UnityEngine;

public class PlayerDeathOnImpact : MonoBehaviour
{
    public float deathVelocityThreshold = 10f;

    void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb != null && rb.linearVelocity.magnitude > deathVelocityThreshold)
        {
            Debug.Log($"Player killed by {collision.gameObject.name} with velocity {rb.linearVelocity.magnitude}");
            Die();
        }
    }

    void Die()
    {
        // Disable or respawn player
        gameObject.SetActive(false);
    }
}
