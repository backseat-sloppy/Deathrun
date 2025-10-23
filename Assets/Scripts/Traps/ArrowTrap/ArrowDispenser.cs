using UnityEngine;
using System.Collections;

public class ArrowDispenser : MonoBehaviour
{
    [Header("References")]
    public GameObject arrowPrefab;
    public Transform[] spawnPoints;

    [Header("Settings")]
    public float shootForce = 40f;
    public float fireRate = 1f;
    public bool randomSpawn = false;

    [Header("Scatter Settings")]
    public float angleVariance = 5f; // degrees to randomly rotate arrow
    public Vector3 positionVariance = new Vector3(0.1f, 0.1f, 0f); // random offset in local space

    private bool isShooting = false;
    private int currentSpawnIndex = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            StartCoroutine(ShootArrows());
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StopAllCoroutines();
            isShooting = false;
        }
    }

    private IEnumerator ShootArrows()
    {
        if (isShooting) yield break;
        isShooting = true;

        while (isShooting)
        {
            ShootArrow();
            yield return new WaitForSeconds(1f / Mathf.Max(0.0001f, fireRate));
        }
    }

    private void ShootArrow()
    {
        if (spawnPoints.Length == 0) return;

        // Pick spawn point
        Transform spawnPoint;
        if (randomSpawn)
            spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        else
        {
            spawnPoint = spawnPoints[currentSpawnIndex];
            currentSpawnIndex = (currentSpawnIndex + 1) % spawnPoints.Length;
        }

        // Apply random position offset
        Vector3 spawnPos = spawnPoint.position + 
                           spawnPoint.TransformVector(new Vector3(
                               Random.Range(-positionVariance.x, positionVariance.x),
                               Random.Range(-positionVariance.y, positionVariance.y),
                               Random.Range(-positionVariance.z, positionVariance.z)
                           ));

        // Apply random rotation offset
        Quaternion randomRot = spawnPoint.rotation * Quaternion.Euler(
            Random.Range(-angleVariance, angleVariance),
            Random.Range(-angleVariance, angleVariance),
            Random.Range(-angleVariance, angleVariance)
        );

        // Spawn arrow
        GameObject arrow = Instantiate(arrowPrefab, spawnPos, randomRot);
        Rigidbody rb = arrow.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.AddForce(arrow.transform.forward * shootForce, ForceMode.VelocityChange);
        }

        // Attach helper scripts if missing
        ArrowFollowVelocity follow = arrow.GetComponent<ArrowFollowVelocity>();
        if (follow == null) follow = arrow.AddComponent<ArrowFollowVelocity>();
        follow.rb = rb;

        ArrowStick stick = arrow.GetComponent<ArrowStick>();
        if (stick == null) stick = arrow.AddComponent<ArrowStick>();
        stick.rb = rb;

        Destroy(arrow, 4f);
    }
}
