using UnityEngine;

public class PlayerDeathOnImpact : MonoBehaviour
{
    //restart position at empty object
    public Vector3 respawnPosition = new Vector3(0, 1, 0);

    // Velocity threshold for death on impact
    public float deathVelocityThreshold = 10f;
    //obejct tags that can cause death on impact
    public string[] deadlyTags = { "Enemy" };

    // Reference to the GameObject that marks the respawn position
    public Transform respawnPoint;

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

    private void Start()
    {
        // If no respawn point is set, use current position as fallback
        if (respawnPoint == null)
        {
            Debug.LogWarning("No respawn point assigned! Using player's starting position.");
        }
    }

    // Handle player death
    void Die()
    {
        // Disable or respawn player
        gameObject.SetActive(false);
    }

    //respawn player at respawn position on R press
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Respawn();
        }
    }
    void Respawn()
    {
        if (respawnPoint != null)
        {
            transform.position = respawnPoint.position;
        }
        else
        {
            transform.position = new Vector3(0, 1, 0); // Fallback position
        }
        gameObject.SetActive(true);
        Debug.Log("Player respawned");
    }
}
