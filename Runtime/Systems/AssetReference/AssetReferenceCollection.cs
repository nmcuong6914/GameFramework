using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ScriptableObject configuration for managing asset references with type-safe enum keys.
/// Provides a centralized way to manage VFX, UI, GameObject, Audio, and Material assets.
/// </summary>
[CreateAssetMenu(fileName = "AssetReferenceCollection", menuName = "BlockSort/Asset Reference Collection")]
public class AssetReferenceCollection : ScriptableObject
{
    [System.Serializable]
    public class AssetEntry
    {
        [Tooltip("The unique key identifier for this asset")]
        public AssetKey key;
        
        [Tooltip("The addressable asset reference")]
        public AssetReference assetReference;
        
        [Tooltip("Optional description for this asset entry")]
        [TextArea(2, 4)]
        public string description;
        
        /// <summary>
        /// Gets the category of this asset entry
        /// </summary>
        public AssetCategory Category => key.GetCategory();
        
        /// <summary>
        /// Gets the display name of this asset entry
        /// </summary>
        public string DisplayName => key.GetDisplayName();
    }
    
    [Header("Asset Entries")]
    [Tooltip("List of all asset entries in this collection")]
    [SerializeField] private List<AssetEntry> assetEntries = new List<AssetEntry>();
    
    [Header("Debug Info")]
    [SerializeField, ReadOnly] private int totalEntries;
    [SerializeField, ReadOnly] private int vfxEntries;
    [SerializeField, ReadOnly] private int uiEntries;
    [SerializeField, ReadOnly] private int gameObjectEntries;
    [SerializeField, ReadOnly] private int audioEntries;
    [SerializeField, ReadOnly] private int materialEntries;
    
    // Runtime lookup dictionary for fast access
    private Dictionary<AssetKey, AssetEntry> assetLookup;
    
    #region Unity Callbacks
    
    void OnEnable()
    {
        BuildLookup();
        UpdateDebugInfo();
    }
    
