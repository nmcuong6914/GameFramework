using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a section in the shop that groups related packages together
/// ScriptableObject for easy editing and asset management
/// </summary>
[CreateAssetMenu(fileName = "ShopSection", menuName = "BlockSort/Shop/Shop Section", order = 2)]
public class ShopSection : ScriptableObject
{
    [Header("Section Identity")]
    [Tooltip("Display name shown in UI")]
    public string displayName = "";
    
    [Tooltip("Type/category of this section")]
    public ShopSectionType sectionType = ShopSectionType.None;
    
    [Header("Section Appearance")]
    [Tooltip("Prefab GameObject for this section")]
    public GameObject prefabContainer;
    
    [Header("Section Settings")]
    [Tooltip("Order in which this section appears (lower = first)")]
    public int displayOrder = 0;
    
    [Tooltip("Whether this section is currently enabled")]
    public bool isEnabled = true;

    [Tooltip("Minimum level required to see this section")]
    public int unlockLevel = 1;

    [Tooltip("Whether this section is available in shop (can be used to hide sections that only serve as data containers)")]
    public bool isAvailableInShop = true;

    [Header("Packages")]
    [Tooltip("List of all packages in this section")]
    public List<ShopPackage> packages = new List<ShopPackage>();
    
    /// <summary>
    /// Check if this section is currently available
    /// </summary>
    public bool IsAvailable(int playerLevel = 0)
    {
        // Check if section is enabled
        if (!isEnabled) return false;
        
        // Check level requirement
        if (playerLevel > 0 && playerLevel < unlockLevel) return false;
        
        return true;
    }

    
    /// <summary>
    /// Clone this section
    /// </summary>
    public ShopSection Clone()
    {
        var clone = Instantiate(this);
        return clone;
    }
    
    /// <summary>
    /// Get section priority for sorting (combines display order and section type)
    /// </summary>
    public int GetSortPriority()
    {
        // Featured sections always come first
        if (sectionType == ShopSectionType.None) return displayOrder - 1000;
        
        // Time-limited sections come next
        if (sectionType == ShopSectionType.Currency) return displayOrder - 500;
        
        // Regular sections use their display order
        return displayOrder;
    }
    
    /// <summary>
    /// Initialize this section with default settings
    /// </summary>
    [ContextMenu("Initialize Default Settings")]
    public void InitializeDefaults()
    {
        // Set default properties based on section type
        switch (sectionType)
        {
            case ShopSectionType.None:
                displayName = "Featured";
                break;
            case ShopSectionType.Coins:
                displayName = "Coins";
                break;
            case ShopSectionType.Boosters:
                displayName = "Boosters";
                break;
            case ShopSectionType.Lives:
                displayName = "Lives";
                break;
            case ShopSectionType.Bundles:
                displayName = "Bundles";
                break;
            case ShopSectionType.Currency:
                displayName = "Limited Time";
                break;
        }
    }
}