using UnityEngine;

public class ArrowFollowVelocity : MonoBehaviour
{
    public Rigidbody rb;

    private void Update()
    {
        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            transform.forward = rb.linearVelocity.normalized;
        }
    }
}
