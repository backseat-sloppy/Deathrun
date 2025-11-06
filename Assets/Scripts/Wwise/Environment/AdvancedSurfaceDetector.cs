using UnityEngine;
using System.Collections.Generic;

namespace DeathrunGame.Audio
{
    /// <summary>
    /// Advanced surface detector that can detect multiple surface types in an area.
    /// Uses raycasting and terrain analysis to determine the most appropriate surface type.
    /// </summary>
    public class AdvancedSurfaceDetector : MonoBehaviour, ISurfaceDetector
    {
        [Header("Detection Settings")]
        [SerializeField] private LayerMask surfaceLayerMask = -1;
        [SerializeField] private float detectionRange = 2f;
        [SerializeField] private int raycastSamples = 5;
        [SerializeField] private bool useTerrainDetection = true;
        [SerializeField] private bool cacheSurfaceTypes = true;
        
        [Header("Fallback Settings")]
        [SerializeField] private SurfaceType fallbackSurfaceType = SurfaceType.Concrete;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugRays = false;

        #region Events
        public event System.Action<SurfaceType> OnSurfaceTypeChanged;
        #endregion

        #region Private Fields
        private SurfaceType _lastDetectedSurface = SurfaceType.Default;
        private readonly Dictionary<Vector3Int, SurfaceType> _surfaceCache = new Dictionary<Vector3Int, SurfaceType>();
        private readonly List<RaycastHit> _hitResults = new List<RaycastHit>();
        #endregion

        #region ISurfaceDetector Implementation
        public SurfaceType GetSurfaceType(Vector3 position, Vector3 normal = default)
        {
            // Check cache first if enabled
            if (cacheSurfaceTypes)
            {
                Vector3Int cacheKey = Vector3Int.RoundToInt(position);
                if (_surfaceCache.TryGetValue(cacheKey, out SurfaceType cachedType))
                {
                    return cachedType;
                }
            }

            SurfaceType detectedType = DetectSurfaceAtPosition(position, normal);
            
            // Cache the result
            if (cacheSurfaceTypes)
            {
                Vector3Int cacheKey = Vector3Int.RoundToInt(position);
                _surfaceCache[cacheKey] = detectedType;
            }

            // Fire event if surface type changed
            if (detectedType != _lastDetectedSurface)
            {
                _lastDetectedSurface = detectedType;
                OnSurfaceTypeChanged?.Invoke(detectedType);
            }

            return detectedType;
        }
        #endregion

        #region Surface Detection Logic
        private SurfaceType DetectSurfaceAtPosition(Vector3 position, Vector3 normal)
        {
            _hitResults.Clear();
            
            // Perform multiple raycasts to get better surface detection
            Vector3[] rayDirections = GetRaycastDirections(normal);
            
            for (int i = 0; i < Mathf.Min(raycastSamples, rayDirections.Length); i++)
            {
                Vector3 rayDirection = rayDirections[i];
                Ray ray = new Ray(position, rayDirection);
                
                if (Physics.Raycast(ray, out RaycastHit hit, detectionRange, surfaceLayerMask))
                {
                    _hitResults.Add(hit);
                    
                    if (showDebugRays)
                    {
                        Debug.DrawRay(position, rayDirection * hit.distance, Color.green, 0.1f);
                    }
                }
                else if (showDebugRays)
                {
                    Debug.DrawRay(position, rayDirection * detectionRange, Color.red, 0.1f);
                }
            }

            // Analyze hits to determine surface type
            return AnalyzeHits(_hitResults, position);
        }

        private Vector3[] GetRaycastDirections(Vector3 preferredNormal)
        {
            Vector3 primaryDirection = preferredNormal != Vector3.zero ? -preferredNormal.normalized : Vector3.down;
            
            return new Vector3[]
            {
                primaryDirection,
                primaryDirection + Vector3.right * 0.2f,
                primaryDirection + Vector3.left * 0.2f,
                primaryDirection + Vector3.forward * 0.2f,
                primaryDirection + Vector3.back * 0.2f
            };
        }

        private SurfaceType AnalyzeHits(List<RaycastHit> hits, Vector3 position)
        {
            if (hits.Count == 0)
            {
                return HandleNoHits(position);
            }

            // Count surface types from hits
            Dictionary<SurfaceType, int> surfaceCounts = new Dictionary<SurfaceType, int>();
            
            foreach (RaycastHit hit in hits)
            {
                SurfaceType surfaceType = GetSurfaceTypeFromHit(hit);
                
                if (surfaceCounts.ContainsKey(surfaceType))
                {
                    surfaceCounts[surfaceType]++;
                }
                else
                {
                    surfaceCounts[surfaceType] = 1;
                }
            }

            // Return most common surface type
            SurfaceType mostCommon = fallbackSurfaceType;
            int maxCount = 0;
            
            foreach (var kvp in surfaceCounts)
            {
                if (kvp.Value > maxCount)
                {
                    maxCount = kvp.Value;
                    mostCommon = kvp.Key;
                }
            }

            return mostCommon;
        }

