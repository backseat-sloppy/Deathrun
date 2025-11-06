using UnityEngine;

namespace DeathrunGame.Audio
{
    /// <summary>
    /// Enum representing different surface types for footstep audio.
    /// Maps to Wwise switch values for the SurfaceType switch group.
    /// </summary>
    public enum SurfaceType
    {
        Concrete = 0,
        Dirt = 1,
        Metal = 2,
        Water = 3,
        Wood = 4,
        Default = Concrete // Fallback surface type
    }
    
    /// <summary>
    /// Static class containing all Wwise event names used in the game.
    /// Centralized location for managing audio event references.
    /// </summary>
    public static class WwiseEvents
    {
        // Player Events
        public const string PLAYER_DEATH_PLAY = "Player_Death_Play";
        public const string PLAYER_FOOTSTEP_PLAY = "Player_Footstep_Play";
        public const string PLAYER_JUMP_PLAY = "Player_Jump_Play";
        public const string PLAYER_LAND_PLAY = "Player_Land_Play";
        
        // Future events can be added here
        // public const string PLAYER_SWORD_SWING = "Player_Sword_Swing";
        // public const string ENVIRONMENT_TRAP_TRIGGER = "Environment_Trap_Trigger";
    }
    
    /// <summary>
    /// Static class containing all Wwise parameter (RTPC) names used in the game.
    /// </summary>
    public static class WwiseParameters
    {
        // Player Parameters
        public const string GP_PLAYER_SPEED = "GP_PlayerSpeed";
        public const string GP_HEIGHT = "GP_Height";
        
        // Future parameters can be added here
        // public const string GP_HEALTH = "GP_Health";
        // public const string GP_DAMAGE = "GP_Damage";
    }
    
    /// <summary>
    /// Static class containing all Wwise switch group and switch names.
    /// </summary>
    public static class WwiseSwitches
    {
        // Switch Groups
        public const string SURFACE_TYPE_GROUP = "SurfaceType";
        
        // Surface Type Switches
        public const string SURFACE_CONCRETE = "Concrete";
        public const string SURFACE_DIRT = "Dirt";
        public const string SURFACE_METAL = "Metal";
        public const string SURFACE_WATER = "Water";
        public const string SURFACE_WOOD = "Wood";
        
        /// <summary>
        /// Get the Wwise switch name for a given surface type.
        /// </summary>
        /// <param name="surfaceType">The surface type enum</param>
        /// <returns>Corresponding Wwise switch name</returns>
        public static string GetSurfaceSwitch(SurfaceType surfaceType)
        {
            return surfaceType switch
            {
                SurfaceType.Concrete => SURFACE_CONCRETE,
                SurfaceType.Dirt => SURFACE_DIRT,
                SurfaceType.Metal => SURFACE_METAL,
                SurfaceType.Water => SURFACE_WATER,
                SurfaceType.Wood => SURFACE_WOOD,
                _ => SURFACE_CONCRETE // Default fallback
            };
        }
    }
    
    /// <summary>
    /// Configuration data for audio events and their parameters.
    /// </summary>
    [System.Serializable]
    public class AudioEventConfig
    {
        [Header("Event Settings")]
        public string eventName;
        public bool useCallback = false;
        public bool stopOnDestroy = true;
        
        [Header("Fade Settings")]
        public float fadeInDuration = 0f;
        public float fadeOutDuration = 0f;
        
        [Header("3D Settings")]
        public bool is3D = true;
        public float maxDistance = 100f;
        
        [Header("Network Settings")]
        public bool syncAcrossNetwork = false;
        public bool ownerOnly = false;
    }
}