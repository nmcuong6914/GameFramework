using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System;

/// <summary>
/// Manages all audio in the game including background music and sound effects
/// Integrates with the existing ServiceLocator and AssetManager systems
/// Does not know about specific game events - responds to generic audio signals
/// </summary>
public class SoundManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    
    [Header("Audio Settings")]
    [SerializeField] private float musicVolume = 0.7f;
    [SerializeField] private float sfxVolume = 1.0f;
    [SerializeField] private bool muteMusic = false;  // Default: music on
    [SerializeField] private bool muteSFX = false;    // Default: sound on
    
    [Header("Sound Collection")]
    [SerializeField] private SoundCollection soundCollection;
    
    private AssetManager assetManager;
    private SoundDataManager soundDataManager;
    
    public bool IsInitialized { get; private set; } = false;

    /// <summary>
    /// Initialize the sound manager and register with services
    /// </summary>
    public async UniTask InitializeAsync()
    {
        Debug.Log("[SoundManager] Initializing Sound Manager...");
        
        // Setup audio sources if not assigned
        SetupAudioSources();
        
        // Get required services
        assetManager = ServiceLocator.TryResolve<AssetManager>();
        
        // Initialize sound data manager
        await InitializeSoundDataManager();
        
        // Load audio assets (both new system and legacy fallback)
        await LoadAudioAssets();
        
        // Load and apply saved audio settings
        LoadAudioSettings();
        
        // Apply initial volume settings
        UpdateVolumeSettings();
        
        IsInitialized = true;
        Debug.Log("[SoundManager] Sound Manager initialized successfully");
    }
    
    private async UniTask InitializeSoundDataManager()
    {
        if (soundCollection != null && assetManager != null)
        {
            soundDataManager = new SoundDataManager();
            await soundDataManager.InitializeAsync(soundCollection, assetManager);
            
            // Preload essential sounds
            await soundDataManager.PreloadEssentialSoundsAsync();
            
            Debug.Log("[SoundManager] Sound Data Manager initialized with SoundCollection");
        }
        else
        {
            Debug.LogWarning("[SoundManager] SoundCollection or AssetManager not available, using legacy audio loading");
        }
    }
    
    private void SetupAudioSources()
    {
        // Create music source if not assigned
        if (musicSource == null)
        {
            GameObject musicGO = new GameObject("MusicSource");
            musicGO.transform.SetParent(transform);
            musicSource = musicGO.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = musicVolume;
        }
        
        // Create SFX source if not assigned
        if (sfxSource == null)
        {
            GameObject sfxGO = new GameObject("SFXSource");
            sfxGO.transform.SetParent(transform);
            sfxSource = sfxGO.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = sfxVolume;
        }
    }
    
    private async UniTask LoadAudioAssets()
    {
        // Load additional game-specific audio clips
        await LoadAdditionalAudioClips();
        
        Debug.Log("[SoundManager] Audio assets loading completed - using SoundCollection system");
    }
    
    private async UniTask LoadAdditionalAudioClips()
    {
        // This method is no longer needed as we use SoundCollection system
        await UniTask.CompletedTask;
    }
    
    private void LoadAudioSettings()
    {
        var audioSettingsProvider = ServiceLocator.TryResolve<GameFramework.Audio.IAudioSettingsProvider>();
        if (audioSettingsProvider != null)
        {
            muteMusic = !audioSettingsProvider.IsMusicEnabled;
            muteSFX = !audioSettingsProvider.IsSoundEnabled;
            
            Debug.Log($"[SoundManager] Loaded audio settings from IAudioSettingsProvider - Music: {(audioSettingsProvider.IsMusicEnabled ? "enabled" : "disabled")}, Sound: {(audioSettingsProvider.IsSoundEnabled ? "enabled" : "disabled")}");
            
            // Start background music if music is enabled
            if (audioSettingsProvider.IsMusicEnabled && !IsMusicPlaying())
            {
                // Delay music start slightly to ensure everything is properly initialized
                StartBackgroundMusicDelayed().Forget();
            }
        }
        else
        {
            // Use default settings if PlayerData is not available
            muteMusic = false; // Default: music on
            muteSFX = false;   // Default: sound on
            
            Debug.Log("[SoundManager] PlayerData not available, using default audio settings - Music: enabled, Sound: enabled");
        }
    }
    
    /// <summary>
    /// Start background music with a small delay to ensure proper initialization
    /// </summary>
    private async UniTaskVoid StartBackgroundMusicDelayed()
    {
        await UniTask.DelayFrame(1);
        PlayBackgroundMusic();
    }

    /// <summary>
    /// Update audio settings from PlayerData (call this when settings change at runtime)
    /// </summary>
    public void RefreshAudioSettings()
    {
        bool wasMusicMuted = muteMusic;
        
        LoadAudioSettings();
        UpdateVolumeSettings();
        
        // If music was muted but is now enabled, start playing background music
        if (wasMusicMuted && !muteMusic)
        {
            PlayBackgroundMusic();
        }
        // If music was enabled but is now muted, stop the music
        else if (!wasMusicMuted && muteMusic)
        {
            StopMusic();
        }
    }
    
    /// <summary>
    /// Play background music (used for starting music when enabled)
    /// </summary>
    public void PlayBackgroundMusic()
    {
        if (!muteMusic && !IsMusicPlaying())
        {
            PlayMusic(SoundID.BackgroundMusic);
            Debug.Log("[SoundManager] Started background music");
        }
    }
    
    private void UpdateVolumeSettings()
    {
        if (musicSource != null)
        {
            musicSource.volume = muteMusic ? 0f : musicVolume;
        }
        
        if (sfxSource != null)
        {
            sfxSource.volume = muteSFX ? 0f : sfxVolume;
        }
    }

    

    
    /// <summary>
    /// Play a sound effect by SoundID enum
    /// </summary>
    /// <param name="soundId">ID of the sound to play</param>
    public void PlaySFX(SoundID soundId)
    {
        PlaySFXAsync(soundId).Forget();
    }
    
    /// <summary>
    /// Play a sound effect asynchronously
    /// </summary>
    /// <param name="soundId">ID of the sound to play</param>
    public async UniTaskVoid PlaySFXAsync(SoundID soundId)
    {
        if (muteSFX || sfxSource == null) return;
        
        AudioClip clip = null;
        float volume = sfxVolume;
        float pitch = 1f;
        
        // Get from sound system
        if (soundDataManager != null && soundDataManager.IsInitialized)
        {
            var loadedSound = soundDataManager.GetLoadedSound(soundId);
            if (loadedSound == null)
            {
                // Try to load it if not already loaded
                loadedSound = await soundDataManager.LoadSoundAsync(soundId);
            }
            
            if (loadedSound != null && loadedSound.IsLoaded)
            {
                clip = loadedSound.AudioClip;
                volume = loadedSound.SoundEntry.volume * sfxVolume;
                pitch = loadedSound.SoundEntry.pitch;
                
                // Apply sound settings
                var originalPitch = sfxSource.pitch;
                sfxSource.pitch = pitch;
                sfxSource.PlayOneShot(clip, volume);
                sfxSource.pitch = originalPitch; // Reset pitch for other sounds
                
                return;
            }
        }
        
        Debug.LogWarning($"[SoundManager] Audio clip for '{soundId}' not found in sound system");
    }
    
    /// <summary>
    /// Play a sound effect directly with AudioClip
    /// </summary>
    /// <param name="clip">AudioClip to play</param>
    public void PlaySFX(AudioClip clip)
    {
        if (muteSFX || sfxSource == null || clip == null) return;
        
        sfxSource.PlayOneShot(clip);
    }
    
    /// <summary>
    /// Set music volume (0-1)
    /// </summary>
    /// <param name="volume">Volume level</param>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = muteMusic ? 0f : musicVolume;
        }
    }
    
    /// <summary>
    /// Play background music by SoundID enum
    /// Music will loop automatically
    /// </summary>
    /// <param name="soundId">ID of the music to play</param>
    public void PlayMusic(SoundID soundId)
    {
        PlayMusicAsync(soundId).Forget();
    }
    
    /// <summary>
    /// Play background music asynchronously
    /// Music will loop automatically
    /// </summary>
    /// <param name="soundId">ID of the music to play</param>
    public async UniTaskVoid PlayMusicAsync(SoundID soundId)
    {
        if (muteMusic || musicSource == null) return;
        
        // Stop current music if playing
        if (musicSource.isPlaying)
        {
            musicSource.Stop();
        }
        
        AudioClip clip = null;
        float volume = musicVolume;
        float pitch = 1f;
        
        // Get from sound system
        if (soundDataManager != null && soundDataManager.IsInitialized)
        {
            var loadedSound = soundDataManager.GetLoadedSound(soundId);
            if (loadedSound == null)
            {
                // Try to load it if not already loaded
                loadedSound = await soundDataManager.LoadSoundAsync(soundId);
            }
            
            if (loadedSound != null && loadedSound.IsLoaded)
            {
                clip = loadedSound.AudioClip;
                volume = loadedSound.SoundEntry.volume * musicVolume;
                pitch = loadedSound.SoundEntry.pitch;
                
                // Apply music settings
                musicSource.clip = clip;
                musicSource.volume = volume;
                musicSource.pitch = pitch;
                musicSource.loop = true; // Music always loops
                musicSource.Play();
                
                Debug.Log($"[SoundManager] Playing music: {soundId}");
                return;
            }
        }
        
        Debug.LogWarning($"[SoundManager] Music clip for '{soundId}' not found in sound system");
    }
    
    /// <summary>
    /// Stop currently playing music
    /// </summary>
    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
            Debug.Log("[SoundManager] Music stopped");
        }
    }
    
    /// <summary>
    /// Pause currently playing music
    /// </summary>
    public void PauseMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
            Debug.Log("[SoundManager] Music paused");
        }
    }
    
    /// <summary>
    /// Resume paused music
    /// </summary>
    public void ResumeMusic()
    {
        if (musicSource != null && !musicSource.isPlaying)
        {
            musicSource.UnPause();
            Debug.Log("[SoundManager] Music resumed");
        }
    }
    
    /// <summary>
    /// Check if music is currently playing
    /// </summary>
    /// <returns>True if music is playing, false otherwise</returns>
    public bool IsMusicPlaying()
    {
        return musicSource != null && musicSource.isPlaying;
    }
    
    /// <summary>
    /// Get the currently playing music clip
    /// </summary>
    /// <returns>The current music AudioClip, or null if none is playing</returns>
    public AudioClip GetCurrentMusicClip()
    {
        return musicSource != null ? musicSource.clip : null;
    }
    
    /// <summary>
    /// Set SFX volume (0-1)
    /// </summary>
    /// <param name="volume">Volume level</param>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = muteSFX ? 0f : sfxVolume;
        }
    }
    
    /// <summary>
    /// Toggle music mute
    /// </summary>
    public void ToggleMusicMute()
    {
        muteMusic = !muteMusic;
        UpdateVolumeSettings();
    }
    
    /// <summary>
    /// Toggle SFX mute
    /// </summary>
    public void ToggleSFXMute()
    {
        muteSFX = !muteSFX;
        UpdateVolumeSettings();
    }
    
    /// <summary>
    /// Set music mute state
    /// </summary>
    /// <param name="mute">True to mute, false to unmute</param>
    public void SetMusicMute(bool mute)
    {
        muteMusic = mute;
        UpdateVolumeSettings();
    }
    
    /// <summary>
    /// Set SFX mute state
    /// </summary>
    /// <param name="mute">True to mute, false to unmute</param>
    public void SetSFXMute(bool mute)
    {
        muteSFX = mute;
        UpdateVolumeSettings();
    }
    
    /// <summary>
    /// Get current music volume
    /// </summary>
    public float GetMusicVolume() => musicVolume;
    
    /// <summary>
    /// Get current SFX volume
    /// </summary>
    public float GetSFXVolume() => sfxVolume;
    
    /// <summary>
    /// Check if music is muted
    /// </summary>
    public bool IsMusicMuted() => muteMusic;
    
    /// <summary>
    /// Check if SFX is muted
    /// </summary>
    public bool IsSFXMuted() => muteSFX;
    
    /// <summary>
    /// Check if a specific sound is available (loaded or can be loaded)
    /// </summary>
    /// <param name="soundId">The ID of the sound to check</param>
    /// <returns>True if available, false otherwise</returns>
    public bool IsSoundAvailable(SoundID soundId)
    {
        // Check sound collection system
        if (soundDataManager != null && soundDataManager.IsInitialized)
        {
            return soundCollection.HasSound(soundId);
        }
        
        return false;
    }
}