        private SurfaceType GetSurfaceTypeFromHit(RaycastHit hit)
        {
            // First, try to get surface type from SurfaceTypeProvider component
            SurfaceTypeProvider provider = hit.collider.GetComponent<SurfaceTypeProvider>();
            if (provider != null)
            {
                return provider.SurfaceType;
            }

            // Try parent object
            provider = hit.collider.GetComponentInParent<SurfaceTypeProvider>();
            if (provider != null)
            {
                return provider.SurfaceType;
            }

            // If terrain detection is enabled, check for terrain
            if (useTerrainDetection && hit.collider.GetComponent<Terrain>() != null)
            {
                return DetectTerrainSurface(hit);
            }

            // Fallback to name/tag based detection
            return DetectSurfaceFromNameAndTag(hit.collider);
        }

        private SurfaceType DetectTerrainSurface(RaycastHit hit)
        {
            Terrain terrain = hit.collider.GetComponent<Terrain>();
            if (terrain == null || terrain.terrainData == null)
            {
                return fallbackSurfaceType;
            }

            // Get the terrain texture weights at hit point
            Vector3 terrainPosition = hit.point - terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            Vector3 relativePosition = new Vector3(
                terrainPosition.x / terrainSize.x,
                terrainPosition.y / terrainSize.y,
                terrainPosition.z / terrainSize.z
            );
            
            int x = Mathf.FloorToInt(relativePosition.x * terrain.terrainData.alphamapWidth);
            int z = Mathf.FloorToInt(relativePosition.z * terrain.terrainData.alphamapHeight);
            
            x = Mathf.Clamp(x, 0, terrain.terrainData.alphamapWidth - 1);
            z = Mathf.Clamp(z, 0, terrain.terrainData.alphamapHeight - 1);

            float[,,] alphaMap = terrain.terrainData.GetAlphamaps(x, z, 1, 1);
            
            // Find the dominant texture
            int dominantTexture = 0;
            float maxWeight = 0f;
            
            for (int i = 0; i < alphaMap.GetLength(2); i++)
            {
                if (alphaMap[0, 0, i] > maxWeight)
                {
                    maxWeight = alphaMap[0, 0, i];
                    dominantTexture = i;
                }
            }

            // Map terrain texture index to surface type
            return MapTerrainTextureToSurface(dominantTexture, terrain);
        }

        private SurfaceType MapTerrainTextureToSurface(int textureIndex, Terrain terrain)
        {
            // This is a simple mapping - you may want to make this configurable
            // or add a component to terrain that maps texture indices to surface types
            
            TerrainData terrainData = terrain.terrainData;
            if (terrainData.terrainLayers != null && textureIndex < terrainData.terrainLayers.Length)
            {
                string textureName = terrainData.terrainLayers[textureIndex].diffuseTexture?.name?.ToLower() ?? "";
                
                if (textureName.Contains("grass") || textureName.Contains("dirt"))
                    return SurfaceType.Dirt;
                if (textureName.Contains("stone") || textureName.Contains("rock"))
                    return SurfaceType.Concrete;
                if (textureName.Contains("sand"))
                    return SurfaceType.Dirt;
                if (textureName.Contains("snow"))
                    return SurfaceType.Concrete;
            }

            return fallbackSurfaceType;
        }

        private SurfaceType DetectSurfaceFromNameAndTag(Collider collider)
        {
            string name = collider.name.ToLower();
            string tag = collider.tag.ToLower();
            string combined = name + " " + tag;

            // Material-based detection
            if (combined.Contains("concrete") || combined.Contains("stone") || combined.Contains("brick"))
                return SurfaceType.Concrete;
            if (combined.Contains("dirt") || combined.Contains("ground") || combined.Contains("earth") || combined.Contains("grass"))
                return SurfaceType.Dirt;
            if (combined.Contains("metal") || combined.Contains("steel") || combined.Contains("iron"))
                return SurfaceType.Metal;
            if (combined.Contains("water") || combined.Contains("liquid"))
                return SurfaceType.Water;
            if (combined.Contains("wood") || combined.Contains("timber") || combined.Contains("plank"))
                return SurfaceType.Wood;

            return fallbackSurfaceType;
        }

        private SurfaceType HandleNoHits(Vector3 position)
        {
            // If no hits, we might be in air or over water
            // You could add additional logic here, like checking for water planes below
            return fallbackSurfaceType;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Clear the surface type cache. Useful when terrain or surface objects change.
        /// </summary>
        public void ClearCache()
        {
            _surfaceCache.Clear();
        }

        /// <summary>
        /// Set the fallback surface type used when no specific surface is detected.
        /// </summary>
        /// <param name="newFallback">New fallback surface type</param>
        public void SetFallbackSurfaceType(SurfaceType newFallback)
        {
            fallbackSurfaceType = newFallback;
        }
        #endregion

        #region Debug
        private void OnDrawGizmosSelected()
        {
            if (showDebugRays)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, detectionRange);
            }
        }
        #endregion
    }
}