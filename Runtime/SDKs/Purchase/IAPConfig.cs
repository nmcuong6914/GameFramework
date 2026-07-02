using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Purchase
{
    /// <summary>
    /// Configuration asset that holds all IAP packages
    /// Provides centralized management of all Unity IAP products
    /// </summary>
    [CreateAssetMenu(fileName = "IAPConfig", menuName = "BlockSort/Purchase/IAP Config", order = 0)]
    public class IAPConfig : ScriptableObject
    {
        [Header("IAP Configuration")]
        [Tooltip("List of all IAP packages available in the game")]
        public List<IAPPackage> iapPackages = new List<IAPPackage>();
        
        [Header("General Settings")]
        [Tooltip("Enable debug logging for the entire IAP system")]
        public bool enableGlobalDebugLogging = false;
        
        [Tooltip("Default purchase timeout (seconds)")]
        public float defaultPurchaseTimeout = 30f;
        
        [Tooltip("Enable analytics tracking")]
        public bool enableAnalytics = true;
        
        /// <summary>
        /// Get all enabled IAP packages
        /// </summary>
        public List<IAPPackage> GetEnabledPackages()
        {
            return iapPackages.Where(package => package != null && package.IsValid()).ToList();
        }
        
        /// <summary>
        /// Get IAP package by ID
        /// </summary>
        public IAPPackage GetPackageById(string packageId)
        {
            return iapPackages.FirstOrDefault(package => package != null && package.packageId == packageId);
        }
        
        /// <summary>
        /// Get IAP package by product ID
        /// </summary>
        public IAPPackage GetPackageByProductId(string productId)
        {
            return iapPackages.FirstOrDefault(package => package != null && 
                (package.productId == productId || package.GetPlatformProductId() == productId));
        }
        
        /// <summary>
        /// Check if a package ID exists
        /// </summary>
        public bool HasPackage(string packageId)
        {
            return GetPackageById(packageId) != null;
        }
        
        /// <summary>
        /// Get all unique product IDs for Unity IAP configuration
        /// </summary>
        public List<string> GetAllProductIds()
        {
            var productIds = new HashSet<string>();
            
            foreach (var package in GetEnabledPackages())
            {
                productIds.Add(package.GetPlatformProductId());
            }
            
            return productIds.ToList();
        }
        
    }
}