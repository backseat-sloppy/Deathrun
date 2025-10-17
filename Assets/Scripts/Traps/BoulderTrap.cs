using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoulderTrap : MonoBehaviour
{
    [Header("Assign the boulder in your scene")]
    public Rigidbody boulderRigidbody;

    [Header("Settings")]
    public float releaseDelay = 2f;
    public bool freezeOnStart = true;
    public string playerTag = "Player"; // tag to detect player

    private bool triggered = false;

    private void Start()
    {
        if (freezeOnStart && boulderRigidbody != null)
        {
            boulderRigidbody.isKinematic = true;
            boulderRigidbody.useGravity = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag(playerTag))
        {
            triggered = true;
            StartCoroutine(ReleaseBoulderCoroutine());
        }
    }

    private IEnumerator ReleaseBoulderCoroutine()
    {
        yield return new WaitForSeconds(releaseDelay);

        if (boulderRigidbody != null)
        {
            boulderRigidbody.isKinematic = false;
            boulderRigidbody.useGravity = true;
        }
    }
}
