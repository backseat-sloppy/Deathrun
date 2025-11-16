using UnityEngine;

/// <summary>
/// Component to identify surface types for audio.
/// Attach this to ground objects to define their surface material.
/// This is used by PlayerFootsteps to set the correct Wwise RTPC value.
/// </summary>
public class SurfaceType : MonoBehaviour
{
    #region Surface Type Enum

    public enum Surface
    {
        Concrete = 0,
        Dirt = 1,
        Metal = 2,
        Water = 3,
        Wood = 4
    }

    #endregion

    #region Serialized Fields

    [Header("Surface Settings")]
    [Tooltip("The type of surface this object represents")]
    [SerializeField] private Surface surfaceType = Surface.Concrete;

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the surface value as a float for Wwise RTPC.
    /// </summary>
    public float GetSurfaceValue()
    {
        return (float)surfaceType;
    }

    /// <summary>
    /// Gets the surface type enum.
    /// </summary>
    public Surface GetSurfaceType()
    {
        return surfaceType;
    }

    /// <summary>
    /// Sets the surface type programmatically.
    /// </summary>
    public void SetSurfaceType(Surface newSurface)
    {
        surfaceType = newSurface;
    }

    #endregion

    #region Editor Helpers

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Update the GameObject name to include surface type for easy identification
        // Uncomment if you want this behavior:
        // gameObject.name = $"{gameObject.name.Split('_')[0]}_{surfaceType}";
    }
#endif

    #endregion
}
