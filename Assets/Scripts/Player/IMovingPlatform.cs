using UnityEngine;

namespace DeathrunGame
{
    /// <summary>
    /// Interface for moving platforms that can carry players.
    /// Exposes linear and angular velocity for smooth player movement integration.
    /// </summary>
    public interface IMovingPlatform
    {
        /// <summary>
        /// Transform of the platform for position and rotation tracking
        /// </summary>
        Transform transform { get; }
        
        /// <summary>
        /// Current linear velocity of the platform in world space (units/second)
        /// </summary>
        Vector3 CurrentLinearVelocity { get; }
        
        /// <summary>
        /// Current angular velocity of the platform in degrees/second around each axis
        /// </summary>
        Vector3 CurrentAngularVelocity { get; }
        
        /// <summary>
        /// Whether the platform is currently active and should affect players
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// Platform priority for when player is on multiple platforms (higher = takes precedence)
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// Whether this platform has teleported since the last frame
        /// (useful for detecting instant position changes that should not be applied as velocity)
        /// </summary>
        bool HasTeleportedThisFrame { get; }
        
        /// <summary>
        /// Surface normal of the platform at the contact point
        /// Used for proper slope handling and surface alignment
        /// </summary>
        Vector3 GetSurfaceNormal(Vector3 contactPoint);
        
        /// <summary>
        /// Get the velocity at a specific point on the platform
        /// Accounts for both linear and angular velocity
        /// </summary>
        /// <param name="worldPoint">Point in world space to calculate velocity for</param>
        /// <returns>Velocity at that point</returns>
        Vector3 GetVelocityAtPoint(Vector3 worldPoint);
        
        /// <summary>
        /// Called when a character starts standing on this platform
        /// </summary>
        /// <param name="character">The character that started contact</param>
        void OnCharacterEnter(GameObject character);
        
        /// <summary>
        /// Called when a character stops standing on this platform
        /// </summary>
        /// <param name="character">The character that ended contact</param>
        void OnCharacterExit(GameObject character);
    }
}