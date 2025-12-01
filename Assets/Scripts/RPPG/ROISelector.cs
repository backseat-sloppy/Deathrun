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
    [SerializeField] private int borderThickness = 3;

    private WebcamController webcamController;
    private Rect roiRect;
    private Texture2D overlayTexture;

    void Start()
    {
        webcamController = GetComponent<WebcamController>();
    }

    void Update()
    {
        if (webcamController != null && webcamController.IsPlaying)
        {
            UpdateROI();
            
            if (showOverlay)
            {
                DrawROIOnTexture();
            }
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

    void DrawROIOnTexture()
    {
        if (webcamController.CameraTexture == null) return;

        int texWidth = webcamController.CameraTexture.width;
        int texHeight = webcamController.CameraTexture.height;

        // Create overlay texture if needed
        if (overlayTexture == null || overlayTexture.width != texWidth || overlayTexture.height != texHeight)
        {
            overlayTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
            overlayTexture.filterMode = FilterMode.Point;
        }

        // Get webcam pixels
        Color32[] pixels = webcamController.CameraTexture.GetPixels32();
        Color32[] overlayPixels = new Color32[pixels.Length];
        System.Array.Copy(pixels, overlayPixels, pixels.Length);

        // Calculate ROI bounds in texture coordinates
        int x = Mathf.Clamp((int)roiRect.x, 0, texWidth - 1);
        int y = Mathf.Clamp((int)roiRect.y, 0, texHeight - 1);
        int w = Mathf.Clamp((int)roiRect.width, 1, texWidth - x);
        int h = Mathf.Clamp((int)roiRect.height, 1, texHeight - y);

        Color32 borderColor = new Color32(
            (byte)(overlayColor.r * 255),
            (byte)(overlayColor.g * 255),
            (byte)(overlayColor.b * 255),
            (byte)(overlayColor.a * 255)
        );

        // Draw rectangle border
        for (int thickness = 0; thickness < borderThickness; thickness++)
        {
            // Top border
            for (int i = x - thickness; i < x + w + thickness; i++)
            {
                if (i >= 0 && i < texWidth && y + h + thickness < texHeight)
                {
                    int index = (y + h + thickness) * texWidth + i;
                    overlayPixels[index] = borderColor;
                }
            }

            // Bottom border
            for (int i = x - thickness; i < x + w + thickness; i++)
            {
                if (i >= 0 && i < texWidth && y - thickness >= 0)
                {
                    int index = (y - thickness) * texWidth + i;
                    overlayPixels[index] = borderColor;
                }
            }

            // Left border
            for (int i = y - thickness; i < y + h + thickness; i++)
            {
                if (i >= 0 && i < texHeight && x - thickness >= 0)
                {
                    int index = i * texWidth + (x - thickness);
                    overlayPixels[index] = borderColor;
                }
            }

            // Right border
            for (int i = y - thickness; i < y + h + thickness; i++)
            {
                if (i >= 0 && i < texHeight && x + w + thickness < texWidth)
                {
                    int index = i * texWidth + (x + w + thickness);
                    overlayPixels[index] = borderColor;
                }
            }
        }

        // Apply overlay to texture
        overlayTexture.SetPixels32(overlayPixels);
        overlayTexture.Apply();

        // Update the display with overlay
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        var webcamComp = GetComponent<WebcamController>();
        
        // Update UI RawImage if present
        var rawImage = webcamComp.GetType().GetField("displayImage", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(webcamComp) as UnityEngine.UI.RawImage;
        
        if (rawImage != null)
        {
            rawImage.texture = overlayTexture;
        }

        // Update 3D plane renderer if present
        var planeRenderer = webcamComp.GetType().GetField("planeRenderer", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(webcamComp) as Renderer;
        
        if (planeRenderer != null)
        {
            planeRenderer.material.mainTexture = overlayTexture;
        }
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

    void OnDestroy()
    {
        if (overlayTexture != null)
        {
            Destroy(overlayTexture);
        }
    }
}
