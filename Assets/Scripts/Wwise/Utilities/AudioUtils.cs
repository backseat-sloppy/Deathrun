using UnityEngine;

namespace DeathrunGame.Audio
{
    /// <summary>
    /// Audio utilities and helper functions for common Wwise operations.
    /// Provides convenience methods for parameter calculations and audio management.
    /// </summary>
    public static class AudioUtils
    {
        #region Parameter Calculation Helpers
        /// <summary>
        /// Convert velocity magnitude to a normalized parameter value.
        /// </summary>
        /// <param name="velocity">Velocity vector</param>
        /// <param name="maxSpeed">Maximum expected speed for normalization</param>
        /// <returns>Normalized speed value (0-1)</returns>
        public static float VelocityToSpeedParameter(Vector3 velocity, float maxSpeed = 20f)
        {
            float speed = velocity.magnitude;
            return Mathf.Clamp01(speed / maxSpeed);
        }

        /// <summary>
        /// Convert height to normalized parameter with custom curve.
        /// </summary>
        /// <param name="height">Height value</param>
        /// <param name="minHeight">Minimum height</param>
        /// <param name="maxHeight">Maximum height</param>
        /// <param name="curve">Optional animation curve for falloff</param>
        /// <returns>Normalized and curved height value</returns>
        public static float HeightToParameter(float height, float minHeight = 0f, float maxHeight = 50f, AnimationCurve curve = null)
        {
            float normalized = Mathf.InverseLerp(minHeight, maxHeight, height);
            
            if (curve != null)
            {
                normalized = curve.Evaluate(normalized);
            }
            
            return Mathf.Clamp01(normalized);
        }

        /// <summary>
        /// Convert damage amount to audio parameter.
        /// </summary>
        /// <param name="damage">Damage amount</param>
        /// <param name="maxDamage">Maximum expected damage</param>
        /// <returns>Normalized damage parameter</returns>
        public static float DamageToParameter(float damage, float maxDamage = 100f)
        {
            return Mathf.Clamp01(damage / maxDamage);
        }

        /// <summary>
        /// Convert health percentage to audio parameter.
        /// </summary>
        /// <param name="currentHealth">Current health</param>
        /// <param name="maxHealth">Maximum health</param>
        /// <returns>Health percentage (0-1)</returns>
        public static float HealthToParameter(float currentHealth, float maxHealth)
        {
            return Mathf.Clamp01(currentHealth / maxHealth);
        }
        #endregion

        #region Distance Calculations
        /// <summary>
        /// Calculate 3D distance parameter for audio falloff.
        /// </summary>
        /// <param name="listener">Listener position</param>
        /// <param name="source">Source position</param>
        /// <param name="maxDistance">Maximum distance for normalization</param>
        /// <returns>Normalized distance parameter</returns>
        public static float CalculateDistanceParameter(Vector3 listener, Vector3 source, float maxDistance = 100f)
        {
            float distance = Vector3.Distance(listener, source);
            return Mathf.Clamp01(distance / maxDistance);
        }

        /// <summary>
        /// Calculate 2D (horizontal) distance parameter.
        /// </summary>
        /// <param name="listener">Listener position</param>
        /// <param name="source">Source position</param>
        /// <param name="maxDistance">Maximum distance for normalization</param>
        /// <returns>Normalized 2D distance parameter</returns>
        public static float Calculate2DDistanceParameter(Vector3 listener, Vector3 source, float maxDistance = 100f)
        {
            Vector2 listener2D = new Vector2(listener.x, listener.z);
            Vector2 source2D = new Vector2(source.x, source.z);
            float distance = Vector2.Distance(listener2D, source2D);
            return Mathf.Clamp01(distance / maxDistance);
        }
        #endregion

        #region Surface Type Utilities
        /// <summary>
        /// Get surface type from material name using keyword matching.
        /// </summary>
        /// <param name="materialName">Name of the material</param>
        /// <returns>Detected surface type</returns>
        public static SurfaceType GetSurfaceTypeFromMaterial(string materialName)
        {
            if (string.IsNullOrEmpty(materialName)) return SurfaceType.Default;
            
            string name = materialName.ToLower();
            
            if (name.Contains("concrete") || name.Contains("stone") || name.Contains("brick") || name.Contains("cement"))
                return SurfaceType.Concrete;
            if (name.Contains("dirt") || name.Contains("ground") || name.Contains("earth") || name.Contains("soil") || name.Contains("grass"))
                return SurfaceType.Dirt;
            if (name.Contains("metal") || name.Contains("steel") || name.Contains("iron") || name.Contains("aluminum"))
                return SurfaceType.Metal;
            if (name.Contains("water") || name.Contains("liquid") || name.Contains("sea") || name.Contains("ocean"))
                return SurfaceType.Water;
            if (name.Contains("wood") || name.Contains("timber") || name.Contains("plank") || name.Contains("oak") || name.Contains("pine"))
                return SurfaceType.Wood;
                
            return SurfaceType.Default;
        }

