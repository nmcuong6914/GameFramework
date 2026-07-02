# Audio System

The Audio System consists of two main components that work together to provide sound in the BlockSort game while maintaining clean separation of concerns.

## Architecture

### SoundManager
- **Purpose**: Generic audio playback system
- **Responsibilities**: Play/stop music and SFX, volume control, asset management
- **Knowledge**: Only knows about audio clips and basic playback - no game-specific logic

### GameAudioBridge  
- **Purpose**: Translates game events into audio commands
- **Responsibilities**: Listen for specific game signals and call appropriate SoundManager methods
- **Knowledge**: Knows about game-specific events like win/lose but only calls generic audio methods

## Features

- **Background Music**: Automatically plays when the game starts
- **Win/Lose SFX**: Automatically triggered by game signals via GameAudioBridge
- **Volume Control**: Separate controls for music and SFX
- **Mute/Unmute**: Toggle audio on/off
- **Asset Integration**: Works with AssetManager for dynamic audio loading
- **Decoupled Design**: SoundManager doesn't know about specific game events

## Setup in Unity

### 1. Add Audio Components
1. Open the GameInitFlow GameObject in your scene
2. Add the `SoundManager` component
3. Add the `GameAudioBridge` component
4. Assign both references in the GameInitFlow script

### 2. Configure Audio Assets
You have two options for setting up audio:

#### Option A: Direct Assignment (Simple)
1. In the SoundManager inspector, assign your audio clips directly:
   - Background Music
   - Win SFX
   - Lose SFX
   - Button Click SFX

#### Option B: AssetManager Integration (Recommended)
1. Set up your audio clips in the AssetReferenceCollection
2. Use these AssetKeys:
   - `Audio_BackgroundMusic` - Main background music
   - `Audio_WinSFX` - Sound when player wins
   - `Audio_LoseSFX` - Sound when player loses
   - `Audio_ButtonClick` - UI button click sound
   - `Audio_BlockMove` - Block movement sound (optional)
   - `Audio_BlockDestroy` - Block destruction sound (optional)

### 3. Audio Source Setup
The SoundManager will automatically create AudioSources if not assigned:
- **Music Source**: Looping, lower volume for background music
- **SFX Source**: One-shot sounds for game events

## How It Works

### Automatic Features
The SoundManager automatically:
- Initializes when the game starts
- Plays background music after game initialization
- Plays win SFX when `WinGameSignal` is fired
- Plays lose SFX when `LoseGameSignal` is fired

### Manual Control
You can also control audio manually:

```csharp
// Get the sound manager
var soundManager = ServiceLocator.Resolve<SoundManager>();

// Control background music
soundManager.PlayBackgroundMusic();
soundManager.StopBackgroundMusic();

// Play sound effects
soundManager.PlaySFX("win_sfx");
soundManager.PlaySFX("lose_sfx");
soundManager.PlaySFX("button_click");

// Volume control (0.0 to 1.0)
soundManager.SetMusicVolume(0.8f);
soundManager.SetSFXVolume(1.0f);

// Mute controls
soundManager.ToggleMusicMute();
soundManager.ToggleSFXMute();
soundManager.SetMusicMute(true);
soundManager.SetSFXMute(false);

// Get current settings
float musicVolume = soundManager.GetMusicVolume();
bool isMusicMuted = soundManager.IsMusicMuted();
```

## Integration Points

### GameInitFlow
Both SoundManager and GameAudioBridge are initialized during game startup and registered with the ServiceLocator.

### Signal System
**SoundManager** listens for:
- `GameInitializationCompleteSignal` - Starts background music

**GameAudioBridge** listens for:
- `WinGameSignal` - Calls SoundManager.PlaySFX("win_sfx")
- `LoseGameSignal` - Calls SoundManager.PlaySFX("lose_sfx")
- Can be extended to handle more game-specific events

### AssetManager
Loads audio clips dynamically using the asset key system.

## Audio Asset Keys

| Asset Key | Purpose | Required |
|-----------|---------|----------|
| `Audio_BackgroundMusic` | Main background music | Yes |
| `Audio_WinSFX` | Victory sound effect | Yes |
| `Audio_LoseSFX` | Game over sound effect | Yes |
| `Audio_ButtonClick` | UI interaction sound | Optional |
| `Audio_BlockMove` | Block movement sound | Optional |
| `Audio_BlockDestroy` | Block destruction sound | Optional |

## Best Practices

### 1. Audio File Setup
- Use compressed formats (OGG Vorbis) for background music
- Use uncompressed (WAV/AIFF) for short SFX
- Keep file sizes reasonable for mobile targets

### 2. Volume Levels
- Background music: 0.5-0.7 range
- SFX: 0.8-1.0 range
- Test on different devices for balance

### 3. Performance
- The system automatically handles audio source pooling
- Uses PlayOneShot for SFX to avoid interruption
- Background music loops automatically

## Troubleshooting

### No Sound Playing
1. Check if SoundManager is assigned in GameInitFlow
2. Verify audio clips are assigned or loaded via AssetManager
3. Check volume levels and mute settings
4. Ensure device volume is up and not muted

### Background Music Not Starting
1. Verify `GameInitializationCompleteSignal` is being fired
2. Check if background music clip is assigned
3. Confirm music is not muted

### Win/Lose SFX Not Playing
1. Ensure `WinGameSignal` and `LoseGameSignal` are being fired correctly
2. Check if SFX clips are assigned
3. Verify SFX is not muted

## Example Usage in UI

For button clicks:
```csharp
public class UIButton : MonoBehaviour
{
    private SoundManager soundManager;
    
    void Start()
    {
        soundManager = ServiceLocator.TryResolve<SoundManager>();
    }
    
    public void OnButtonClick()
    {
        soundManager?.PlaySFX("button_click");
        // Handle button logic...
    }
}
```

## Extending the System

To add new sounds:
1. Add new AssetKeys to the AssetKey enum
2. Update the LoadAudioAssets method in SoundManager
3. Add clips to your audioClips dictionary
4. Call PlaySFX with the new clip name

The sound system is designed to be flexible and extensible while providing automatic handling of core game audio events.
