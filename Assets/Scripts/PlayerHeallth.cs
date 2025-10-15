using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;  // Maximum health of the player
    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;  // Set the starting health
    }

    // Call this function to apply damage to the player
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;  // Decrease health by the damage amount

        Debug.Log("Player took " + damage + " damage. Current Health: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();  // Call the Die method when health reaches 0
        }
    }

    // Handle player death (optional: add death animation or other behaviors)
    private void Die()
    {
        Debug.Log("Player died!");
        // You can add logic for when the player dies, such as triggering a death animation or game over.
    }

    // Apply force to the player when hit by an enemy sword
    public void ApplyForce(Vector3 direction, float force)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(direction * force, ForceMode.Impulse);  // Apply force to the player
        }
    }
}

