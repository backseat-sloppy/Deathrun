using UnityEngine;

public class WebcamDisplay : MonoBehaviour
{
    private WebcamController webcamController;
    private MeshRenderer childRenderer;

    void Start()
    {
        webcamController = GetComponent<WebcamController>();
        childRenderer = GetComponentInChildren<MeshRenderer>();
        
        if (childRenderer == null)
        {
            Debug.LogWarning("No MeshRenderer found in children. Add a Plane as a child.");
        }
    }

    void Update()
    {
        if (webcamController != null && webcamController.IsPlaying && childRenderer != null)
        {
            if (childRenderer.material.mainTexture == null)
            {
                childRenderer.material.mainTexture = webcamController.CameraTexture;
            }
        }
    }
}
