using System;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Purchase
{
    /// <summary>
    /// Represents a single IAP product configuration
    /// Contains all Unity IAP-specific information needed for product setup
    /// ScriptableObject for easy editing and asset management
    /// </summary>
    [CreateAssetMenu(fileName = "IAPPackage", menuName = "BlockSort/Purchase/IAP Package", order = 1)]
    [Serializable]
    public class IAPPackage : ScriptableObject
    {
        [Header("Product Identity")]
        [Tooltip("Unique identifier for this IAP package")]
        public string packageId = "";
        
        [Tooltip("Display name for this package (for debugging)")]
        public string displayName = "";
        
        [Header("Store Configuration")]
        [Tooltip("Main product ID (used as fallback for all platforms)")]
        public string productId = "";
        
        [Tooltip("Apple App Store Product ID (if different from main product ID)")]
        public string appleProductId = "";
        
        [Tooltip("Google Play Store Product ID (if different from main product ID)")]
        public string googleProductId = "";
        
        
        [Header("IAP Settings")]
        [Tooltip("Product type for Unity IAP")]
        public ProductType productType = ProductType.Consumable;
        
        [Tooltip("Is this product currently enabled?")]
        public bool isEnabled = true;
        
        
        /// <summary>
        /// Get the platform-specific product ID
        /// </summary>
        public string GetPlatformProductId()
        {
#if UNITY_IOS
            return !string.IsNullOrEmpty(appleProductId) ? appleProductId : productId;
#elif UNITY_ANDROID
            return !string.IsNullOrEmpty(googleProductId) ? googleProductId : productId;
#elif UNITY_AMAZON
            return !string.IsNullOrEmpty(amazonProductId) ? amazonProductId : productId;
#else
            return productId;
#endif
        }
        
        /// <summary>
        /// Validate this IAP package configuration
        /// </summary>
        public bool IsValid()
        {
            // Must have package ID and product ID
            if (string.IsNullOrEmpty(packageId) || string.IsNullOrEmpty(productId))
                return false;
            
            // Must be enabled
            if (!isEnabled)
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Get display name for debugging
        /// </summary>
        public string GetDisplayName()
        {
            return !string.IsNullOrEmpty(displayName) ? displayName : packageId;
        }
        
        /// <summary>
        /// Create Unity ProductDefinition from this IAP package
        /// </summary>
        public ProductDefinition ToProductDefinition()
        {
            return new ProductDefinition(GetPlatformProductId(), productType);
        }
        
        /// <summary>
        /// Get analytics data for this product
        /// </summary>
        public System.Collections.Generic.Dictionary<string, object> GetAnalyticsData()
        {
            var data = new System.Collections.Generic.Dictionary<string, object>
            {
                ["iap_package_id"] = packageId,
                ["product_id"] = GetPlatformProductId(),
                ["product_type"] = productType.ToString()
            };
            
            return data;
        }
        
        private void OnValidate()
        {
            // Auto-generate display name if empty
            if (string.IsNullOrEmpty(displayName) && !string.IsNullOrEmpty(packageId))
            {
                displayName = packageId.Replace("_", " ").Replace("-", " ");
                // Capitalize first letter of each word
                var words = displayName.Split(' ');
                for (int i = 0; i < words.Length; i++)
                {
                    if (words[i].Length > 0)
                    {
                        words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                    }
                }
                displayName = string.Join(" ", words);
            }
            
            // Validate configuration
            if (!IsValid())
            {
                Debug.LogWarning($"IAPPackage '{packageId}' has invalid configuration", this);
            }
        }
    }
}