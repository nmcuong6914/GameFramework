using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System;
using System.Linq;

/// <summary>
/// Runtime data for loaded audio clips
/// Manages the lifecycle of loaded audio assets
/// </summary>
public class LoadedSoundData
{
    public SoundID SoundId { get; private set; }
    public AudioClip AudioClip { get; private set; }
    public SoundEntry SoundEntry { get; private set; }
    public bool IsLoaded { get; private set; }
    
    public LoadedSoundData(SoundID soundId, SoundEntry soundEntry)
    {
        SoundId = soundId;
        SoundEntry = soundEntry;
        AudioClip = null;
        IsLoaded = false;
    }
    
    public void SetAudioClip(AudioClip clip)
    {
        AudioClip = clip;
        IsLoaded = clip != null;
    }
    
    public void Release()
    {
        AudioClip = null;
        IsLoaded = false;
    }
}

/// <summary>
/// Manager for loading and caching audio clips from SoundCollection
/// Works with AssetManager and Addressables for efficient asset management
/// </summary>
public class SoundDataManager
{
    private SoundCollection soundCollection;
    private AssetManager assetManager;
    private Dictionary<SoundID, LoadedSoundData> loadedSounds;
    private HashSet<SoundID> currentlyLoading;
    
    public bool IsInitialized { get; private set; } = false;
    
    /// <summary>
    /// Initialize the sound data manager
    /// </summary>
    /// <param name="collection">The sound collection to use</param>
    /// <param name="assetManager">The asset manager for loading</param>
    public async UniTask InitializeAsync(SoundCollection collection, AssetManager assetManager)
    {
        if (IsInitialized)
        {
            Debug.LogWarning("[SoundDataManager] Already initialized");
            return;
        }
        
        this.soundCollection = collection;
        this.assetManager = assetManager;
        this.loadedSounds = new Dictionary<SoundID, LoadedSoundData>();
        this.currentlyLoading = new HashSet<SoundID>();
        
        if (soundCollection != null)
        {
            soundCollection.Initialize();
            Debug.Log($"[SoundDataManager] Initialized with {soundCollection.GetSoundCount()} sounds available");
        }
        else
        {
            Debug.LogError("[SoundDataManager] SoundCollection is null!");
        }
        
        IsInitialized = true;
        await UniTask.CompletedTask;
    }
    
    /// <summary>
    /// Load a sound by ID asynchronously
    /// </summary>
    /// <param name="soundId">The ID of the sound to load</param>
    /// <returns>LoadedSoundData if successful, null otherwise</returns>
    public async UniTask<LoadedSoundData> LoadSoundAsync(SoundID soundId)
    {
        if (!IsInitialized)
        {
            Debug.LogError("[SoundDataManager] Not initialized!");
            return null;
        }
        
        // Return cached sound if already loaded
        if (loadedSounds.TryGetValue(soundId, out var cachedSound) && cachedSound.IsLoaded)
        {
            return cachedSound;
        }
        
        // Prevent multiple simultaneous loads of the same sound
        if (currentlyLoading.Contains(soundId))
        {
            // Wait for the other load to complete
            while (currentlyLoading.Contains(soundId))
            {
                await UniTask.Yield();
            }
            
            // Return the result (might be null if load failed)
            return loadedSounds.TryGetValue(soundId, out var result) ? result : null;
        }
        
        // Get sound entry from collection
        var soundEntry = soundCollection?.GetSound(soundId);
        
        if (soundEntry == null)
        {
            Debug.LogError($"[SoundDataManager] Sound '{soundId}' not found in collection");
            return null;
        }
        
        // Check if asset key is valid for audio
        if (!soundEntry.assetKey.IsAudio())
        {
            Debug.LogError($"[SoundDataManager] Sound '{soundId}' has invalid asset key '{soundEntry.assetKey}' (not an audio asset)");
            return null;
        }
        
        currentlyLoading.Add(soundId);
        
        try
        {
            Debug.Log($"[SoundDataManager] Loading sound: {soundId}");
            
            // Use AssetManager to load the audio clip
            var audioClip = await LoadAudioClipThroughAssetManager(soundEntry);
            
            if (audioClip != null)
            {
                // Create or update loaded sound data
                var loadedSoundData = loadedSounds.TryGetValue(soundId, out var existing) 
                    ? existing 
                    : new LoadedSoundData(soundId, soundEntry);
                
                loadedSoundData.SetAudioClip(audioClip);
                loadedSounds[soundId] = loadedSoundData;
                
                Debug.Log($"[SoundDataManager] Successfully loaded sound: {soundId}");
                return loadedSoundData;
            }
            else
            {
                Debug.LogError($"[SoundDataManager] Failed to load audio clip for sound: {soundId}");
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SoundDataManager] Exception loading sound '{soundId}': {e.Message}");
            return null;
        }
        finally
        {
            currentlyLoading.Remove(soundId);
        }
    }
    