    void OnValidate()
    {
        BuildLookup();
        UpdateDebugInfo();
        ValidateEntries();
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Gets an asset entry by its key
    /// </summary>
    /// <param name="key">The asset key to look up</param>
    /// <param name="entry">The found asset entry (if any)</param>
    /// <returns>True if the entry was found, false otherwise</returns>
    public bool TryGetAssetEntry(AssetKey key, out AssetEntry entry)
    {
        if (assetLookup == null) BuildLookup();
        return assetLookup.TryGetValue(key, out entry);
    }
    
    /// <summary>
    /// Gets an asset reference by its key
    /// </summary>
    /// <param name="key">The asset key to look up</param>
    /// <returns>The asset reference, or null if not found</returns>
    public AssetReference GetAssetReference(AssetKey key)
    {
        if (TryGetAssetEntry(key, out var entry))
            return entry.assetReference;
        return null;
    }
    
    /// <summary>
    /// Gets all asset entries for a specific category
    /// </summary>
    /// <param name="category">The category to filter by</param>
    /// <returns>List of asset entries in the specified category</returns>
    public List<AssetEntry> GetAssetsByCategory(AssetCategory category)
    {
        if (assetLookup == null) BuildLookup();
        return assetEntries.Where(entry => entry.Category == category).ToList();
    }
    
    /// <summary>
    /// Gets all VFX asset entries
    /// </summary>
    public List<AssetEntry> GetVFXAssets() => GetAssetsByCategory(AssetCategory.VFX);
    
    /// <summary>
    /// Gets all UI asset entries
    /// </summary>
    public List<AssetEntry> GetUIAssets() => GetAssetsByCategory(AssetCategory.UI);
    
    /// <summary>
    /// Gets all GameObject asset entries
    /// </summary>
    public List<AssetEntry> GetGameObjectAssets() => GetAssetsByCategory(AssetCategory.GameObject);
    
    /// <summary>
    /// Gets all Audio asset entries
    /// </summary>
    public List<AssetEntry> GetAudioAssets() => GetAssetsByCategory(AssetCategory.Audio);
    
    /// <summary>
    /// Gets all Material asset entries
    /// </summary>
    public List<AssetEntry> GetMaterialAssets() => GetAssetsByCategory(AssetCategory.Material);
    
    /// <summary>
    /// Gets all asset keys in this collection
    /// </summary>
    public IEnumerable<AssetKey> GetAllAssetKeys()
    {
        if (assetLookup == null) BuildLookup();
        return assetLookup.Keys;
    }
    
    /// <summary>
    /// Checks if an asset key exists in this collection
    /// </summary>
    public bool ContainsAssetKey(AssetKey key)
    {
        if (assetLookup == null) BuildLookup();
        return assetLookup.ContainsKey(key);
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Builds the runtime lookup dictionary for fast access
    /// </summary>
    private void BuildLookup()
    {
        assetLookup = assetEntries?.ToDictionary(entry => entry.key, entry => entry) ?? new Dictionary<AssetKey, AssetEntry>();
    }
    
    /// <summary>
    /// Updates debug information shown in the inspector
    /// </summary>
    private void UpdateDebugInfo()
    {
        if (assetEntries == null) return;
        
        totalEntries = assetEntries.Count;
        vfxEntries = assetEntries.Count(e => e.Category == AssetCategory.VFX);
        uiEntries = assetEntries.Count(e => e.Category == AssetCategory.UI);
        gameObjectEntries = assetEntries.Count(e => e.Category == AssetCategory.GameObject);
        audioEntries = assetEntries.Count(e => e.Category == AssetCategory.Audio);
        materialEntries = assetEntries.Count(e => e.Category == AssetCategory.Material);
    }
    
    /// <summary>
    /// Validates entries and logs any issues
    /// </summary>
    private void ValidateEntries()
    {
        if (assetEntries == null) return;
        
        // Check for duplicate keys
        var duplicateKeys = assetEntries.GroupBy(e => e.key)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
        
        foreach (var key in duplicateKeys)
        {
            Debug.LogWarning($"Duplicate asset key found: {key}", this);
        }
        
        // Check for missing asset references
        var entriesWithMissingRefs = assetEntries.Where(e => e.assetReference == null || !e.assetReference.RuntimeKeyIsValid());
        foreach (var entry in entriesWithMissingRefs)
        {
            Debug.LogWarning($"Asset entry '{entry.key}' has missing or invalid asset reference", this);
        }
    }
    
    #endregion
    
    #region Editor Helpers
    
#if UNITY_EDITOR
    /// <summary>
    /// Adds a new asset entry (Editor only)
    /// </summary>
    public void AddAssetEntry(AssetKey key, AssetReference assetReference, string description = "")
    {
        if (ContainsAssetKey(key))
        {
            Debug.LogWarning($"Asset key '{key}' already exists in collection", this);
            return;
        }
        
        var newEntry = new AssetEntry
        {
            key = key,
            assetReference = assetReference,
            description = description
        };
        
        assetEntries.Add(newEntry);
        BuildLookup();
        UpdateDebugInfo();
        
        UnityEditor.EditorUtility.SetDirty(this);
    }
    
    /// <summary>
    /// Removes an asset entry (Editor only)
    /// </summary>
    public bool RemoveAssetEntry(AssetKey key)
    {
        var entry = assetEntries.FirstOrDefault(e => e.key == key);
        if (entry != null)
        {
            assetEntries.Remove(entry);
            BuildLookup();
            UpdateDebugInfo();
            UnityEditor.EditorUtility.SetDirty(this);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Sorts entries by category and then by key
    /// </summary>
    [UnityEditor.MenuItem("CONTEXT/AssetReferenceCollection/Sort Entries")]
    private void SortEntries()
    {
        if (assetEntries == null) return;
        
        assetEntries.Sort((a, b) =>
        {
            int categoryComparison = a.Category.CompareTo(b.Category);
            return categoryComparison != 0 ? categoryComparison : a.key.CompareTo(b.key);
        });
        
        BuildLookup();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
    
    #endregion
}

/// <summary>
/// Custom attribute to make fields read-only in the inspector
/// </summary>
public class ReadOnlyAttribute : PropertyAttribute
{
}

#if UNITY_EDITOR
/// <summary>
/// Custom property drawer for read-only fields
/// </summary>
[UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
{
    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        UnityEditor.EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
}
#endif