        /// <summary>
        /// Get all switch names for surface types.
        /// </summary>
        /// <returns>Array of all surface switch names</returns>
        public static string[] GetAllSurfaceSwitches()
        {
            return new string[]
            {
                WwiseSwitches.SURFACE_CONCRETE,
                WwiseSwitches.SURFACE_DIRT,
                WwiseSwitches.SURFACE_METAL,
                WwiseSwitches.SURFACE_WATER,
                WwiseSwitches.SURFACE_WOOD
            };
        }
        #endregion

        #region Audio Event Validation
        /// <summary>
        /// Check if an event name is valid (not null or empty).
        /// </summary>
        /// <param name="eventName">Event name to validate</param>
        /// <returns>True if valid</returns>
        public static bool IsValidEventName(string eventName)
        {
            return !string.IsNullOrEmpty(eventName);
        }

        /// <summary>
        /// Check if a parameter name is valid.
        /// </summary>
        /// <param name="parameterName">Parameter name to validate</param>
        /// <returns>True if valid</returns>
        public static bool IsValidParameterName(string parameterName)
        {
            return !string.IsNullOrEmpty(parameterName);
        }

        /// <summary>
        /// Validate parameter value range.
        /// </summary>
        /// <param name="value">Value to validate</param>
        /// <param name="min">Minimum allowed value</param>
        /// <param name="max">Maximum allowed value</param>
        /// <returns>Clamped value within range</returns>
        public static float ValidateParameterValue(float value, float min = 0f, float max = 100f)
        {
            return Mathf.Clamp(value, min, max);
        }
        #endregion

        #region Timing Utilities
        /// <summary>
        /// Check if enough time has passed since last audio event.
        /// Useful for throttling frequent events like footsteps.
        /// </summary>
        /// <param name="lastEventTime">Time of last event</param>
        /// <param name="minimumInterval">Minimum time between events</param>
        /// <returns>True if enough time has passed</returns>
        public static bool CanTriggerEvent(float lastEventTime, float minimumInterval)
        {
            return Time.time - lastEventTime >= minimumInterval;
        }

        /// <summary>
        /// Calculate footstep interval based on movement speed.
        /// </summary>
        /// <param name="speed">Current movement speed</param>
        /// <param name="baseInterval">Base interval at normal speed</param>
        /// <param name="normalSpeed">Speed considered "normal"</param>
        /// <returns>Adjusted footstep interval</returns>
        public static float CalculateFootstepInterval(float speed, float baseInterval = 0.5f, float normalSpeed = 5f)
        {
            if (speed <= 0f) return float.MaxValue; // No footsteps when not moving
            
            float speedRatio = normalSpeed / speed;
            return baseInterval * speedRatio;
        }
        #endregion

        #region Performance Optimization
        /// <summary>
        /// Check if parameter change is significant enough to warrant an update.
        /// Helps reduce unnecessary parameter updates.
        /// </summary>
        /// <param name="oldValue">Previous parameter value</param>
        /// <param name="newValue">New parameter value</param>
        /// <param name="threshold">Minimum change threshold</param>
        /// <returns>True if change is significant</returns>
        public static bool IsSignificantParameterChange(float oldValue, float newValue, float threshold = 0.01f)
        {
            return Mathf.Abs(newValue - oldValue) >= threshold;
        }

        /// <summary>
        /// Quantize a parameter value to reduce precision and network traffic.
        /// </summary>
        /// <param name="value">Value to quantize</param>
        /// <param name="steps">Number of quantization steps</param>
        /// <returns>Quantized value</returns>
        public static float QuantizeParameter(float value, int steps = 20)
        {
            if (steps <= 1) return value;
            
            float stepSize = 1f / (steps - 1);
            return Mathf.Round(value / stepSize) * stepSize;
        }
        #endregion

        #region Debug Utilities
        /// <summary>
        /// Format audio parameter info for debug logging.
        /// </summary>
        /// <param name="parameterName">Parameter name</param>
        /// <param name="value">Parameter value</param>
        /// <param name="gameObjectName">GameObject name (optional)</param>
        /// <returns>Formatted debug string</returns>
        public static string FormatParameterDebugInfo(string parameterName, float value, string gameObjectName = null)
        {
            string target = string.IsNullOrEmpty(gameObjectName) ? "Global" : gameObjectName;
            return $"Audio Parameter: {parameterName} = {value:F2} [{target}]";
        }

        /// <summary>
        /// Format audio event info for debug logging.
        /// </summary>
        /// <param name="eventName">Event name</param>
        /// <param name="gameObjectName">GameObject name</param>
        /// <param name="playingId">Playing ID (optional)</param>
        /// <returns>Formatted debug string</returns>
        public static string FormatEventDebugInfo(string eventName, string gameObjectName, uint playingId = 0)
        {
            string idInfo = playingId != 0 ? $" (ID: {playingId})" : "";
            return $"Audio Event: {eventName} on {gameObjectName}{idInfo}";
        }
        #endregion
    }
}