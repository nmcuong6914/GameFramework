using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single purchasable package in the shop
/// Contains all information needed for display, purchase validation, and reward delivery
/// ScriptableObject for easy editing and asset management
/// </summary>
[CreateAssetMenu(fileName = "ShopPackage", menuName = "BlockSort/Shop/Shop Package", order = 3)]
public class ShopPackage : ScriptableObject
{
    [Header("Package Identity")]
    [Tooltip("Unique identifier for this package")]
    public string packageId = "";
    
    [Tooltip("Display title shown in UI")]
    public string title = "";
    
    [Tooltip("Short description of the package")]
    public string description = "";
    
    [Header("Purchase Configuration")]
    [Tooltip("How this package can be purchased")]
    public ShopPurchaseType purchaseType = ShopPurchaseType.IAP;
    
    [Tooltip("IAP Package ID (links to IAPPackage configuration for IAP purchases)")]
    public string iapPackageId = "";
    
    [Tooltip("Cost in coins (for coin purchases)")]
    public int coinCost = 0;
    
    [Header("Package Appearance")]
    [Tooltip("Prefab GameObject for this package")]
    public GameObject prefab;
    
    [Header("Package Content")]
    [Tooltip("Loot rewards given when this package is purchased")]
    public LootReward lootReward = new LootReward();
    
    [Header("Availability")]
    [Tooltip("Is this package currently enabled?")]
    public bool isEnabled = true;
    
    [Tooltip("Maximum purchases per player (-1 for unlimited)")]
    public int stock = -1;
    
    [Tooltip("Minimum level required to purchase")]
    public int unlockLevel = 1;
    
    [Header("Display Configuration")]
    [Tooltip("Display order within the section (lower = first)")]
    public int displayOrder = 0;
    
    [Header("Sale Configuration")]
    [Tooltip("Is this package on sale?")]
    public bool isOnSale = false;
    
    [Tooltip("Sale percentage for display (e.g., 50 for 50% off)")]
    [Range(0, 99)]
    public int salePercentage = 0;
    
    [Tooltip("Original price text for display during sales")]
    public string originalPriceText = "";
    
    [Header("Time-Limited Settings")]
    [Tooltip("Is this a time-limited package?")]
    public bool isTimeLimited = false;
    
    [Tooltip("Start time for availability (UTC timestamp)")]
    public long startTimeUtc = 0;
    
    [Tooltip("End time for availability (UTC timestamp)")]
    public long endTimeUtc = 0;

    /// <summary>
    /// Get the current status of this package
    /// </summary>
    public ShopPackageStatus GetStatus(int playerLevel = 0, int playerPurchases = 0)
    {
        // Check if disabled
        if (!isEnabled) return ShopPackageStatus.Disabled;
        
        // Check level requirement
        if (playerLevel > 0 && playerLevel < unlockLevel) return ShopPackageStatus.Locked;
        
        // Check time limits
        if (isTimeLimited)
        {
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (currentTime < startTimeUtc || currentTime > endTimeUtc)
                return ShopPackageStatus.Expired;
        }
        
        // Check per-player purchase limit
        if (stock >= 0 && playerPurchases >= stock)
            return ShopPackageStatus.SoldOut;
        
        return ShopPackageStatus.Available;
    }
    
    /// <summary>
    /// Check if this package can be purchased
    /// </summary>
    public bool CanPurchase(int playerLevel = 0, int playerPurchases = 0)
    {
        return GetStatus(playerLevel, playerPurchases) == ShopPackageStatus.Available;
    }
    

    
    /// <summary>
    /// Get remaining time for time-limited packages
    /// </summary>
    public TimeSpan GetRemainingTime()
    {
        if (!isTimeLimited) return TimeSpan.MaxValue;
        
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var remainingSeconds = endTimeUtc - currentTime;
        
        if (remainingSeconds <= 0) return TimeSpan.Zero;
        
        return TimeSpan.FromSeconds(remainingSeconds);
    }
    
    
    /// <summary>
    /// Get sort priority (based on display order)
    /// </summary>
    public int GetSortPriority()
    {
        return displayOrder;
    }
}