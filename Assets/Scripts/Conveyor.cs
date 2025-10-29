using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace DeathrunGame
{
    /// <summary>
    /// Conveyor belt implementation that applies constant tangential velocity to characters.
    /// Implements IMovingPlatform interface directly to provide both platform movement and conveyor functionality.
    /// Can be used for moving walkways, assembly lines, or any surface that should push players.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Conveyor : NetworkBehaviour, IMovingPlatform
    {
        #region Serialized Fields
        
        [Header("Platform Configuration")]
        [SerializeField] private bool isActive = true;
        [SerializeField] private int priority = 0;
        [SerializeField] private LayerMask affectedLayers = -1;
        
        [Header("Conveyor Settings")]
        [SerializeField] private Vector3 conveyorDirection = Vector3.forward;
        [SerializeField] private float conveyorSpeed = 5f;
        [SerializeField] private bool useLocalDirection = true;
        [SerializeField] private AnimationCurve speedCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        
        [Header("Visual Effects")]
        [SerializeField] private Renderer conveyorRenderer;
        [SerializeField] private string texturePropertyName = "_MainTex";
        [SerializeField] private bool animateTexture = true;
        [SerializeField] private float textureScrollSpeed = 1f;
        
        [Header("Audio")]
        [SerializeField] private AudioSource conveyorAudioSource;
        [SerializeField] private AudioClip conveyorSound;
        [SerializeField] private bool playAudioWhenActive = true;
        [SerializeField] private float audioPitchVariation = 0.1f;
        
        [Header("Effects")]
        [SerializeField] private ParticleSystem conveyorParticles;
        [SerializeField] private bool enableParticleEffects = true;
        
        [Header("Advanced Settings")]
        [SerializeField] private bool affectAirborneCharacters = false;
        [SerializeField] private float airborneEffectiveness = 0.3f;
        [SerializeField] private LayerMask conveyorMask = -1;
        [SerializeField] private float rampUpTime = 1f;
        [SerializeField] private float rampDownTime = 1f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool showGizmos = true;
        
        #endregion

        #region Private Fields
        
        // Conveyor state
        private float currentEffectiveness = 0f;
        private bool isConveyorActive = true;
        private Vector3 worldConveyorDirection;
        
        // Visual effects
        private Material conveyorMaterial;
        private Vector2 textureOffset = Vector2.zero;
        private float targetAudioPitch = 1f;
        
        // Character tracking
        private readonly HashSet<GameObject> charactersOnPlatform = new HashSet<GameObject>();
        private readonly Dictionary<GameObject, float> characterContactTimes = new Dictionary<GameObject, float>();
        
        // Components
        private new Collider collider;
        
        #endregion

        #region IMovingPlatform Implementation
        
        public Vector3 CurrentLinearVelocity => ConveyorVelocity;
        public Vector3 CurrentAngularVelocity => Vector3.zero; // Conveyors typically don't rotate
        public bool IsActive => isActive;
        public int Priority => priority;
        public bool HasTeleportedThisFrame => false; // Conveyors don't teleport

        public Vector3 GetSurfaceNormal(Vector3 contactPoint)
        {
            return transform.up;
        }

        public Vector3 GetVelocityAtPoint(Vector3 worldPoint)
        {
            return ConveyorVelocity;
        }

        public void OnCharacterEnter(GameObject character)
        {
            if (charactersOnPlatform.Add(character))
            {
                characterContactTimes[character] = 0f;
                Debug.Log($"Character {character.name} entered conveyor {name}");
            }
        }

        public void OnCharacterExit(GameObject character)
        {
            if (charactersOnPlatform.Remove(character))
            {
                characterContactTimes.Remove(character);
                Debug.Log($"Character {character.name} exited conveyor {name}");
            }
        }

        #endregion

        #region Properties
        
        /// <summary>
        /// Current conveyor velocity in world space
        /// </summary>
        public Vector3 ConveyorVelocity => worldConveyorDirection * conveyorSpeed * currentEffectiveness;
        
        /// <summary>
        /// Whether the conveyor is currently active
        /// </summary>
        public bool IsConveyorActive => isConveyorActive && IsActive;
        
        /// <summary>
        /// Current effectiveness multiplier (0-1, based on ramp up/down)
        /// </summary>
        public float CurrentEffectiveness => currentEffectiveness;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            collider = GetComponent<Collider>();
            
            // Initialize conveyor material
            if (conveyorRenderer != null)
            {
                conveyorMaterial = conveyorRenderer.material;
            }
            
            // Setup audio
            SetupAudio();
            
            // Initialize direction
            UpdateConveyorDirection();
        }

        private void Start()
        {
            // Start particle effects if enabled
            if (enableParticleEffects && conveyorParticles != null)
            {
                var main = conveyorParticles.main;
                main.loop = true;
                if (IsConveyorActive)
                {
                    conveyorParticles.Play();
                }
            }
        }

        private void Update()
        {
            // Update conveyor direction
            UpdateConveyorDirection();
            
            // Update effectiveness (ramp up/down)
            UpdateEffectiveness();
            
            // Update visual effects
            UpdateVisualEffects();
            
            // Update audio
            UpdateAudio();
        }

        private void FixedUpdate()
        {
            // Update contact times for all characters
            var charactersToUpdate = new List<GameObject>(characterContactTimes.Keys);
            foreach (var character in charactersToUpdate)
            {
                if (character != null)
                {
                    characterContactTimes[character] += Time.fixedDeltaTime;
                }
                else
                {
                    characterContactTimes.Remove(character);
                }
            }
        }

        #endregion

        #region Conveyor Logic
        
        private void UpdateConveyorDirection()
        {
            if (useLocalDirection)
            {
                worldConveyorDirection = transform.TransformDirection(conveyorDirection.normalized);
            }
            else
            {
                worldConveyorDirection = conveyorDirection.normalized;
            }
        }

        private void UpdateEffectiveness()
        {
            float targetEffectiveness = IsConveyorActive ? 1f : 0f;
            float rampSpeed = targetEffectiveness > currentEffectiveness ? 
                (1f / rampUpTime) : (1f / rampDownTime);
            
            currentEffectiveness = Mathf.MoveTowards(currentEffectiveness, targetEffectiveness, 
                rampSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Get the conveyor velocity to apply to a character
        /// </summary>
        /// <returns>Velocity vector to add to character movement</returns>
        public Vector3 GetConveyorVelocity()
        {
            if (!IsConveyorActive || currentEffectiveness <= 0f)
                return Vector3.zero;
            
            return ConveyorVelocity;
        }

        /// <summary>
        /// Get conveyor velocity for a specific character with individual modifiers
        /// </summary>
        /// <param name="character">Character to get velocity for</param>
        /// <param name="isGrounded">Whether the character is grounded</param>
        /// <returns>Velocity vector to apply</returns>
        public Vector3 GetConveyorVelocityForCharacter(GameObject character, bool isGrounded)
        {
            if (!IsConveyorActive || currentEffectiveness <= 0f)
                return Vector3.zero;
            
            // Check if character should be affected
            if (!ShouldAffectCharacter(character, isGrounded))
                return Vector3.zero;
            
            Vector3 baseVelocity = ConveyorVelocity;
            
            // Apply effectiveness curve based on contact time
            if (characterContactTimes.TryGetValue(character, out float contactTime))
            {
                float normalizedTime = Mathf.Clamp01(contactTime / rampUpTime);
                float curveMultiplier = speedCurve.Evaluate(normalizedTime);
                baseVelocity *= curveMultiplier;
            }
            
            // Reduce effectiveness for airborne characters
            if (!isGrounded && affectAirborneCharacters)
            {
                baseVelocity *= airborneEffectiveness;
            }
            
            return baseVelocity;
        }

        private bool ShouldAffectCharacter(GameObject character, bool isGrounded)
        {
            // Check layer mask
            if ((conveyorMask.value & (1 << character.layer)) == 0)
                return false;
            
            // Check if airborne characters should be affected
            if (!isGrounded && !affectAirborneCharacters)
                return false;
            
            return true;
        }

        #endregion

        #region Collision Detection
        
        private void OnTriggerEnter(Collider other)
        {
            if (ShouldAffectCollider(other))
            {
                OnCharacterEnter(other.gameObject);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (ShouldAffectCollider(other))
            {
                OnCharacterExit(other.gameObject);
            }
        }

        private bool ShouldAffectCollider(Collider other)
        {
            // Check if the collider is on an affected layer
            return (affectedLayers.value & (1 << other.gameObject.layer)) != 0;
        }

        #endregion

        #region Visual Effects
        
        private void UpdateVisualEffects()
        {
            if (animateTexture && conveyorMaterial != null)
            {
                UpdateTextureAnimation();
            }
            
            if (enableParticleEffects && conveyorParticles != null)
            {
                UpdateParticleEffects();
            }
        }

        private void UpdateTextureAnimation()
        {
            // Scroll texture based on conveyor movement
            Vector3 localVelocity = transform.InverseTransformDirection(ConveyorVelocity);
            
            // Use X and Z components for UV scrolling
            Vector2 scrollRate = new Vector2(localVelocity.x, localVelocity.z) * textureScrollSpeed * Time.deltaTime;
            textureOffset += scrollRate;
            
            // Wrap texture offset to prevent floating point precision issues
            textureOffset.x = textureOffset.x % 1f;
            textureOffset.y = textureOffset.y % 1f;
            
            conveyorMaterial.SetTextureOffset(texturePropertyName, textureOffset);
        }

        private void UpdateParticleEffects()
        {
            var velocityOverLifetime = conveyorParticles.velocityOverLifetime;
            velocityOverLifetime.enabled = currentEffectiveness > 0f;
            
            if (velocityOverLifetime.enabled)
            {
                velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
                velocityOverLifetime.x = ConveyorVelocity.x;
                velocityOverLifetime.y = ConveyorVelocity.y;
                velocityOverLifetime.z = ConveyorVelocity.z;
            }
            
            // Control particle emission based on effectiveness
            var emission = conveyorParticles.emission;
            emission.rateOverTime = emission.rateOverTime.constant * currentEffectiveness;
        }

        #endregion

        #region Audio
        
        private void SetupAudio()
        {
            if (conveyorAudioSource == null && conveyorSound != null)
            {
                conveyorAudioSource = gameObject.AddComponent<AudioSource>();
            }
            
            if (conveyorAudioSource != null && conveyorSound != null)
            {
                conveyorAudioSource.clip = conveyorSound;
                conveyorAudioSource.loop = true;
                conveyorAudioSource.playOnAwake = false;
                targetAudioPitch = conveyorAudioSource.pitch;
            }
        }

        private void UpdateAudio()
        {
            if (conveyorAudioSource == null || conveyorSound == null) return;
            
            bool shouldPlay = playAudioWhenActive && IsConveyorActive && currentEffectiveness > 0.1f;
            
            if (shouldPlay && !conveyorAudioSource.isPlaying)
            {
                conveyorAudioSource.Play();
            }
            else if (!shouldPlay && conveyorAudioSource.isPlaying)
            {
                conveyorAudioSource.Stop();
            }
            
            if (conveyorAudioSource.isPlaying)
            {
                // Adjust pitch based on effectiveness
                float pitchVariation = Random.Range(-audioPitchVariation, audioPitchVariation);
                conveyorAudioSource.pitch = targetAudioPitch * currentEffectiveness + pitchVariation;
                conveyorAudioSource.volume = currentEffectiveness;
            }
        }

        #endregion

        #region Public Interface
        
        /// <summary>
        /// Set the conveyor direction and speed
        /// </summary>
        /// <param name="direction">Direction vector (will be normalized)</param>
        /// <param name="speed">Speed in units per second</param>
        /// <param name="useLocal">Whether to treat direction as local space</param>
        public void SetConveyorMovement(Vector3 direction, float speed, bool useLocal = true)
        {
            conveyorDirection = direction.normalized;
            conveyorSpeed = speed;
            useLocalDirection = useLocal;
            UpdateConveyorDirection();
        }

        /// <summary>
        /// Enable or disable the conveyor
        /// </summary>
        /// <param name="active">Whether the conveyor should be active</param>
        /// <param name="immediate">Whether to bypass ramp up/down time</param>
        public void SetConveyorActive(bool active, bool immediate = false)
        {
            isConveyorActive = active;
            
            if (immediate)
            {
                currentEffectiveness = active ? 1f : 0f;
            }
            
            // Update particle effects
            if (conveyorParticles != null)
            {
                if (active && enableParticleEffects)
                {
                    conveyorParticles.Play();
                }
                else
                {
                    conveyorParticles.Stop();
                }
            }
        }

        /// <summary>
        /// Reverse the conveyor direction
        /// </summary>
        public void ReverseConveyor()
        {
            conveyorDirection = -conveyorDirection;
            UpdateConveyorDirection();
        }

        /// <summary>
        /// Set the conveyor speed
        /// </summary>
        /// <param name="speed">New speed value</param>
        public void SetConveyorSpeed(float speed)
        {
            conveyorSpeed = speed;
        }

        /// <summary>
        /// Get all characters currently on this conveyor
        /// </summary>
        /// <returns>Collection of character GameObjects</returns>
        public IReadOnlyCollection<GameObject> GetCharactersOnPlatform()
        {
            return charactersOnPlatform;
        }

        #endregion

        #region Network Synchronization
        
        /// <summary>
        /// Network RPC to synchronize conveyor state changes
        /// </summary>
        /// <param name="active">Whether conveyor is active</param>
        /// <param name="direction">Conveyor direction</param>
        /// <param name="speed">Conveyor speed</param>
        [ClientRpc]
        public void SyncConveyorStateClientRpc(bool active, Vector3 direction, float speed)
        {
            if (!IsServer)
            {
                isConveyorActive = active;
                conveyorDirection = direction;
                conveyorSpeed = speed;
                UpdateConveyorDirection();
            }
        }

        #endregion

        #region Debug Visualization
        
        private void OnDrawGizmos()
        {
            if (!showGizmos) return;
            
            // Draw platform bounds
            Gizmos.color = isActive ? Color.green : Color.red;
            if (collider != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                
                if (collider is BoxCollider boxCollider)
                {
                    Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
                }
                else if (collider is SphereCollider sphereCollider)
                {
                    Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
                }
                else
                {
                    Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                }
                
                Gizmos.matrix = Matrix4x4.identity;
            }
            
            if (!Application.isPlaying) return;
            
            // Draw conveyor direction
            Gizmos.color = IsConveyorActive ? Color.green : Color.red;
            Vector3 center = transform.position + Vector3.up * 0.1f;
            Vector3 direction = worldConveyorDirection * 2f * currentEffectiveness;
            
            if (direction.magnitude > 0.1f)
            {
                Gizmos.DrawRay(center, direction);
                
                // Draw arrowhead
                Vector3 arrowHead = center + direction;
                Vector3 arrowSide1 = arrowHead - direction.normalized * 0.3f + Vector3.Cross(direction.normalized, Vector3.up) * 0.15f;
                Vector3 arrowSide2 = arrowHead - direction.normalized * 0.3f - Vector3.Cross(direction.normalized, Vector3.up) * 0.15f;
                
                Gizmos.DrawLine(arrowHead, arrowSide1);
                Gizmos.DrawLine(arrowHead, arrowSide2);
            }
        }

        private void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;
            
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 4f);
            
            if (screenPos.z > 0)
            {
                Rect labelRect = new Rect(screenPos.x - 100, Screen.height - screenPos.y - 80, 200, 80);
                
                GUI.color = Color.white;
                GUI.Box(labelRect, "");
                
                GUI.color = Color.black;
                GUI.Label(labelRect, $"Conveyor: {name}\n" +
                                    $"Active: {IsConveyorActive}\n" +
                                    $"Speed: {conveyorSpeed:F1} m/s\n" +
                                    $"Effectiveness: {currentEffectiveness:F2}\n" +
                                    $"Characters: {charactersOnPlatform.Count}");
            }
        }

        #endregion

        #region Editor Utilities
        
        #if UNITY_EDITOR
        [ContextMenu("Test Reverse Conveyor")]
        private void TestReverseConveyor()
        {
            ReverseConveyor();
        }

        [ContextMenu("Toggle Conveyor")]
        private void TestToggleConveyor()
        {
            SetConveyorActive(!isConveyorActive);
        }

        private void OnValidate()
        {
            // Clamp values in editor
            conveyorSpeed = Mathf.Max(0f, conveyorSpeed);
            airborneEffectiveness = Mathf.Clamp01(airborneEffectiveness);
            textureScrollSpeed = Mathf.Max(0f, textureScrollSpeed);
            rampUpTime = Mathf.Max(0.1f, rampUpTime);
            rampDownTime = Mathf.Max(0.1f, rampDownTime);
            audioPitchVariation = Mathf.Max(0f, audioPitchVariation);
            
            // Ensure direction is normalized
            if (conveyorDirection.magnitude > 0.01f)
            {
                conveyorDirection = conveyorDirection.normalized;
            }
        }
        #endif

        #endregion
    }
}