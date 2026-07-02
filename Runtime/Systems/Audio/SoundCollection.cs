using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System;

/// <summary>
/// Data structure for individual sound entries
/// </summary>
[Serializable]
public class SoundEntry
{
    [Header("Sound Info")]
    public SoundID soundId;
    public GameAudioType audioType;
    
    [Header("Asset Key")]
    public AssetKey assetKey;
    
    [Header("Playback Settings")]
    [Range(0f, 1f)]
    public float volume = 1f;
    [Range(0.1f, 3f)]
    public float pitch = 1f;
    
    public SoundEntry()
    {
        soundId = SoundID.ButtonClick; // Default to a common sound
        audioType = GameAudioType.SFX;
        volume = 1f;
        pitch = 1f;
    }
    
    /// <summary>
    /// Constructor with SoundID that automatically sets the audio type
    /// </summary>
    /// <param name="id">The sound ID</param>
    public SoundEntry(SoundID id)
    {
        soundId = id;
        audioType = GameAudioType.Music; // Auto-set based on SoundID
        volume = 1f;
        pitch = 1f;
    }
}

/// <summary>
/// ScriptableObject that holds all sound references for the game
/// Uses Addressables for efficient loading and memory management
/// </summary>
[CreateAssetMenu(fileName = "SoundCollection", menuName = "Audio/Sound Collection", order = 1)]
public class SoundCollection : ScriptableObject
{
    [Header("Music")]
    [SerializeField] private List<SoundEntry> musicSounds = new List<SoundEntry>();
    
    [Header("Sound Effects")]
    [SerializeField] private List<SoundEntry> sfxSounds = new List<SoundEntry>();
    
    [Header("UI Sounds")]
    [SerializeField] private List<SoundEntry> uiSounds = new List<SoundEntry>();
    
    // Dictionary for fast lookup by sound ID
    private Dictionary<SoundID, SoundEntry> soundLookup;
    
    /// <summary>
    /// Initialize the sound collection for fast lookups
    /// </summary>
    public void Initialize()
    {   
        soundLookup = new Dictionary<SoundID, SoundEntry>();
        
        // Add all sounds to lookup dictionary
        AddSoundsToLookup(musicSounds);
        AddSoundsToLookup(sfxSounds);
        AddSoundsToLookup(uiSounds);
        
        Debug.Log($"[SoundCollection] Initialized with {soundLookup.Count} sounds");
    }
    
    private void AddSoundsToLookup(List<SoundEntry> sounds)
    {
        foreach (var sound in sounds)
        {
            if (sound.soundId == SoundID.ButtonClick && soundLookup.ContainsKey(SoundID.ButtonClick))
            {
                Debug.LogWarning($"[SoundCollection] Sound with default ID found, skipping: {sound.soundId}");
                continue;
            }
            
            if (soundLookup.ContainsKey(sound.soundId))
            {
                Debug.LogWarning($"[SoundCollection] Duplicate sound ID found: {sound.soundId}");
                continue;
            }
            
            soundLookup[sound.soundId] = sound;
        }
    }
    
    /// <summary>
    /// Get a sound entry by ID
    /// </summary>
    /// <param name="soundId">The ID of the sound to find</param>
    /// <returns>SoundEntry if found, null otherwise</returns>
    public SoundEntry GetSound(SoundID soundId)
    {
        if (soundLookup == null)
        {
            Initialize();
        }
        
        return soundLookup.TryGetValue(soundId, out var sound) ? sound : null;
    }
    
    /// <summary>
    /// Check if a sound exists in the collection
    /// </summary>
    /// <param name="soundId">The ID of the sound to check</param>
    /// <returns>True if the sound exists, false otherwise</returns>
    public bool HasSound(SoundID soundId)
    {
        if (soundLookup == null)
        {
            Initialize();
        }
        
        return soundLookup.ContainsKey(soundId);
    }
    
    /// <summary>
    /// Get all sounds of a specific type
    /// </summary>
    /// <param name="audioType">The type of audio to get</param>
    /// <returns>List of sound entries of the specified type</returns>
    public List<SoundEntry> GetSoundsByType(GameAudioType audioType)
    {
        return audioType switch
        {
            GameAudioType.Music => new List<SoundEntry>(musicSounds),
            GameAudioType.SFX => new List<SoundEntry>(sfxSounds),
            GameAudioType.UI => new List<SoundEntry>(uiSounds),
            _ => new List<SoundEntry>()
        };
    }
    
    /// <summary>
    /// Get all sound IDs in the collection
    /// </summary>
    /// <returns>Array of all sound IDs</returns>
    public SoundID[] GetAllSoundIds()
    {
        if (soundLookup == null)
        {
            Initialize();
        }
        
        var ids = new SoundID[soundLookup.Count];
        soundLookup.Keys.CopyTo(ids, 0);
        return ids;
    }
    
    /// <summary>
    /// Get total number of sounds in the collection
    /// </summary>
    /// <returns>Total count of sounds</returns>
    public int GetSoundCount()
    {
        return musicSounds.Count + sfxSounds.Count + uiSounds.Count;
    }
    
    /// <summary>
    /// Validate the sound collection for missing references or duplicate IDs
    /// </summary>
    /// <returns>True if valid, false if issues found</returns>
    public bool ValidateCollection()
    {
        bool isValid = true;
        var allIds = new HashSet<SoundID>();
        
        // Check all sound lists
        isValid &= ValidateSoundList(musicSounds, "Music", allIds);
        isValid &= ValidateSoundList(sfxSounds, "SFX", allIds);
        isValid &= ValidateSoundList(uiSounds, "UI", allIds);
        
        return isValid;
    }
    
    private bool ValidateSoundList(List<SoundEntry> sounds, string category, HashSet<SoundID> allIds)
    {
        bool isValid = true;
        
        for (int i = 0; i < sounds.Count; i++)
        {
            var sound = sounds[i];
            
            // Check for duplicate ID
            if (allIds.Contains(sound.soundId))
            {
                Debug.LogError($"[SoundCollection] Duplicate sound ID found: {sound.soundId} in {category}");
                isValid = false;
                continue;
            }
            
            allIds.Add(sound.soundId);
            
            // Check for valid asset key
            if (!sound.assetKey.IsAudio())
            {
                Debug.LogWarning($"[SoundCollection] {category} sound '{sound.soundId}' has invalid asset key '{sound.assetKey}' (not an audio asset)");
                isValid = false;
            }
        }
        
        return isValid;
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Editor-only method to add a new sound entry
    /// </summary>
    public void AddSound(SoundEntry sound)
    {
        switch (sound.audioType)
        {
            case GameAudioType.Music:
                musicSounds.Add(sound);
                break;
            case GameAudioType.SFX:
                sfxSounds.Add(sound);
                break;
            case GameAudioType.UI:
                uiSounds.Add(sound);
                break;
        }
        
        // Force reinitialization
    }
    
    /// <summary>
    /// Editor-only method to remove a sound entry
    /// </summary>
    public bool RemoveSound(SoundID soundId)
    {
        bool removed = false;
        
        removed |= musicSounds.RemoveAll(s => s.soundId == soundId) > 0;
        removed |= sfxSounds.RemoveAll(s => s.soundId == soundId) > 0;
        removed |= uiSounds.RemoveAll(s => s.soundId == soundId) > 0;
        
        return removed;
    }
    #endif
}