    /// <summary>
    /// Load multiple sounds asynchronously
    /// </summary>
    /// <param name="soundIds">Array of sound IDs to load</param>
    /// <returns>Dictionary of loaded sounds (key: soundId, value: LoadedSoundData or null if failed)</returns>
    public async UniTask<Dictionary<SoundID, LoadedSoundData>> LoadSoundsAsync(params SoundID[] soundIds)
    {
        var results = new Dictionary<SoundID, LoadedSoundData>();
        var tasks = new List<UniTask<LoadedSoundData>>();
        
        // Start all loads concurrently
        foreach (var soundId in soundIds)
        {
            tasks.Add(LoadSoundAsync(soundId));
        }
        
        // Wait for all to complete
        var loadedSounds = await UniTask.WhenAll(tasks);
        
        // Build results dictionary
        for (int i = 0; i < soundIds.Length; i++)
        {
            results[soundIds[i]] = loadedSounds[i];
        }
        
        return results;
    }
    
    /// <summary>
    /// Preload essential sounds (like UI sounds and background music)
    /// </summary>
    /// <returns>Number of sounds successfully preloaded</returns>
    public async UniTask<int> PreloadEssentialSoundsAsync()
    {
        if (!IsInitialized)
        {
            Debug.LogError("[SoundDataManager] Not initialized!");
            return 0;
        }
        
        var essentialSounds = new List<SoundID>();
        
        // Add commonly used sounds
        var uiSounds = soundCollection.GetSoundsByType(GameAudioType.UI);
        foreach (var sound in uiSounds)
        {
            essentialSounds.Add(sound.soundId);
        }
        
        // Add background music
        var musicSounds = soundCollection.GetSoundsByType(GameAudioType.Music);
        foreach (var sound in musicSounds)
        {
            // Check if it's background music
            if (sound.soundId == SoundID.BackgroundMusic)
            {
                essentialSounds.Add(sound.soundId);
            }
        }
        
        Debug.Log($"[SoundDataManager] Preloading {essentialSounds.Count} essential sounds...");
        
        var results = await LoadSoundsAsync(essentialSounds.ToArray());
        int successCount = 0;
        
        foreach (var result in results.Values)
        {
            if (result != null && result.IsLoaded)
            {
                successCount++;
            }
        }
        
        Debug.Log($"[SoundDataManager] Preloaded {successCount}/{essentialSounds.Count} essential sounds");
        return successCount;
    }
    
    /// <summary>
    /// Get a loaded sound by ID (returns null if not loaded)
    /// </summary>
    /// <param name="soundId">The ID of the sound</param>
    /// <returns>LoadedSoundData if loaded, null otherwise</returns>
    public LoadedSoundData GetLoadedSound(SoundID soundId)
    {
        return loadedSounds.TryGetValue(soundId, out var sound) && sound.IsLoaded ? sound : null;
    }
    
    /// <summary>
    /// Check if a sound is currently loaded
    /// </summary>
    /// <param name="soundId">The ID of the sound</param>
    /// <returns>True if loaded, false otherwise</returns>
    public bool IsSoundLoaded(SoundID soundId)
    {
        return loadedSounds.TryGetValue(soundId, out var sound) && sound.IsLoaded;
    }
    
