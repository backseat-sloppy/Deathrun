using UnityEngine;

public class FallParticles : MonoBehaviour
{
    [Header("References")]
    public ParticleSystem fallEmitter;
    public Rigidbody rb;

    [Header("Settings")]
    public float fallThreshold = -2f; // minimum downward velocity before we consider it "falling"

    private bool isFalling;

    void Start()
    {
        // Auto-find Rigidbody if not assigned
        if (rb == null) 
        {
            rb = GetComponent<Rigidbody>();
        }
        
        if (fallEmitter) 
        {
            fallEmitter.Stop();
        }
        
        if (rb == null)
        {
            Debug.LogError("[FallParticles] No Rigidbody found! Please assign one.");
            enabled = false;
        }
    }

    void Update()
    {
        if (rb == null) return;

        // Get vertical velocity from Rigidbody
        float verticalVelocity = rb.linearVelocity.y;

        // Detect falling - only based on downward velocity
        bool currentlyFalling = verticalVelocity < fallThreshold;

        if (currentlyFalling && !isFalling)
        {
            // Player just started falling
            if (fallEmitter) fallEmitter.Play();
            isFalling = true;
        }
        else if (!currentlyFalling && isFalling)
        {
            // Player stopped falling (moving upward or stopped)
            if (fallEmitter) fallEmitter.Stop();
            isFalling = false;
        }
    }
}