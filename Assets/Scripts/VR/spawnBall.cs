using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class spawnBall : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject ballPrefab;
    public float spawnSpeed = 5f;
    
    [Header("Input")]
    [SerializeField] private InputActionProperty spawnAction = new InputActionProperty(new InputAction("Spawn Ball", InputActionType.Button));
    
    private void OnEnable()
    {
        spawnAction.action?.Enable();
    }
    
    private void OnDisable()
    {
        spawnAction.action?.Disable();
    }

    void Start()
    {
        // Set up default binding if none exists
        if (string.IsNullOrEmpty(spawnAction.action.bindings[0].path))
        {
            spawnAction.action.AddBinding("<XRController>{LeftHand}/triggerPressed");
        }
    }

    void Update()
    {
        if (spawnAction.action?.WasPressedThisFrame() == true)
        {
            SpawnBall();
        }
    }
    
    private void SpawnBall()
    {
        if (ballPrefab == null) return;
        
        GameObject ball = Instantiate(ballPrefab, transform.position, transform.rotation);
        Rigidbody spawnedBallRigidbody = ball.GetComponent<Rigidbody>();
        if (spawnedBallRigidbody != null)
        {
            spawnedBallRigidbody.linearVelocity = transform.forward * spawnSpeed;
        }
    }
}