    /// <summary>
    /// Release a loaded sound from memory
    /// </summary>
    /// <param name="soundId">The ID of the sound to release</param>
    public void ReleaseSound(SoundID soundId)
    {
        if (loadedSounds.TryGetValue(soundId, out var sound))
        {
            // Note: Your AssetManager handles the actual Addressable asset release internally
            // We just need to clear our local reference and let AssetManager manage the lifecycle
            
            sound.Release();
            Debug.Log($"[SoundDataManager] Released sound: {soundId}");
        }
    }
    
    /// <summary>
    /// Release all loaded sounds
    /// </summary>
    public void ReleaseAllSounds()
    {
        foreach (var soundId in loadedSounds.Keys.ToArray())
        {
            ReleaseSound(soundId);
        }
        
        loadedSounds.Clear();
        Debug.Log("[SoundDataManager] Released all loaded sounds");
    }
    
    /// <summary>
    /// Get statistics about loaded sounds
    /// </summary>
    /// <returns>String with loading statistics</returns>
    public string GetLoadingStats()
    {
        int totalSounds = soundCollection?.GetSoundCount() ?? 0;
        int loadedCount = loadedSounds.Values.Count(sound => sound.IsLoaded);
        int currentlyLoadingCount = currentlyLoading.Count;
        
        return $"Loaded: {loadedCount}/{totalSounds}, Currently Loading: {currentlyLoadingCount}";
    }
    
    private async UniTask<AudioClip> LoadAudioClipThroughAssetManager(SoundEntry soundEntry)
    {
        if (assetManager == null)
        {
            Debug.LogError("[SoundDataManager] AssetManager is null!");
            return null;
        }
        
        try
        {
            // Use the existing AssetManager with AssetKey from AssetReferenceCollection
            // This integrates properly with your centralized asset management system
            var audioClip = await assetManager.LoadAssetAsync<AudioClip>(soundEntry.assetKey);
            
            if (audioClip != null)
            {
                Debug.Log($"[SoundDataManager] Successfully loaded audio clip '{audioClip.name}' through AssetManager (AssetKey: {soundEntry.assetKey})");
                return audioClip;
            }
            else
            {
                Debug.LogWarning($"[SoundDataManager] AssetManager returned null for sound '{soundEntry.soundId}' with AssetKey '{soundEntry.assetKey}'");
                
                // Fallback: Try the legacy key mapping for backward compatibility
                if (TryGetAssetKeyForSound(soundEntry.soundId, out AssetKey fallbackKey) && fallbackKey != soundEntry.assetKey)
                {
                    Debug.Log($"[SoundDataManager] Trying fallback loading via legacy AssetKey: {fallbackKey}");
                    audioClip = await assetManager.LoadAssetAsync<AudioClip>(fallbackKey);
                    
                    if (audioClip != null)
                    {
                        Debug.Log($"[SoundDataManager] Successfully loaded audio clip '{audioClip.name}' through AssetManager (Legacy fallback: {fallbackKey})");
                        return audioClip;
                    }
                }
                
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SoundDataManager] Error loading audio clip '{soundEntry.soundId}' through AssetManager: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Try to get a matching AssetKey for a sound ID (for fallback loading)
    /// </summary>
    /// <param name="soundId">The sound ID</param>
    /// <param name="assetKey">The matching AssetKey if found</param>
    /// <returns>True if a matching key was found</returns>
    private bool TryGetAssetKeyForSound(SoundID soundId, out AssetKey assetKey)
    {
        // Map common sound IDs to your existing AssetKeys
        var soundKeyMappings = new Dictionary<SoundID, AssetKey>
        {
            { SoundID.BackgroundMusic, AssetKey.Audio_BackgroundMusic },
            { SoundID.WinSFX, AssetKey.Audio_WinSFX },
            { SoundID.LoseSFX, AssetKey.Audio_LoseSFX },
            { SoundID.ButtonClick, AssetKey.Audio_ButtonClick },
            { SoundID.BlockMove, AssetKey.Audio_BlockMove },
            { SoundID.BlockDestroy, AssetKey.Audio_BlockDestroy },
            // Add more mappings as needed
        };
        
        return soundKeyMappings.TryGetValue(soundId, out assetKey);
    }
}
