using UnityEngine;
using System.Collections;

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

    // Particle effect to play on death
    [Header("Death Effects")]
    public ParticleSystem deathParticles;
    public float particlePlayTime = 2f; // Time to wait for particles to play before fully disabling player
    
    [Header("Player Components to Disable")]
    public MeshRenderer playerMesh;
    public Collider playerCollider;
    public MonoBehaviour[] playerControlScripts; // Add any player control scripts here in inspector

    private bool isDead = false;
    private Rigidbody playerRigidbody;

    // Called when the collider enters a collision
    void OnCollisionEnter(Collision collision)
    {
        if (isDead) return; // Prevent multiple death triggers
        
        Rigidbody rb = collision.rigidbody;
        if (rb != null && rb.linearVelocity.magnitude > deathVelocityThreshold)
        {
            Debug.Log($"Player killed by {collision.gameObject.name} with velocity {rb.linearVelocity.magnitude}");
            Die();
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (isDead) return; // Prevent multiple death triggers
        
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

        // Ensure death particles are stopped initially
        if (deathParticles != null)
        {
            deathParticles.Stop();
        }

        // Try to auto-find player rigidbody
        playerRigidbody = GetComponent<Rigidbody>();

        // Try to auto-find player mesh if not assigned
        if (playerMesh == null)
        {
            playerMesh = GetComponentInChildren<MeshRenderer>();
        }

        // Try to auto-find player collider if not assigned
        if (playerCollider == null)
        {
            playerCollider = GetComponent<Collider>();
        }
    }

    // Handle player death
    void Die()
    {
        if (isDead) return; // Prevent multiple death calls
        isDead = true;

        Debug.Log("💀 Player died");

        // Start the death sequence
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // Step 1: Disable player controls and visuals immediately
        DisablePlayerControls();
        DisablePlayerMesh();
        
        // Freeze player movement
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.isKinematic = true;
        }

        // Step 2: Play death particle effect
        if (deathParticles != null)
        {
            deathParticles.transform.position = transform.position;
            deathParticles.Play();
        }

        // Step 3: Show death screen
        if (deathScreenManager != null)
        {
            deathScreenManager.ShowDeathScreen();
        }

        // Step 4: Wait for particles to play
        yield return new WaitForSeconds(particlePlayTime);

        // Step 5: Fully disable the player GameObject
        gameObject.SetActive(false);
    }

    private void DisablePlayerControls()
    {
        // Disable player collider to prevent further collisions
        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }

        // Disable all player control scripts
        if (playerControlScripts != null)
        {
            foreach (var script in playerControlScripts)
            {
                if (script != null)
                {
                    script.enabled = false;
                }
            }
        }
    }

    private void DisablePlayerMesh()
    {
        // Hide player mesh
        if (playerMesh != null)
        {
            playerMesh.enabled = false;
        }
    }

    private void EnablePlayerControls()
    {
        // Enable player collider
        if (playerCollider != null)
        {
            playerCollider.enabled = true;
        }

        // Enable all player control scripts
        if (playerControlScripts != null)
        {
            foreach (var script in playerControlScripts)
            {
                if (script != null)
                {
                    script.enabled = true;
                }
            }
        }
    }

    private void EnablePlayerMesh()
    {
        // Show player mesh
        if (playerMesh != null)
        {
            playerMesh.enabled = true;
        }
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
        // Stop any running death sequence
        StopAllCoroutines();
        
        // Stop death particles on respawn
        if (deathParticles != null)
        {
            deathParticles.Stop();
        }

        // Reset death state
        isDead = false;

        // Re-enable player mesh and controls
        EnablePlayerMesh();
        EnablePlayerControls();

        // Reset rigidbody
        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = false;
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        // Move player to respawn point
        if (respawnPoint != null)
        {
            transform.position = respawnPoint.position;
        }
        else
        {
            transform.position = new Vector3(0, 1, 0); // Fallback position
        }
        
        // Ensure player is active
        gameObject.SetActive(true);
        
        Debug.Log("✨ Player respawned");
    }
}