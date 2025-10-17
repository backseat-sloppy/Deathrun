using UnityEngine;

public class ArrowStick : MonoBehaviour
{
    public Rigidbody rb;
    private bool stuck = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (stuck) return;

        stuck = true;

        // Stop all physics movement
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Attach to the object it hit (so it moves if the target moves)
        transform.SetParent(collision.transform);

        // Optionally destroy after a few seconds to clean up
        Destroy(gameObject, 4f);
    }
}
