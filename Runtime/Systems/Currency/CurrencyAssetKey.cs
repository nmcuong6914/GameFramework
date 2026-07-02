using System;

/// <summary>
/// Simplified enum containing only currency-related AssetKeys for easy Inspector selection
/// This enum mirrors the currency values from the main AssetKey enum
/// </summary>
public enum CurrencyAssetKey
{
    Currency_Coin = 600,
    Currency_Lives = 601,
    Currency_Booster_Hammer = 602,
    Currency_Booster_Shuffle = 603,
    Currency_Booster_Undo = 604,
    Currency_Booster_Hint = 605,
    Currency_Booster_Dynamite = 606
}

/// <summary>
/// Extension methods to convert between CurrencyAssetKey and AssetKey
/// </summary>
public static class CurrencyAssetKeyExtensions
{
    /// <summary>
    /// Convert CurrencyAssetKey to AssetKey
    /// </summary>
    public static AssetKey ToAssetKey(this CurrencyAssetKey currencyAssetKey)
    {
        return (AssetKey)(int)currencyAssetKey;
    }
    
    /// <summary>
    /// Convert AssetKey to CurrencyAssetKey (if it's a currency asset)
    /// </summary>
    public static CurrencyAssetKey ToCurrencyAssetKey(this AssetKey assetKey)
    {
        int value = (int)assetKey;
        if (value >= 600 && value <= 700 && Enum.IsDefined(typeof(CurrencyAssetKey), value))
        {
            return (CurrencyAssetKey)value;
        }
        return CurrencyAssetKey.Currency_Coin; // Default fallback
    }
}
