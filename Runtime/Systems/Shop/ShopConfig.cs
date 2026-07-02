using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ScriptableObject that holds all shop configuration data
/// Manages shop sections, packages, and provides query methods for the shop system
/// </summary>
[CreateAssetMenu(fileName = "ShopConfig", menuName = "BlockSort/Shop/Shop Config", order = 1)]
public class ShopConfig : ScriptableObject
{
    [Header("Shop Configuration")]
    [Tooltip("List of all shop sections/categories as ScriptableObject references")]
    public List<ShopSection> shopSections = new List<ShopSection>();
    
    [Header("General Settings")]
    [Tooltip("Is the shop system enabled?")]
    public bool shopEnabled = true;
    
    [Tooltip("Minimum level required to access shop")]
    public int shopUnlockLevel = 1;
    
    [Tooltip("Default section to show when shop opens")]
    public ShopSectionType defaultSectionType = ShopSectionType.None;
    
    
    /// <summary>
    /// Get all available sections for a specific player level
    /// </summary>
    public List<ShopSection> GetAvailableSections(int playerLevel = 0)
    {
        return shopSections
            .Where(section => section.IsAvailable(playerLevel) && section.isAvailableInShop)
            .OrderBy(section => section.GetSortPriority())
            .ToList();
    }
    
    /// <summary>
    /// Get a specific section by type
    /// </summary>
    public ShopSection GetSection(ShopSectionType sectionType)
    {
        return shopSections.FirstOrDefault(s => s.sectionType == sectionType);
    }
    
    /// <summary>
    /// Get all packages in a specific section
    /// </summary>
    public List<ShopPackage> GetPackagesInSection(ShopSectionType sectionType, int playerLevel = 0)
    {
        var section = GetSection(sectionType);
        if (section == null || section.packages == null)
        {
            return new List<ShopPackage>();
        }
        
        return section.packages
            .Where(package => package != null && 
                            package.isEnabled &&
                            package.GetStatus(playerLevel) != ShopPackageStatus.Disabled)
            .OrderBy(package => package.GetSortPriority())
            .ToList();
    }
    
    /// <summary>
    /// Get all available packages for a specific player level
    /// </summary>
    public List<ShopPackage> GetAvailablePackages(int playerLevel = 0)
    {
        var allPackages = new List<ShopPackage>();
        
        // Collect packages from all sections
        foreach (var section in shopSections)
        {
            if (section != null && section.packages != null)
            {
                allPackages.AddRange(section.packages.Where(p => p != null));
            }
        }
        
        return allPackages
            .Where(package => package.isEnabled && package.CanPurchase(playerLevel))
            .OrderBy(package => package.GetSortPriority())
            .ToList();
    }
    
    /// <summary>
    /// Get a specific package by ID
    /// </summary>
    public ShopPackage GetPackage(string packageId)
    {
        // Search through all sections for the package
        foreach (var section in shopSections)
        {
            if (section != null && section.packages != null)
            {
                var package = section.packages.FirstOrDefault(p => p != null && p.packageId == packageId);
                if (package != null)
                {
                    return package;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Get a specific package by ID from a specific section type
    /// </summary>
    public ShopPackage GetPackageFromSection(string packageId, ShopSectionType sectionType)
    {
        var section = GetSection(sectionType);
        if (section == null || section.packages == null)
        {
            return null;
        }
        
        return section.packages.FirstOrDefault(p => p != null && p.packageId == packageId);
    }
    
    /// <summary>
    /// Get packages by purchase type
    /// </summary>
    public List<ShopPackage> GetPackagesByType(ShopPurchaseType purchaseType, int playerLevel = 0)
    {
        var allPackages = new List<ShopPackage>();
        
        // Collect packages from all sections
        foreach (var section in shopSections)
        {
            if (section != null && section.packages != null)
            {
                allPackages.AddRange(section.packages.Where(p => p != null));
            }
        }
        
        return allPackages
            .Where(package => package.purchaseType == purchaseType && 
                            package.isEnabled &&
                            package.CanPurchase(playerLevel))
            .OrderBy(package => package.GetSortPriority())
            .ToList();
    }
    
    /// <summary>
    /// Get all packages (used by ShopPopup)
    /// </summary>
    public List<ShopPackage> GetAllPackages()
    {
        var allPackages = new List<ShopPackage>();
        
        // Collect packages from all sections
        foreach (var section in shopSections)
        {
            if (section != null && section.packages != null)
            {
                allPackages.AddRange(section.packages.Where(p => p != null));
            }
        }
        
        return allPackages;
    }
    
    
    /// <summary>
    /// Initialize with default configuration
    /// </summary>
    [ContextMenu("Initialize Default Configuration")]
    public void InitializeDefaultConfiguration()
    {
        // Clear existing data
        shopSections.Clear();
        
        // Create default sections
        CreateDefaultSections();
        
        Debug.Log("ShopConfig initialized with default configuration!");
    }
    
    private void CreateDefaultSections()
    {
        // Note: Since ShopSection is now a ScriptableObject, you should create these as separate assets
        // This method is kept for reference but won't create actual sections
        // To create sections, use: Create > BlockSort > Shop > Shop Section in Unity
        
        Debug.LogWarning("ShopConfig: Sections are now ScriptableObjects. Create them manually using Create > BlockSort > Shop > Shop Section");
        Debug.LogWarning("Then assign them to the shopSections list in this ShopConfig.");
        Debug.LogWarning("Packages should be created and assigned to each section's packages list.");
        
        // Clear the list since we can't create ScriptableObject instances at runtime like this
        shopSections.Clear();
        
        // Set default values
        defaultSectionType = ShopSectionType.None;
        shopUnlockLevel = 1;
        shopEnabled = true;
    }
}

/// <summary>
/// Statistics about the shop configuration
/// </summary>
[Serializable]
public class ShopStatistics
{
    public int totalSections;
    public int enabledSections;
    public int totalPackages;
    public int enabledPackages;
    public int iapPackages;
    public int coinPackages;
    public int featuredPackages;
    public int salePackages;
    public int timeLimitedPackages;
}