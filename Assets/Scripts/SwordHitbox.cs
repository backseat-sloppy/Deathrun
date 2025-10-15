using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    public int damage = 10;  // Damage dealt to enemies
    public float force = 1000f;  // Force applied to physics objects

    private Collider swordCollider;  // The collider attached to the sword
    private Animator animator; // Reference to the animator (so we can trigger the swing)

    public bool isPlayer = false;  // Flag to check if this hitbox belongs to the player or enemy
    public bool IsHitboxActive { get; private set; }  // Track if the hitbox is active (public property)

    void Start()
    {
        swordCollider = GetComponent<Collider>();
        swordCollider.enabled = false;  // Disable hitbox initially
        IsHitboxActive = false;  // Set the hitbox as inactive
        animator = GetComponentInParent<Animator>(); // Assuming the sword is a child of the player or enemy
    }

    public void EnableHitbox()
    {
        swordCollider.enabled = true;  // Enable hitbox during the swing
        IsHitboxActive = true;  // Set the hitbox as active
        Debug.Log("Hitbox Enabled");
    }

    public void DisableHitbox()
    {
        swordCollider.enabled = false;  // Disable hitbox when swing is over
        IsHitboxActive = false;  // Set the hitbox as inactive
        Debug.Log("Hitbox Disabled");
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if the hitbox is attacking the opposite character
        if (isPlayer)
        {
            // Apply force to physics objects
            if (other.attachedRigidbody != null)
            {
                Vector3 direction = (other.transform.position - transform.position).normalized;
                other.attachedRigidbody.AddForce(direction * force);
                Debug.Log($"Force applied to: {other.name}");
            }
            EnemyHealth enemy = other.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Debug.Log($"Damage applied to: {other.name}");
            }
        }
        else
        {
            // Apply damage to the player
            if (other.CompareTag("Player"))
            {
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);  // Apply damage to the player
                    ApplyForce(other);  // Apply launch force if needed
                }
            }
        }
    }

    // Apply force to the hit object
    private void ApplyForce(Collider other)
    {
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 direction = (other.transform.position - transform.position).normalized;
            rb.AddForce(direction * force, ForceMode.Impulse);  // Apply force to the object
        }
    }
}









