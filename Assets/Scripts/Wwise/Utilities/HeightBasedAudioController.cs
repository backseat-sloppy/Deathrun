using UnityEngine;

namespace DeathrunGame.Audio
{
    /// <summary>
    /// Utility component for advanced height-based audio parameter control.
    /// Provides enhanced GP_Height parameter management with smoothing, zones, and falloff curves.
    /// Can be used alongside PlayerSoundController for more sophisticated height-based audio effects.
    /// </summary>
    public class HeightBasedAudioController : MonoBehaviour
    {
        [Header("Height Detection")]
        [SerializeField] private LayerMask groundLayerMask = -1;
        [SerializeField] private float maxDetectionDistance = 100f;
        [SerializeField] private float minHeight = 0f;
        [SerializeField] private float maxHeight = 50f;
        
        [Header("Parameter Smoothing")]
        [SerializeField] private bool smoothHeightParameter = true;
        [SerializeField] private float smoothingSpeed = 5f;
        [SerializeField] private float updateInterval = 0.1f;
        
        [Header("Height Zones")]
        [SerializeField] private HeightZone[] heightZones = new HeightZone[]
        {
            new HeightZone { name = "Ground", minHeight = 0f, maxHeight = 2f, parameterMultiplier = 0f },
            new HeightZone { name = "Low", minHeight = 2f, maxHeight = 10f, parameterMultiplier = 0.3f },
            new HeightZone { name = "Medium", minHeight = 10f, maxHeight = 25f, parameterMultiplier = 0.7f },
            new HeightZone { name = "High", minHeight = 25f, maxHeight = 50f, parameterMultiplier = 1f }
        };
        
        [Header("Falloff Curve")]
        [SerializeField] private AnimationCurve heightFalloffCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool drawDebugRays = false;

        #region Private Fields
        private WwiseAudioManager _audioManager;
        private float _currentHeight = 0f;
        private float _targetHeight = 0f;
        private float _lastUpdateTime = 0f;
        private HeightZone _currentZone;
        #endregion

        #region Events
        public event System.Action<float> OnHeightChanged;
        public event System.Action<HeightZone> OnHeightZoneChanged;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            _audioManager = WwiseAudioManager.Instance;
            
            if (_audioManager == null)
            {
                Debug.LogError("HeightBasedAudioController: WwiseAudioManager not found!");
                enabled = false;
                return;
            }

            // Initialize height
            UpdateHeight();
        }

        private void Update()
        {
            if (Time.time - _lastUpdateTime >= updateInterval)
            {
                UpdateHeight();
                _lastUpdateTime = Time.time;
            }

            if (smoothHeightParameter)
            {
                SmoothHeightParameter();
            }
        }
        #endregion

        #region Height Detection
        private void UpdateHeight()
        {
            float detectedHeight = DetectHeightAboveGround();
            
            if (Mathf.Abs(detectedHeight - _targetHeight) > 0.1f)
            {
                _targetHeight = detectedHeight;
                
                if (!smoothHeightParameter)
                {
                    _currentHeight = _targetHeight;
                    UpdateHeightParameter();
                }
                
                CheckHeightZone(detectedHeight);
                OnHeightChanged?.Invoke(detectedHeight);
            }
        }

        private float DetectHeightAboveGround()
        {
            Vector3 rayStart = transform.position;
            Ray ray = new Ray(rayStart, Vector3.down);
            
            if (Physics.Raycast(ray, out RaycastHit hit, maxDetectionDistance, groundLayerMask))
            {
                float height = hit.distance;
                
                if (drawDebugRays)
                {
                    Debug.DrawRay(rayStart, Vector3.down * hit.distance, Color.green, updateInterval);
                }
                
                return Mathf.Clamp(height, minHeight, maxHeight);
            }
            else
            {
                if (drawDebugRays)
                {
                    Debug.DrawRay(rayStart, Vector3.down * maxDetectionDistance, Color.red, updateInterval);
                }
                
                // No ground detected, assume maximum height
                return maxHeight;
            }
        }

        private void SmoothHeightParameter()
        {
            if (Mathf.Abs(_currentHeight - _targetHeight) > 0.01f)
            {
                _currentHeight = Mathf.Lerp(_currentHeight, _targetHeight, smoothingSpeed * Time.deltaTime);
                UpdateHeightParameter();
            }
        }

        private void UpdateHeightParameter()
        {
            float normalizedHeight = Mathf.InverseLerp(minHeight, maxHeight, _currentHeight);
            
            // Apply falloff curve
            float curvedHeight = heightFalloffCurve.Evaluate(normalizedHeight);
            
            // Apply zone multiplier if current zone exists
            if (_currentZone != null)
            {
                curvedHeight *= _currentZone.parameterMultiplier;
            }
            
            // Set the Wwise parameter
            _audioManager?.SetParameter(WwiseParameters.GP_HEIGHT, curvedHeight, gameObject);
            
            if (showDebugInfo)
            {
                Debug.Log($"HeightBasedAudioController: Height={_currentHeight:F1}, Normalized={normalizedHeight:F2}, Curved={curvedHeight:F2}, Zone={_currentZone?.name ?? "None"}");
            }
        }
        #endregion

