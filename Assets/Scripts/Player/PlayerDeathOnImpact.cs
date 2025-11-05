using UnityEngine;

public class PlayerDeathOnImpact : MonoBehaviour
{
    // Velocity threshold for death on impact
    public float deathVelocityThreshold = 10f;
    //obejct tags that can cause death on impact
    public string[] deadlyTags = { "Enemy" };

    // Called when the collider enters a collision
    void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb != null && rb.linearVelocity.magnitude > deathVelocityThreshold)
        {
            Debug.Log($"Player killed by {collision.gameObject.name} with velocity {rb.linearVelocity.magnitude}");
            Die();
        }
    }
    void OnTriggerEnter(Collider other)
    {
        foreach (string tag in deadlyTags)
        {
            if (other.CompareTag(tag))
            {
                Debug.Log($"Player killed by trigger with {other.gameObject.name}");
                Die();
                break;
            }
        }
    }

    // Handle player death
    void Die()
    {
        // Disable or respawn player
        gameObject.SetActive(false);
    }
}
