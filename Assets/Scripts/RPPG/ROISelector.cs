using UnityEngine;

public class ROISelector : MonoBehaviour
{
    [Header("ROI Position (normalized 0-1)")]
    [SerializeField] [Range(0f, 1f)] private float centerX = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float centerY = 0.7f;
    
    [Header("ROI Size (normalized 0-1)")]
    [SerializeField] [Range(0.05f, 1f)] private float width = 0.3f;
    [SerializeField] [Range(0.05f, 1f)] private float height = 0.15f;
    
    [Header("Visualization")]
    [SerializeField] private bool showOverlay = true;
    [SerializeField] private Color overlayColor = new Color(0, 1, 0, 0.5f);

    private WebcamController webcamController;
    private Rect roiRect;

    void Start()
    {
        webcamController = GetComponent<WebcamController>();
    }

    void Update()
    {
        if (webcamController != null && webcamController.IsPlaying)
        {
            UpdateROI();
        }
    }

    void UpdateROI()
    {
        int texWidth = webcamController.CameraTexture.width;
        int texHeight = webcamController.CameraTexture.height;

        // Convert normalized coords to pixel coords
        float pixelWidth = width * texWidth;
        float pixelHeight = height * texHeight;
        float pixelX = (centerX * texWidth) - (pixelWidth / 2f);
        float pixelY = (centerY * texHeight) - (pixelHeight / 2f);

        roiRect = new Rect(pixelX, pixelY, pixelWidth, pixelHeight);
    }

    public Color32[] GetROIPixels()
    {
        if (webcamController == null || !webcamController.IsPlaying) 
            return null;

        Color32[] allPixels = webcamController.GetPixelData();
        if (allPixels == null) 
            return null;

        int texWidth = webcamController.CameraTexture.width;
        int texHeight = webcamController.CameraTexture.height;

        int x = Mathf.Clamp((int)roiRect.x, 0, texWidth - 1);
        int y = Mathf.Clamp((int)roiRect.y, 0, texHeight - 1);
        int w = Mathf.Clamp((int)roiRect.width, 1, texWidth - x);
        int h = Mathf.Clamp((int)roiRect.height, 1, texHeight - y);

        Color32[] roiPixels = new Color32[w * h];
        
        for (int row = 0; row < h; row++)
        {
            for (int col = 0; col < w; col++)
            {
                int srcIndex = (y + row) * texWidth + (x + col);
                int dstIndex = row * w + col;
                roiPixels[dstIndex] = allPixels[srcIndex];
            }
        }

        return roiPixels;
    }

    void OnGUI()
    {
        if (showOverlay && webcamController != null && webcamController.IsPlaying)
        {
            // Draw ROI rectangle overlay
            GUI.color = overlayColor;
            
            // Scale to screen space (assuming camera feed fills screen)
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            
            Rect screenRect = new Rect(
                roiRect.x / webcamController.CameraTexture.width * screenWidth,
                (1f - (roiRect.y + roiRect.height) / webcamController.CameraTexture.height) * screenHeight,
                roiRect.width / webcamController.CameraTexture.width * screenWidth,
                roiRect.height / webcamController.CameraTexture.height * screenHeight
            );
            
            GUI.Box(screenRect, "");
            GUI.color = Color.white;
        }
    }
}
