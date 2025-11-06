# Deathrun Wwise Audio System

A professional, multiplayer-ready Wwise audio system for Unity. This system provides comprehensive audio management with surface detection, height-based parameters, and network synchronization.

## 📁 Folder Structure

```
Assets/Scripts/Wwise/
├── Core/
│   ├── IAudioInterfaces.cs      # Audio interfaces and networking data structures
│   ├── WwiseConstants.cs        # Event names, parameters, and switch definitions
│   └── WwiseAudioManager.cs     # Singleton audio manager
├── Player/
│   ├── PlayerSoundController.cs         # Main player audio controller
│   ├── NetworkPlayerSoundController.cs  # Networked player audio (extends base)
│   └── FootstepCollider.cs              # Foot collision-based footstep detection
├── Environment/
│   ├── SurfaceTypeProvider.cs           # Component for defining surface types
│   └── AdvancedSurfaceDetector.cs       # Advanced surface detection system
├── Networking/
│   └── NetworkAudioSync.cs              # Network audio synchronization
└── Utilities/
    ├── HeightBasedAudioController.cs     # Advanced height parameter control
    └── AudioUtils.cs                     # Audio utility functions
```

## 🎵 Supported Wwise Events

- **Player_Death_Play** - Player death sound
- **Player_Footstep_Play** - Footstep sounds with surface switching
- **Player_Jump_Play** - Jump sound
- **Player_Land_Play** - Landing sound with height parameter

## 🎛️ Wwise Parameters (RTPCs)

- **GP_PlayerSpeed** - Player movement speed (0-20+ units/second)
- **GP_Height** - Height above ground for landing sounds (0-1 normalized)

## 🔄 Wwise Switches

- **SurfaceType** (Switch Group)
  - Concrete
  - Dirt  
  - Metal
  - Water
  - Wood

## 🚀 Quick Setup Guide

### 1. Basic Setup

1. Add `WwiseAudioManager` to your scene (will auto-create if missing)
2. Add `PlayerSoundController` or `NetworkPlayerSoundController` to your player prefab
3. Configure the audio settings in the inspector

### 2. For Multiplayer Games

Use `NetworkPlayerSoundController` instead of `PlayerSoundController`:

```csharp
// The NetworkPlayerSoundController automatically handles:
// - Network audio event synchronization
// - Parameter syncing across clients
// - Owner-only vs. everyone audio events
```

### 3. Footstep Detection Setup

**Option A: Foot Colliders (Recommended)**
```csharp
// Add FootstepCollider to each foot bone/GameObject
var leftFootCollider = leftFootBone.AddComponent<FootstepCollider>();
leftFootCollider.SetFootType(FootType.Left);

var rightFootCollider = rightFootBone.AddComponent<FootstepCollider>();
rightFootCollider.SetFootType(FootType.Right);
```

**Option B: Time-Based Footsteps (Fallback)**
```csharp
// PlayerSoundController will automatically fall back to time-based footsteps
// if no FootstepCollider components are found
```

### 4. Surface Detection Setup

**Option A: Simple Surface Provider**
```csharp
// Add SurfaceTypeProvider to any collider
var provider = groundObject.AddComponent<SurfaceTypeProvider>();
provider.SurfaceType = SurfaceType.Metal;
```

**Option B: Advanced Surface Detection**
```csharp
// Add AdvancedSurfaceDetector to player or environment
var detector = player.AddComponent<AdvancedSurfaceDetector>();
// Automatically detects surfaces via raycasting and terrain analysis
```

### 5. Height-Based Audio

For advanced height effects, add the `HeightBasedAudioController`:

```csharp
var heightController = player.AddComponent<HeightBasedAudioController>();
// Automatically manages GP_Height parameter with zones and smoothing
```

## 🎮 Usage Examples

### Playing Audio Events

```csharp
// Simple event triggering
WwiseAudioManager.Instance.PostEvent(WwiseEvents.PLAYER_JUMP_PLAY, gameObject);

// With parameters
WwiseAudioManager.Instance.SetParameter(WwiseParameters.GP_HEIGHT, 0.8f, gameObject);
WwiseAudioManager.Instance.PostEvent(WwiseEvents.PLAYER_LAND_PLAY, gameObject);

// Surface switching
WwiseAudioManager.Instance.SetSurfaceType(SurfaceType.Metal, gameObject);
```

### Multiplayer Audio Events

```csharp
// Network synchronized events (owner triggers, everyone hears)
var networkAudio = GetComponent<NetworkAudioSync>();
networkAudio.TriggerNetworkAudioEvent(WwiseEvents.PLAYER_DEATH_PLAY);

// With parameters (network optimized - sends parameters then event)
var parameters = new AudioParameterData[]
{
    new AudioParameterData(WwiseParameters.GP_HEIGHT, normalizedHeight, false)
};
networkAudio.TriggerNetworkAudioEventWithParameters(WwiseEvents.PLAYER_LAND_PLAY, parameters);
```

### Foot Collider Footsteps

```csharp
// Manual footstep triggering (useful for animation events)
var footCollider = GetComponent<FootstepCollider>();
footCollider.ManualTriggerFootstep();

// Enable/disable foot collider system
var playerSound = GetComponent<PlayerSoundController>();
playerSound.SetFootCollidersEnabled(true);

// Check if using foot colliders
bool usingColliders = playerSound.IsUsingFootColliders();
```

