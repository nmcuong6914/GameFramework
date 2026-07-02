using UnityEngine;

/// <summary>
/// Example showing how to use the Audio System (SoundManager + GameAudioBridge)
/// This file demonstrates the various ways to interact with audio in the game
/// </summary>
public class AudioSystemExample : MonoBehaviour
{
    private SoundManager soundManager;
    // private GameAudioBridge gameAudioBridge; // TODO: Uncomment after Unity compiles GameAudioBridge
    
    private void Start()
    {
        // Get audio components from ServiceLocator
        soundManager = ServiceLocator.TryResolve<SoundManager>();
        // gameAudioBridge = ServiceLocator.TryResolve<GameAudioBridge>(); // TODO: Uncomment after Unity compiles GameAudioBridge
        
        if (soundManager == null)
        {
            Debug.LogWarning("SoundManager not found in ServiceLocator");
        }
        
        // TODO: Uncomment after Unity compiles GameAudioBridge
        // if (gameAudioBridge == null)
        // {
        //     Debug.LogWarning("GameAudioBridge not found in ServiceLocator");
        // }
        
        // SoundManager usage - generic audio control:
        
        // Control background music
        // soundManager.PlayMusic(SoundID.BackgroundMusic);
        // soundManager.PlayMusic(SoundID.MainTheme);
        // soundManager.StopMusic();
        // soundManager.PauseMusic();
        // soundManager.ResumeMusic();
        
        // Play sound effects by name
        // soundManager.PlaySFX(SoundID.WinSFX);
        // soundManager.PlaySFX(SoundID.LoseSFX);
        // soundManager.PlaySFX(SoundID.ButtonClick);
        
        // Control volume (0.0 to 1.0)
        // soundManager.SetMusicVolume(0.8f);
        // soundManager.SetSFXVolume(1.0f);
        
        // Mute/unmute
        // soundManager.ToggleMusicMute();
        // soundManager.ToggleSFXMute();
        // soundManager.SetMusicMute(true);
        // soundManager.SetSFXMute(false);
        
        // Get current settings
        // float musicVolume = soundManager.GetMusicVolume();
        // float sfxVolume = soundManager.GetSFXVolume();
        // bool musicMuted = soundManager.IsMusicMuted();
        // bool sfxMuted = soundManager.IsSFXMuted();
        
        // GameAudioBridge usage - game-specific audio:
        
        // Manual triggers (usually these are called automatically via signals)
        // gameAudioBridge.PlayWinSound();
        // gameAudioBridge.PlayLoseSound();
        // gameAudioBridge.PlayButtonClickSound();
        
        // Configure bridge behavior
        // gameAudioBridge.SetWinLoseSoundsEnabled(true);
    }
    
    // Example: Play button click sound when a UI button is pressed
    // Option 1: Direct call to SoundManager (generic approach)
    public void OnButtonClickedDirect()
    {
        if (soundManager != null)
        {
            soundManager.PlaySFX(SoundID.ButtonClick);
        }
    }
    
    // Option 2: Call through GameAudioBridge (game-specific approach)
    public void OnButtonClickedViaBridge()
    {
        // TODO: Uncomment after Unity compiles GameAudioBridge
        // if (gameAudioBridge != null)
        // {
        //     gameAudioBridge.PlayButtonClickSound();
        // }
    }
    
    // Example: Manual win/lose SFX triggers (these normally happen automatically)
    public void PlayWinSound()
    {
        // Option 1: Direct
        if (soundManager != null)
        {
            soundManager.PlaySFX(SoundID.WinSFX);
        }
        
        // Option 2: Via bridge (recommended for consistency)
        // if (gameAudioBridge != null)
        // {
        //     gameAudioBridge.PlayWinSound();
        // }
    }
    
    public void PlayLoseSound()
    {
        // Option 1: Direct
        if (soundManager != null)
        {
            soundManager.PlaySFX(SoundID.LoseSFX);
        }
        
        // Option 2: Via bridge (recommended for consistency)
        // if (gameAudioBridge != null)
        // {
        //     gameAudioBridge.PlayLoseSound();
        // }
    }
    
    // Music Control Examples
    public void PlayBackgroundMusic()
    {
        if (soundManager != null)
        {
            soundManager.PlayMusic(SoundID.BackgroundMusic);
        }
    }
    
    public void PlayMainTheme()
    {
        if (soundManager != null)
        {
            soundManager.PlayMusic(SoundID.BackgroundMusic);
        }
    }
    
    public void StopMusic()
    {
        if (soundManager != null)
        {
            soundManager.StopMusic();
        }
    }
    
    public void PauseMusic()
    {
        if (soundManager != null)
        {
            soundManager.PauseMusic();
        }
    }
    
    public void ResumeMusic()
    {
        if (soundManager != null)
        {
            soundManager.ResumeMusic();
        }
    }
}

/* 
 * INTEGRATION NOTES:
 * 
 * 1. The SoundManager automatically:
 *    - Plays background music when the game initializes
 *    - Plays win SFX when WinGameSignal is fired
 *    - Plays lose SFX when LoseGameSignal is fired
 * 
 * 2. To setup in Unity:
 *    - Add SoundManager component to GameInitFlow GameObject
 *    - Assign audio clips in the inspector (or let it load via AssetManager)
 *    - Make sure the GameInitFlow has the soundManager reference assigned
 * 
 * 3. Audio Asset Setup:
 *    - Import your audio files into Unity
 *    - Either assign them directly in the SoundManager inspector
 *    - Or set them up in the AssetReferenceCollection with keys:
 *      * AssetKey.Audio_Music for background music
 *      * AssetKey.Audio_SFX for sound effects  
 *      * AssetKey.Audio_UI for UI sounds
 * 
 * 4. The system integrates with:
 *    - ServiceLocator (registered automatically)
 *    - SignalBus (listens for win/lose signals)
 *    - AssetManager (loads audio assets)
 */