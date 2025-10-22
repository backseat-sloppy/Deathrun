using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class spawnBall : MonoBehaviour
{
    public GameObject ballPrefab;
    public float spawnSpeed = 5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
        {
            GameObject ball = Instantiate(ballPrefab, transform.position, Quaternion.identity);
            Rigidbody spawnedBallRigidbody = ball.GetComponent<Rigidbody>();
            spawnedBallRigidbody.linearVelocity = transform.forward * spawnSpeed;
        }
    }
}
