using System;
using UnityEngine;

/// <summary>
/// Configuration data for a currency type
/// </summary>
[Serializable]
public class CurrencyConfig
{
    [Tooltip("The type of currency (Coin, Lives, Booster types)")]
    public CurrencyType currencyType;
    [Header("Shop Integration")]
    [Tooltip("Shop package ID associated with this currency (for buy more item popup). This ID will be used to find the package in ShopConfig -> ShopSection (Currency)")]
    public string shopPackageId;

    [Tooltip("Display name shown in UI")]
    public string displayName;

    [Tooltip("Description text shown in buy currency popup")]
    public string description;

    [Tooltip("Icon asset reference - currency assets only")]
    public AssetKey iconAssetKey;

    [Tooltip("How this currency regenerates: None = no regeneration, OverTime = regenerates periodically")]
    public RegenerationType regenerationType;

    [Tooltip("Starting amount for new players")]
    public int defaultAmount;

    [Tooltip("Maximum amount that can be held (999999 recommended)")]
    public int maxAmount;

    [Tooltip("Time in seconds between regenerations (only for OverTime type)")]
    public float regenerationInterval; // In seconds

    [Tooltip("Amount to regenerate each interval (only for OverTime type)")]
    public int regenerationAmount;
}
