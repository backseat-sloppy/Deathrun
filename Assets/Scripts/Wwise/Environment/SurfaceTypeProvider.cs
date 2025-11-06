using UnityEngine;

namespace DeathrunGame.Audio
{
    /// <summary>
    /// Simple component that provides surface type information for objects.
    /// Attach this to any collider to specify what surface type it represents.
    /// </summary>
    public class SurfaceTypeProvider : MonoBehaviour
    {
        [Header("Surface Configuration")]
        [SerializeField] private SurfaceType surfaceType = SurfaceType.Default;
        [SerializeField] private bool applyToChildren = true;

        public SurfaceType SurfaceType => surfaceType;

        private void Start()
        {
            if (applyToChildren)
            {
                // Apply this surface type to all child colliders that don't have their own provider
                ApplyToChildColliders();
            }
        }

        private void ApplyToChildColliders()
        {
            Collider[] childColliders = GetComponentsInChildren<Collider>();
            
            foreach (Collider col in childColliders)
            {
                // Only apply if the collider doesn't already have a surface type provider
                if (col.gameObject != gameObject && col.GetComponent<SurfaceTypeProvider>() == null)
                {
                    SurfaceTypeProvider childProvider = col.gameObject.AddComponent<SurfaceTypeProvider>();
                    childProvider.surfaceType = this.surfaceType;
                    childProvider.applyToChildren = false; // Prevent infinite recursion
                }
            }
        }

        /// <summary>
        /// Set the surface type programmatically.
        /// </summary>
        /// <param name="newSurfaceType">New surface type to set</param>
        public void SetSurfaceType(SurfaceType newSurfaceType)
        {
            surfaceType = newSurfaceType;
        }

        private void OnValidate()
        {
            // Update any child providers when changed in inspector
            if (applyToChildren && Application.isPlaying)
            {
                ApplyToChildColliders();
            }
        }
    }
}