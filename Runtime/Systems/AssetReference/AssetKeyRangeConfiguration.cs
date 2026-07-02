using UnityEngine;

/// <summary>
/// ScriptableObject configuration for asset key ranges used in property drawers
/// This allows configurable ranges instead of hardcoded values
/// </summary>
[CreateAssetMenu(fileName = "AssetKeyRangeConfiguration", menuName = "BlockSort/Asset Key Range Configuration", order = 2)]
public class AssetKeyRangeConfiguration : ScriptableObject
{
    [System.Serializable]
    public class AssetKeyRange
    {
        [Tooltip("The category of assets")]
        public AssetCategory category;
        
        [Tooltip("Minimum value for this asset category (inclusive)")]
        public int minValue;
        
        [Tooltip("Maximum value for this asset category (inclusive)")]
        public int maxValue;
        
        [Tooltip("Display name prefix to remove (e.g., 'Currency_')")]
        public string displayPrefix = "";
        
        public AssetKeyRange(AssetCategory cat, int min, int max, string prefix = "")
        {
            category = cat;
            minValue = min;
            maxValue = max;
            displayPrefix = prefix;
        }
    }
    
    [Header("Asset Key Ranges")]
    [Tooltip("Configure ranges for different asset categories")]
    public AssetKeyRange[] assetRanges = new AssetKeyRange[]
    {
        new AssetKeyRange(AssetCategory.VFX, 100, 199, "VFX_"),
        new AssetKeyRange(AssetCategory.UI, 200, 299, "UI_"),
        new AssetKeyRange(AssetCategory.GameObject, 300, 399, "GameObject_"),
        new AssetKeyRange(AssetCategory.Audio, 400, 499, "Audio_"),
        new AssetKeyRange(AssetCategory.Material, 500, 599, "Material_"),
        new AssetKeyRange(AssetCategory.Currency, 600, 699, "Currency_")
    };
    
    /// <summary>
    /// Get the range configuration for a specific category
    /// </summary>
    public AssetKeyRange GetRangeForCategory(AssetCategory category)
    {
        foreach (var range in assetRanges)
        {
            if (range.category == category)
                return range;
        }
        return null;
    }
    
    /// <summary>
    /// Check if an asset key is within the specified category range
    /// </summary>
    public bool IsKeyInCategory(AssetKey key, AssetCategory category)
    {
        var range = GetRangeForCategory(category);
        if (range == null) return false;
        
        int keyValue = (int)key;
        return keyValue >= range.minValue && keyValue <= range.maxValue;
    }
}
