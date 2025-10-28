using UnityEngine;

public class WreckingTrap : MonoBehaviour
{
    //swing object back and forth
    public float swingAngle = 45f;
    public float swingSpeed = 2f;
    private Quaternion initialRotation;
    void Start()
    {
        initialRotation = transform.rotation;
    }
    void Update()
    {
        float angle = swingAngle * Mathf.Sin(Time.time * swingSpeed);
        transform.rotation = initialRotation * Quaternion.Euler(0, 0, angle);
    }
    
}
