using System;

/// <summary>
/// Enum representing different types of assets in the game.
/// This provides type safety and prevents typos when referencing asset keys.
/// </summary>
public enum AssetKey
{
    None = 0, // Default/invalid key value

    // VFX Assets 100 -> 200
    VFX_WinLevel = 100,
    VFX_BlockDestroy = 101,
    VFX_Ice_Lock_Explosion = 102,
    VFX_Particles = 103,
    VFX_BombExplode = 104,

    // UI Assets 200 -> 300
    UI_MainMenu = 200,
    UI_GameHUD = 201,
    UI_Popup = 202,
    UI_Button = 203,
    UI_Timer = 204,
    UI_TimerWarning = 205,

    // GameObject Assets 300 -> 400
    GameObject_Block = 300,
    GameObject_Obstacle = 301,
    GameObject_Floor = 302,
    GameObject_Wall_Straight = 303,
    GameObject_Wall_OuterCorner = 304,
    Key = 305,
    DynamitePrefab = 306,

    // Audio Assets 400 -> 500
    Audio_Sample = 400,
    Audio_Music = 401,
    Audio_SFX = 402,
    Audio_UI = 403,
    Audio_BackgroundMusic = 404,
    Audio_WinSFX = 405,
    Audio_LoseSFX = 406,
    Audio_ButtonClick = 407,
    Audio_BlockMove = 408,
    Audio_BlockDestroy = 409,
    Audio_BlockPickup = 410,
    Audio_BlockDrop = 411,
    Audio_PassGate = 412,
Audio_CollectReward = 413,

    // Material Assets 500 -> 600
    Material_Block = 500,
    Material_Ground = 501,
    Material_UI = 502,

    // Currency Assets 600 -> 700
    Currency_Coin = 600,
    Currency_Lives = 601,
    Currency_Booster_Hammer = 602,
    Currency_Booster_Shuffle = 603,
    Currency_Booster_Undo = 604,
    Currency_Booster_Hint = 605,
    Currency_Booster_Dynamite = 606,
    Currency_Booster_FreezeTime = 607,
    Currency_Booster_ExtraTime = 608,

    // Shop Assets 800 -> 900
    Shop_Package_Small = 800,
    Shop_Package_Medium = 801,
    Shop_Package_Large = 802,
    Shop_Package_Mega = 803,
    Shop_Section_Featured = 810,
    Shop_Section_Currency = 811,
    Shop_Section_Boosters = 812,
    Shop_Section_Lives = 813,
    Shop_Section_Bundles = 814,
    Shop_Section_TimeLimited = 815,
    Shop_Banner_Sale = 820,
    Shop_Banner_BestValue = 821,
    Shop_Banner_MostPopular = 822,
    Shop_Banner_New = 823,
    Shop_Banner_Limited = 824
}

/// <summary>
/// Helper class for asset key operations
/// </summary>
public static class AssetKeyExtensions
{
    /// <summary>
    /// Gets the category of the asset key
    /// </summary>
    public static AssetCategory GetCategory(this AssetKey key)
    {
        int value = (int)key;
        if (value >= 100 && value < 200) return AssetCategory.VFX;
        if (value >= 200 && value < 300) return AssetCategory.UI;
        if (value >= 300 && value < 400) return AssetCategory.GameObject;
        if (value >= 400 && value < 500) return AssetCategory.Audio;
        if (value >= 500 && value < 600) return AssetCategory.Material;
        if (value >= 600 && value < 700) return AssetCategory.Currency;
        if (value >= 800 && value < 900) return AssetCategory.Shop;
        return AssetCategory.Unknown;
    }
    
    /// <summary>
    /// Check if the asset key is a VFX asset
    /// </summary>
    public static bool IsVFX(this AssetKey key)
    {
        return key.GetCategory() == AssetCategory.VFX;
    }
    
    /// <summary>
    /// Check if the asset key is a UI asset
    /// </summary>
    public static bool IsUI(this AssetKey key)
    {
        return key.GetCategory() == AssetCategory.UI;
    }
    
    /// <summary>
    /// Check if the asset key is a GameObject asset
    /// </summary>
    public static bool IsGameObject(this AssetKey key)
    {
        return key.GetCategory() == AssetCategory.GameObject;
    }
    
    /// <summary>
    /// Check if the asset key is an Audio asset
    /// </summary>
    public static bool IsAudio(this AssetKey key)
    {
        return key.GetCategory() == AssetCategory.Audio;
    }
    
    /// <summary>
    /// Check if the asset key is a Material asset
    /// </summary>
    public static bool IsMaterial(this AssetKey key)
    {
        return key.GetCategory() == AssetCategory.Material;
    }
    
    /// <summary>
    /// Check if the asset key is a Currency asset
    /// </summary>
    public static bool IsCurrency(this AssetKey key)
    {
        return key.GetCategory() == AssetCategory.Currency;
    }
    
    /// <summary>
    /// Check if the asset key is a Shop asset
    /// </summary>
    public static bool IsShop(this AssetKey key)
    {
        return key.GetCategory() == AssetCategory.Shop;
    }
    
    /// <summary>
    /// Gets a user-friendly display name for the asset key
    /// </summary>
    public static string GetDisplayName(this AssetKey key)
    {
        return key.ToString().Replace("_", " ");
    }
}

/// <summary>
/// Categories for different types of assets
/// </summary>
public enum AssetCategory
{
    Unknown,
    VFX,
    UI,
    GameObject,
    Audio,
    Material,
    Currency,
    Shop
}