        #region Height Zones
        private void CheckHeightZone(float height)
        {
            HeightZone newZone = GetHeightZoneForHeight(height);
            
            if (newZone != _currentZone)
            {
                HeightZone previousZone = _currentZone;
                _currentZone = newZone;
                
                OnHeightZoneChanged?.Invoke(newZone);
                
                if (showDebugInfo)
                {
                    Debug.Log($"HeightBasedAudioController: Zone changed from {previousZone?.name ?? "None"} to {newZone?.name ?? "None"}");
                }
            }
        }

        private HeightZone GetHeightZoneForHeight(float height)
        {
            foreach (HeightZone zone in heightZones)
            {
                if (height >= zone.minHeight && height <= zone.maxHeight)
                {
                    return zone;
                }
            }
            
            return null;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Manually set the height parameter value.
        /// </summary>
        /// <param name="height">Height value to set</param>
        /// <param name="normalized">Whether the height is already normalized (0-1)</param>
        public void SetHeight(float height, bool normalized = false)
        {
            if (normalized)
            {
                _currentHeight = Mathf.Lerp(minHeight, maxHeight, height);
            }
            else
            {
                _currentHeight = Mathf.Clamp(height, minHeight, maxHeight);
            }
            
            _targetHeight = _currentHeight;
            UpdateHeightParameter();
        }

        /// <summary>
        /// Get the current height above ground.
        /// </summary>
        /// <returns>Current height in world units</returns>
        public float GetCurrentHeight()
        {
            return _currentHeight;
        }

        /// <summary>
        /// Get the current height normalized to 0-1 range.
        /// </summary>
        /// <returns>Normalized height value</returns>
        public float GetNormalizedHeight()
        {
            return Mathf.InverseLerp(minHeight, maxHeight, _currentHeight);
        }

        /// <summary>
        /// Get the current height zone.
        /// </summary>
        /// <returns>Current height zone or null</returns>
        public HeightZone GetCurrentHeightZone()
        {
            return _currentZone;
        }

        /// <summary>
        /// Set new height range limits.
        /// </summary>
        /// <param name="newMinHeight">New minimum height</param>
        /// <param name="newMaxHeight">New maximum height</param>
        public void SetHeightRange(float newMinHeight, float newMaxHeight)
        {
            minHeight = newMinHeight;
            maxHeight = newMaxHeight;
            
            // Recalculate current parameter
            UpdateHeightParameter();
        }

        /// <summary>
        /// Add or update a height zone.
        /// </summary>
        /// <param name="zone">Height zone to add</param>
        public void AddHeightZone(HeightZone zone)
        {
            // Find existing zone with same name or add new one
            for (int i = 0; i < heightZones.Length; i++)
            {
                if (heightZones[i].name == zone.name)
                {
                    heightZones[i] = zone;
                    return;
                }
            }
            
            // Add new zone
            System.Array.Resize(ref heightZones, heightZones.Length + 1);
            heightZones[heightZones.Length - 1] = zone;
        }

        /// <summary>
        /// Force an immediate height update (bypasses update interval).
        /// </summary>
        public void ForceHeightUpdate()
        {
            UpdateHeight();
            if (!smoothHeightParameter)
            {
                UpdateHeightParameter();
            }
        }
        #endregion

        #region Debug
        private void OnDrawGizmosSelected()
        {
            if (showDebugInfo)
            {
                // Draw height zones
                Vector3 basePos = transform.position;
                
                foreach (HeightZone zone in heightZones)
                {
                    Color zoneColor = Color.HSVToRGB((zone.parameterMultiplier * 0.7f) % 1f, 0.8f, 0.8f);
                    zoneColor.a = 0.3f;
                    Gizmos.color = zoneColor;
                    
                    Vector3 zoneCenter = basePos + Vector3.up * ((zone.minHeight + zone.maxHeight) * 0.5f);
                    Vector3 zoneSize = new Vector3(2f, zone.maxHeight - zone.minHeight, 2f);
                    
                    Gizmos.DrawCube(zoneCenter, zoneSize);
                }
                
                // Draw current height
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position + Vector3.down * _currentHeight, 0.5f);
            }
        }
        #endregion
    }

    /// <summary>
    /// Data structure representing a height zone with parameter modification.
    /// </summary>
    [System.Serializable]
    public class HeightZone
    {
        [Header("Zone Definition")]
        public string name = "Zone";
        public float minHeight = 0f;
        public float maxHeight = 10f;
        
        [Header("Audio Modification")]
        [Range(0f, 2f)]
        public float parameterMultiplier = 1f;
        
        [Header("Zone Effects")]
        public string[] triggerEvents = new string[0]; // Events to trigger when entering this zone
        public string[] stopEvents = new string[0]; // Events to stop when leaving this zone
        
        public bool IsInZone(float height)
        {
            return height >= minHeight && height <= maxHeight;
        }
        
        public float GetNormalizedPositionInZone(float height)
        {
            if (!IsInZone(height)) return 0f;
            return Mathf.InverseLerp(minHeight, maxHeight, height);
        }
    }
}