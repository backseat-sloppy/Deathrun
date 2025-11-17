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

    // Reference to death screen manager
    private DeathScreenManager deathScreenManager;

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

        // Find death screen manager
        deathScreenManager = FindObjectOfType<DeathScreenManager>();
        if (deathScreenManager == null)
        {
            Debug.LogWarning("No DeathScreenManager found in scene. Death screen will not appear.");
        }
    }

    // Handle player death
    void Die()
    {
        // Show death screen
        if (deathScreenManager != null)
        {
            deathScreenManager.ShowDeathScreen();
        }
        else
        {
            // Fallback: just disable player
            gameObject.SetActive(false);
        }

        Debug.Log("💀 Player died");
    }

    //respawn player at respawn position on R press
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Respawn();
        }
    }

    public void Respawn()
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
        Debug.Log("✨ Player respawned");
    }
}
