using UnityEngine;

public class Movement : MonoBehaviour
{
    public float moveSpeed = 0.5f;   // How fast it moves
    public float moveRange = 0.2f;   // How far it moves from its original position

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // Create smooth looping motion using sine waves
        float offsetX = Mathf.Sin(Time.time * moveSpeed) * moveRange;
        float offsetY = Mathf.Cos(Time.time * moveSpeed * 0.8f) * moveRange; // Slight variation for organic motion

        transform.position = startPos + new Vector3(offsetX, offsetY, 0);
    }
}