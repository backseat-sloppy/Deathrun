using UnityEngine;

public class FallParticles : MonoBehaviour
{
    [Header("References")]
    public ParticleSystem fallEmitter;
    public CharacterController controller;

    [Header("Settings")]
    public float fallThreshold = -2f; // minimum downward velocity before we consider it "falling"

    private Vector3 lastPosition;
    private bool isFalling;

    void Start()
    {
        if (!controller) controller = GetComponent<CharacterController>();
        if (fallEmitter) fallEmitter.Stop();
        lastPosition = transform.position;
    }

    void Update()
    {
        // Calculate vertical velocity (approximation)
        float verticalVelocity = (transform.position.y - lastPosition.y) / Time.deltaTime;
        lastPosition = transform.position;

        // Detect falling
        bool currentlyFalling = !controller.isGrounded && verticalVelocity < fallThreshold;

        if (currentlyFalling && !isFalling)
        {
            // Player just started falling
            if (fallEmitter) fallEmitter.Play();
            isFalling = true;
        }
        else if (!currentlyFalling && isFalling)
        {
            // Player landed or stopped falling
            if (fallEmitter) fallEmitter.Stop();
            isFalling = false;
        }
    }
}