### Manual Surface Detection

```csharp
var playerSound = GetComponent<PlayerSoundController>();

// Force surface type update
playerSound.ForceUpdateSurfaceType();

// Manually trigger death sound
playerSound.PlayDeathSound();
```

## 🔧 Component Configuration

### PlayerSoundController Settings

- **Enable Audio**: Toggle audio on/off
- **Debug Audio**: Enable debug logging
- **Use Foot Colliders**: Whether to use FootstepCollider components (recommended)
- **Footstep Interval**: Time between footstep sounds (fallback if no foot colliders)
- **Min Speed For Footsteps**: Minimum movement speed to trigger footsteps
- **Land Height Threshold**: Minimum fall height to trigger landing sound
- **Max Land Height**: Maximum height for parameter normalization
- **Surface Detection Distance**: Raycast distance for surface detection

### FootstepCollider Settings

- **Foot Type**: Left or Right foot designation
- **Ground Layer Mask**: Which layers count as ground for footstep detection
- **Minimum Velocity Threshold**: Minimum movement speed to trigger footsteps
- **Footstep Cooldown**: Minimum time between footstep triggers (prevents rapid-fire)
- **Require Movement**: Whether player must be moving to trigger footsteps
- **Movement Threshold**: Minimum movement speed required

### NetworkPlayerSoundController Additional Settings

- **Sync Footsteps**: Whether to sync footstep events (usually disabled for performance)
- **Sync Movement Parameters**: Sync speed/height parameters across network
- **Parameter Sync Interval**: How often to sync parameters

### WwiseAudioManager Features

- **Automatic Initialization**: Checks Wwise initialization status
- **Event Tracking**: Tracks all playing events and their GameObjects
- **Parameter Management**: Centralized RTPC control
- **Switch Management**: Centralized switch control
- **Pause/Resume**: Application-wide audio pause/resume

## 🌐 Multiplayer Considerations

### Network Events
- **Death, Jump, Land**: Synchronized across all clients
- **Footsteps**: Local only (too frequent for networking)
- **Parameters**: Synced individually for network efficiency

### Performance Optimization
- Parameter updates are throttled to prevent network spam
- Events can be filtered by importance
- Local vs. networked audio separation
- Array serialization avoided for better network performance

### Client Authority
- Only the object owner can trigger networked audio events
- Non-owners receive and play events from network
- Fallback to local audio for non-networked scenarios

## 🔍 Debug Features

### Visual Debug
- Surface detection raycasts (when enabled)
- Height zones visualization
- Current surface type display

### Console Logging
- Event posting success/failure
- Parameter value changes
- Network audio synchronization
- Surface type changes

### Inspector Tools
- Real-time parameter monitoring
- Surface type override
- Network sync status

## 📋 Integration Checklist

- [ ] WwiseAudioManager present in scene
- [ ] Player has PlayerSoundController or NetworkPlayerSoundController
- [ ] FootstepCollider components added to each foot bone/GameObject (recommended)
- [ ] Ground objects have SurfaceTypeProvider components OR AdvancedSurfaceDetector on player
- [ ] Wwise events are correctly named and match WwiseConstants
- [ ] Network prefab has NetworkAudioSync component (for multiplayer)
- [ ] Audio layers and masks are properly configured
- [ ] Test all surface types and height-based landing sounds
- [ ] Test foot collider footstep detection vs. time-based fallback

## 🐛 Troubleshooting

### No Audio Playing
1. Check if Wwise is initialized (`AkUnitySoundEngine.IsInitialized()`)
2. Verify WwiseAudioManager is in scene and initialized
3. Check event names match exactly between code and Wwise project
4. Ensure GameObjects are active and enabled

### Surface Detection Not Working
1. Verify ground objects have SurfaceTypeProvider components
2. Check LayerMask settings on PlayerSoundController
3. Ensure Surface Detection Distance is sufficient
4. Check if AdvancedSurfaceDetector is properly configured

### Multiplayer Issues
1. Verify NetworkAudioSync is on network prefab
2. Check that only owners are triggering network events
3. Ensure network events are properly configured in NetworkPlayerSoundController
4. Test with multiple clients to verify synchronization

### Performance Issues
1. Disable footstep networking if enabled
2. Increase parameter sync interval
3. Reduce surface detection frequency
4. Use simple SurfaceTypeProvider instead of AdvancedSurfaceDetector where possible

## 📝 Extension Points

The system is designed to be easily extensible:

- Add new events to `WwiseEvents`
- Add new parameters to `WwiseParameters`
- Add new switches to `WwiseSwitches`
- Create custom surface detectors implementing `ISurfaceDetector`
- Extend `PlayerSoundController` for custom player audio behavior
- Add new components implementing `IAudioSource` for non-player audio

## 🔗 Dependencies

- Unity Netcode for GameObjects (for multiplayer features)
- Wwise Unity Integration
- Unity Physics (for surface detection raycasting)

---

*This audio system provides a solid foundation for professional game audio with Wwise. All components are designed to be modular, performant, and multiplayer-ready.*