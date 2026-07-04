using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Purchase
{
    /// <summary>
    /// Main purchase manager for Unity IAP integration
    /// Handles initialization, purchase processing, validation, and history tracking
    /// Does NOT handle any UI - only business logic and callbacks
    /// </summary>
    public class PurchaseManager : MonoBehaviour, IStoreListener
    {
        [Header("Configuration")]
        [SerializeField] private bool enableDebugLogging = true;
        [SerializeField] private float purchaseTimeoutSeconds = 30f;
        [SerializeField] private IAPConfig iapConfig; // Reference to IAP configuration
        [SerializeField] private ShopConfig shopConfig; // Reference to shop configuration for linking
        
        // Core components
        private IStoreController storeController;
        private IExtensionProvider storeExtensionProvider;
        private PurchaseHistory purchaseHistory;
        private Dictionary<string, PurchaseProductInfo> productInfoCache;
        private Dictionary<string, PurchaseRequest> pendingPurchases;
        
        // State
        private bool isInitialized = false;
        private bool isInitializing = false;
        private CancellationTokenSource cancellationTokenSource;
        
        // Properties
        public bool IsInitialized => isInitialized;
        public bool IsInitializing => isInitializing;
        public bool IsPaid
        {
            get
            {
                if (purchaseHistory == null || iapConfig == null) return false;
                
                var nonConsumableIds = iapConfig.iapPackages
                    .Where(p => p.productType == ProductType.NonConsumable)
                    .Select(p => p.productId)
                    .ToList();
                
                var platformIds = iapConfig.iapPackages
                    .Where(p => p.productType == ProductType.NonConsumable)
                    .Select(p => p.GetPlatformProductId())
                    .ToList();
                
                return purchaseHistory.Transactions
                    .Any(t => t.IsValid && (nonConsumableIds.Contains(t.ProductId) || platformIds.Contains(t.ProductId)));
            }
        }
        public PurchaseHistory History => purchaseHistory;
        
        public event Action<Dictionary<CurrencyType, int>, string, string> LootRewardEarned;
        
        private void Awake()
        {
            // Basic initialization - no dependencies
            Initialize();
        }
        
        private void Start()
        {
            // NOTE: Don't auto-initialize here anymore
            // Let GameInitFlow control initialization
        }
        
        /// <summary>
        /// Initialize the purchase system
        /// </summary>
        private void Initialize()
        {
            // Initialize components
            purchaseHistory = new PurchaseHistory();
            productInfoCache = new Dictionary<string, PurchaseProductInfo>();
            pendingPurchases = new Dictionary<string, PurchaseRequest>();
            cancellationTokenSource = new CancellationTokenSource();
            
            // Load saved data
            LoadPurchaseHistory();
            LoadProductInfoCache();
            
            // Service will be registered by GameInitFlow
            
            if (enableDebugLogging)
            {
                Debug.Log("PurchaseManager: Initialized successfully");
            }
        }
        
        /// <summary>
        /// Initialize Unity IAP asynchronously
        /// </summary>
        public async UniTask InitializeAsync()
        {
            if (isInitialized || isInitializing) return;
            
            isInitializing = true;
            
            try
            {
                if (enableDebugLogging)
                {
                    Debug.Log("PurchaseManager: Starting IAP initialization...");
                }
                
                // Build product catalog
                var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
                var products = BuildProductCatalog();
                
                foreach (var product in products)
                {
                    builder.AddProduct(product.id, product.type);
                    if (enableDebugLogging)
                    {
                        Debug.Log($"PurchaseManager: Added product {product.id} ({product.type})");
                    }
                }
                
                // Initialize Unity Purchasing
                UnityPurchasing.Initialize(this, builder);
                
                // Wait for initialization to complete with timeout
                var timeoutTime = Time.time + purchaseTimeoutSeconds;
                while (!isInitialized && Time.time < timeoutTime && isInitializing)
                {
                    await UniTask.NextFrame(cancellationTokenSource.Token);
                }
                
                if (!isInitialized)
                {
                    throw new TimeoutException("IAP initialization timed out");
                }
            }
            catch (Exception e)
            {
                isInitializing = false;
                var errorMessage = $"Failed to initialize IAP: {e.Message}";
                
                if (enableDebugLogging)
                {
                    Debug.LogError($"PurchaseManager: {errorMessage}");
                }
                
                PurchaseEventManager.FireInitialized(
                    InitializationEventArgs.Failure(InitializationFailureReason.PurchasingUnavailable, errorMessage)
                );
            }
        }
        
        /// <summary>
        /// Build product catalog from shop configuration
        /// </summary>
        private List<ProductDefinition> BuildProductCatalog()
        {
            var products = new List<ProductDefinition>();
            
            if (iapConfig != null)
            {
                // Get all enabled IAP packages from IAP config
                var iapPackages = iapConfig.GetEnabledPackages();
                
                foreach (var package in iapPackages)
                {
                    // Convert IAPPackage to ProductDefinition
                    var productDefinition = package.ToProductDefinition();
                    if (productDefinition != null)
                    {
                        products.Add(productDefinition);
                    }
                }
                
                if (enableDebugLogging)
                {
                    Debug.Log($"[PurchaseManager] Built product catalog with {products.Count} products");
                }
            }
            else
            {
                Debug.LogError("[PurchaseManager] IAPConfig is not assigned!");
            }
            
            return products;
        }
        
        /// <summary>
        /// Purchase a product by product ID
        /// </summary>
        public void PurchaseProduct(string productId, ShopPackage shopPackage = null)
        {
            if (!isInitialized)
            {
                var errorArgs = PurchaseEventArgs.FromFailure(null, PurchaseFailureReason.PurchasingUnavailable, 
                    "Purchase system not initialized", shopPackage);
                PurchaseEventManager.FirePurchaseFailed(errorArgs);
                return;
            }
            
            var product = storeController.products.WithID(productId);
            if (product == null)
            {
                var errorArgs = PurchaseEventArgs.FromFailure(null, PurchaseFailureReason.ProductUnavailable, 
                    $"Product {productId} not found", shopPackage);
                PurchaseEventManager.FirePurchaseFailed(errorArgs);
                return;
            }
            
            if (!product.availableToPurchase)
            {
                var errorArgs = PurchaseEventArgs.FromFailure(product, PurchaseFailureReason.ProductUnavailable, 
                    $"Product {productId} not available for purchase", shopPackage);
                PurchaseEventManager.FirePurchaseFailed(errorArgs);
                return;
            }
            
            // Create purchase request
            var request = new PurchaseRequest(productId, shopPackage);
            pendingPurchases[productId] = request;
            
            // Fire purchase started event
            var startArgs = PurchaseEventArgs.FromProduct(product, shopPackage);
            PurchaseEventManager.FirePurchaseStarted(startArgs);
            
            if (enableDebugLogging)
            {
                Debug.Log($"PurchaseManager: Initiating purchase for {productId}");
            }
            
            // Start purchase
            storeController.InitiatePurchase(product);
        }
        
        /// <summary>
        /// Purchase a shop package
        /// </summary>
        public void PurchasePackage(ShopPackage shopPackage)
        {
            if (shopPackage == null)
            {
                var errorArgs = PurchaseEventArgs.FromFailure(null, PurchaseFailureReason.ProductUnavailable, 
                    "Shop package is null");
                PurchaseEventManager.FirePurchaseFailed(errorArgs);
                return;
            }
            
            if (shopPackage.purchaseType != ShopPurchaseType.IAP)
            {
                var errorArgs = PurchaseEventArgs.FromFailure(null, PurchaseFailureReason.ProductUnavailable, 
                    "Package is not an IAP product", shopPackage);
                PurchaseEventManager.FirePurchaseFailed(errorArgs);
                return;
            }
            
            if (string.IsNullOrEmpty(shopPackage.iapPackageId))
            {
                var errorArgs = PurchaseEventArgs.FromFailure(null, PurchaseFailureReason.ProductUnavailable, 
                    "Shop package has no IAP package ID", shopPackage);
                PurchaseEventManager.FirePurchaseFailed(errorArgs);
                return;
            }
            
            // Find the IAP package
            var iapPackage = iapConfig?.GetPackageById(shopPackage.iapPackageId);
            if (iapPackage == null)
            {
                var errorArgs = PurchaseEventArgs.FromFailure(null, PurchaseFailureReason.ProductUnavailable, 
                    $"IAP package '{shopPackage.iapPackageId}' not found", shopPackage);
                PurchaseEventManager.FirePurchaseFailed(errorArgs);
                return;
            }
            
            var productId = iapPackage.GetPlatformProductId();
            PurchaseProduct(productId, shopPackage);
        }
        
        /// <summary>
        /// Restore purchases (iOS only)
        /// </summary>
        public void RestorePurchases()
        {
            if (!isInitialized)
            {
                if (enableDebugLogging)
                {
                    Debug.LogWarning("PurchaseManager: Cannot restore purchases - not initialized");
                }
                return;
            }
            
#if UNITY_IOS
            var apple = storeExtensionProvider.GetExtension<IAppleExtensions>();
            apple.RestoreTransactions((success) =>
            {
                if (enableDebugLogging)
                {
                    Debug.Log($"PurchaseManager: Restore purchases {(success ? "succeeded" : "failed")}");
                }
            });
#else
            if (enableDebugLogging)
            {
                Debug.LogWarning("PurchaseManager: Restore purchases not supported on this platform");
            }
#endif
        }
        
        /// <summary>
        /// Get product information by product ID
        /// </summary>
        public PurchaseProductInfo GetProductInfo(string productId)
        {
            if (productInfoCache.ContainsKey(productId))
            {
                var info = productInfoCache[productId];
                var maxAge = TimeSpan.FromHours(24f); // Cache for 24 hours
                
                if (!info.IsStale(maxAge))
                {
                    return info;
                }
            }
            
            // Try to get from store controller
            if (isInitialized && storeController != null)
            {
                var product = storeController.products.WithID(productId);
                if (product != null)
                {
                    var info = new PurchaseProductInfo(product);
                    productInfoCache[productId] = info;
                    return info;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Get product information by IAP package ID
        /// </summary>
        public PurchaseProductInfo GetProductInfoByIAPPackageID(string iapPackageID)
        {
            if (string.IsNullOrEmpty(iapPackageID))
            {
                return null;
            }
            
            // Find the IAP package
            var iapPackage = iapConfig?.GetPackageById(iapPackageID);
            if (iapPackage == null)
            {
                if (enableDebugLogging)
                {
                    Debug.LogWarning($"PurchaseManager: IAP package '{iapPackageID}' not found");
                }
                return null;
            }
            
            // Get the platform-specific product ID and retrieve product info
            var productId = iapPackage.GetPlatformProductId();
            return GetProductInfo(productId);
        }
        
        /// <summary>
        /// Get all available products
        /// </summary>
        public PurchaseProductInfo[] GetAllProducts()
        {
            if (!isInitialized || storeController == null)
                return new PurchaseProductInfo[0];
            
            var products = new List<PurchaseProductInfo>();
            
            foreach (var product in storeController.products.all)
            {
                var info = GetProductInfo(product.definition.id);
                if (info != null)
                {
                    products.Add(info);
                }
            }
            
            return products.ToArray();
        }
        
        /// <summary>
        /// Check if a product is owned (for non-consumables)
        /// </summary>
        public bool IsProductOwned(string productId)
        {
            if (!isInitialized || storeController == null)
                return false;
            
            var product = storeController.products.WithID(productId);
            return product?.hasReceipt == true;
        }
        
        #region IStoreListener Implementation
        
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            storeController = controller;
            storeExtensionProvider = extensions;
            isInitialized = true;
            isInitializing = false;
            
            if (enableDebugLogging)
            {
                Debug.Log($"PurchaseManager: IAP initialized with {controller.products.all.Length} products");
            }
            
            // Update product info cache
            // Always refresh product info on initialization
            RefreshProductInfoCache();
            
            // Fire initialization success event
            PurchaseEventManager.FireInitialized(
                InitializationEventArgs.Success(controller.products.all.Length)
            );
        }
        
        public void OnInitializeFailed(InitializationFailureReason error)
        {
            OnInitializeFailed(error, null);
        }
        
        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            isInitialized = false;
            isInitializing = false;
            
            var errorMessage = $"IAP initialization failed: {error}";
            if (!string.IsNullOrEmpty(message))
            {
                errorMessage += $" - {message}";
            }
            
            if (enableDebugLogging)
            {
                Debug.LogError($"PurchaseManager: {errorMessage}");
            }
            
            // Fire initialization failed event
            PurchaseEventManager.FireInitialized(
                InitializationEventArgs.Failure(error, errorMessage)
            );
        }
        
        public PurchaseProcessingResult ProcessPurchase(UnityEngine.Purchasing.PurchaseEventArgs args)
        {
            var productId = args.purchasedProduct.definition.id;
            var product = args.purchasedProduct;
            
            if (product == null)
            {
                if (enableDebugLogging)
                {
                    Debug.LogError($"PurchaseManager: Product {productId} not found during processing");
                }
                return PurchaseProcessingResult.Complete;
            }
            
            // Get associated shop package
            ShopPackage shopPackage = null;
            if (pendingPurchases.ContainsKey(productId))
            {
                shopPackage = pendingPurchases[productId].ShopPackage;
            }
            
            var purchaseArgs = Purchase.PurchaseEventArgs.FromProduct(product, shopPackage);
            
            if (enableDebugLogging)
            {
                Debug.Log($"PurchaseManager: Processing purchase for {productId}");
            }
            
            // Create transaction record
            var transaction = PurchaseTransaction.FromProduct(product, shopPackage);
            purchaseHistory.AddTransaction(transaction);
            
            // Save purchase history
            SavePurchaseHistory();
            
            // Clean up pending purchase
            pendingPurchases.Remove(productId);
            
            // Process loot rewards from shop package
            if (shopPackage != null)
            {
                ProcessLootRewards(shopPackage, "IAP Purchase");
            }
            
            // Mark player as paid user (for disabling interstitial ads)
            MarkPlayerAsPaid();
            
            // Fire purchase completed event
            PurchaseEventManager.FirePurchaseCompleted(purchaseArgs);
            
            if (enableDebugLogging)
            {
                Debug.Log($"PurchaseManager: Purchase completed for {productId}");
            }
            
            // Track analytics
            TrackPurchaseAnalytics(product, shopPackage);
            
            return PurchaseProcessingResult.Complete;
        }
        
        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            var productId = product?.definition?.id ?? "unknown";
            
            // Get associated shop package
            ShopPackage shopPackage = null;
            if (pendingPurchases.ContainsKey(productId))
            {
                shopPackage = pendingPurchases[productId].ShopPackage;
                pendingPurchases.Remove(productId);
            }
            
            var errorMessage = $"Purchase failed: {failureReason}";
            var errorArgs = PurchaseEventArgs.FromFailure(product, failureReason, errorMessage, shopPackage);
            
            if (enableDebugLogging)
            {
                Debug.LogError($"PurchaseManager: {errorMessage} for product {productId}");
            }
            
            // Fire purchase failed event
            PurchaseEventManager.FirePurchaseFailed(errorArgs);
            
            // Track analytics
            TrackPurchaseFailureAnalytics(product, failureReason, shopPackage);
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Process loot rewards from a shop package and add them to player data
        /// </summary>
        private void ProcessLootRewards(ShopPackage shopPackage, string source = "Purchase")
        {
            if (shopPackage?.lootReward == null)
            {
                if (enableDebugLogging)
                {
                    Debug.Log($"PurchaseManager: No loot rewards to process for {source}");
                }
                return;
            }
            
            try
            {
                var purchaseDataProvider = ServiceLocator.TryResolve<BlockSort.Purchase.IPurchaseDataProvider>();
                if (purchaseDataProvider == null)
                {
                    Debug.LogError($"PurchaseManager: IPurchaseDataProvider not found - cannot process loot rewards for {source}");
                    return;
                }
                
                // Check if there are any currency rewards
                if (shopPackage.lootReward.currencyRewards == null || shopPackage.lootReward.currencyRewards.Count == 0)
                {
                    if (enableDebugLogging)
                    {
                        Debug.Log($"PurchaseManager: No currency rewards in loot package for {source}");
                    }
                    return;
                }
                
                // Convert LootReward to the format expected by PlayerDataManager
                var currencyRewards = new Dictionary<CurrencyType, int>();
                
                foreach (var reward in shopPackage.lootReward.currencyRewards)
                {
                    if (reward.IsValid)
                    {
                        if (currencyRewards.ContainsKey(reward.currencyType))
                        {
                            currencyRewards[reward.currencyType] += reward.amount;
                        }
                        else
                        {
                            currencyRewards[reward.currencyType] = reward.amount;
                        }
                    }
                }
                
                if (currencyRewards.Count > 0)
                {
                    // Apply the rewards using data provider
                    purchaseDataProvider.ApplyLootRewards(currencyRewards, $"{source} ({shopPackage.packageId})");
                    
                    // Fire loot reward signal to show popup
                    FireLootRewardSignal(currencyRewards, source, shopPackage.title);
                    
                    // Track loot reward analytics
                    TrackLootRewardAnalytics(shopPackage, currencyRewards, source);
                    
                    if (enableDebugLogging)
                    {
                        var rewardStrings = new List<string>();
                        foreach (var kvp in currencyRewards)
                        {
                            rewardStrings.Add($"{kvp.Key}: {kvp.Value}");
                        }
                        Debug.Log($"PurchaseManager: Applied loot rewards for {source}: {string.Join(", ", rewardStrings)}");
                    }
                }
                else
                {
                    if (enableDebugLogging)
                    {
                        Debug.Log($"PurchaseManager: No valid currency rewards to apply for {source}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"PurchaseManager: Failed to process loot rewards for {source}: {e.Message}");
            }
        }
        
        /// <summary>
        /// Mark the player as a paid user to disable interstitial ads
        /// </summary>
        private void MarkPlayerAsPaid()
        {
            try
            {
                var purchaseDataProvider = ServiceLocator.TryResolve<BlockSort.Purchase.IPurchaseDataProvider>();
                if (purchaseDataProvider != null)
                {
                    purchaseDataProvider.MarkAsPaid();
                    
                    if (enableDebugLogging)
                    {
                        Debug.Log("PurchaseManager: Player marked as paid - interstitial ads will be disabled");
                    }
                }
                else
                {
                    Debug.LogError("PurchaseManager: IPurchaseDataProvider not available - cannot mark player as paid");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"PurchaseManager: Failed to mark player as paid: {e.Message}");
            }
        }
        
        private void RefreshProductInfoCache()
        {
            if (!isInitialized || storeController == null) return;
            
            foreach (var product in storeController.products.all)
            {
                var info = new PurchaseProductInfo(product);
                productInfoCache[product.definition.id] = info;
            }
            
            SaveProductInfoCache();
            
            if (enableDebugLogging)
            {
                Debug.Log($"PurchaseManager: Updated product info cache with {productInfoCache.Count} products");
            }
        }
        
        private void TrackPurchaseAnalytics(Product product, ShopPackage shopPackage)
        {
            try
            {
                var analyticsManager = ServiceLocator.TryResolve<Analytics.AnalyticsManager>();
                if (analyticsManager != null)
                {
                    var parameters = new Dictionary<string, object>
                    {
                        ["product_id"] = product.definition.id,
                        ["price"] = (float)product.metadata.localizedPrice,
                        ["currency"] = product.metadata.isoCurrencyCode,
                        ["transaction_id"] = product.transactionID,
                    };
                    
                    if (shopPackage != null)
                    {
                        parameters["package_id"] = shopPackage.packageId;
                        parameters["package_type"] = shopPackage.purchaseType.ToString();
                    }
                    
                    analyticsManager.TrackEvent("iap_purchase_completed", parameters);
                }
            }
            catch (Exception e)
            {
                if (enableDebugLogging)
                {
                    Debug.LogError($"PurchaseManager: Failed to track purchase analytics: {e}");
                }
            }
        }
        
        private void TrackPurchaseFailureAnalytics(Product product, PurchaseFailureReason reason, ShopPackage shopPackage)
        {
            try
            {
                var analyticsManager = ServiceLocator.TryResolve<Analytics.AnalyticsManager>();
                if (analyticsManager != null)
                {
                    var parameters = new Dictionary<string, object>
                    {
                        ["product_id"] = product?.definition?.id ?? "unknown",
                        ["failure_reason"] = reason.ToString(),
                    };
                    
                    if (shopPackage != null)
                    {
                        parameters["package_id"] = shopPackage.packageId;
                    }
                    
                    analyticsManager.TrackEvent("iap_purchase_failed", parameters);
                }
            }
            catch (Exception e)
            {
                if (enableDebugLogging)
                {
                    Debug.LogError($"PurchaseManager: Failed to track purchase failure analytics: {e}");
                }
            }
        }
        
        /// <summary>
        /// Fire a loot reward signal to show the reward popup
        /// </summary>
        private void FireLootRewardSignal(Dictionary<CurrencyType, int> currencyRewards, string source, string title = null)
        {
            try
            {
                var displayTitle = !string.IsNullOrEmpty(title) ? title : "Congratulations!";
                LootRewardEarned?.Invoke(currencyRewards, source, displayTitle);
                
                if (enableDebugLogging)
                {
                    Debug.Log($"PurchaseManager: Invoked LootRewardEarned event for {source}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"PurchaseManager: Failed to fire loot reward: {e.Message}");
            }
        }
        
        private void TrackLootRewardAnalytics(ShopPackage shopPackage, Dictionary<CurrencyType, int> currencyRewards, string source)
        {
            try
            {
                var analyticsManager = ServiceLocator.TryResolve<Analytics.AnalyticsManager>();
                if (analyticsManager != null)
                {
                    var parameters = new Dictionary<string, object>
                    {
                        ["source"] = source,
                        ["package_id"] = shopPackage.packageId,
                        ["total_reward_types"] = currencyRewards.Count,
                    };
                    
                    // Add individual currency rewards to analytics
                    foreach (var kvp in currencyRewards)
                    {
                        parameters[$"reward_{kvp.Key.ToString().ToLower()}"] = kvp.Value;
                    }
                    
                    analyticsManager.TrackEvent("loot_reward_received", parameters);
                    
                    if (enableDebugLogging)
                    {
                        Debug.Log($"PurchaseManager: Tracked loot reward analytics for {source}");
                    }
                }
            }
            catch (Exception e)
            {
                if (enableDebugLogging)
                {
                    Debug.LogError($"PurchaseManager: Failed to track loot reward analytics: {e}");
                }
            }
        }
        
        private void SavePurchaseHistory()
        {
            try
            {
                var json = JsonUtility.ToJson(purchaseHistory);
                PlayerPrefs.SetString("PurchaseHistory", json);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                if (enableDebugLogging)
                {
                    Debug.LogError($"PurchaseManager: Failed to save purchase history: {e}");
                }
            }
        }
        
        private void LoadPurchaseHistory()
        {
            try
            {
                var json = PlayerPrefs.GetString("PurchaseHistory", "");
                if (!string.IsNullOrEmpty(json))
                {
                    purchaseHistory = JsonUtility.FromJson<PurchaseHistory>(json);
                }
            }
            catch (Exception e)
            {
                if (enableDebugLogging)
                {
                    Debug.LogError($"PurchaseManager: Failed to load purchase history: {e}");
                }
                purchaseHistory = new PurchaseHistory();
            }
        }
        
        private void SaveProductInfoCache()
        {
            try
            {
                var cacheData = productInfoCache.Values.ToArray();
                var json = JsonUtility.ToJson(new Serialization.SerializableArray<PurchaseProductInfo> { items = cacheData });
                PlayerPrefs.SetString("ProductInfoCache", json);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                if (enableDebugLogging)
                {
                    Debug.LogError($"PurchaseManager: Failed to save product info cache: {e}");
                }
            }
        }
        
        private void LoadProductInfoCache()
        {
            try
            {
                var json = PlayerPrefs.GetString("ProductInfoCache", "");
                if (!string.IsNullOrEmpty(json))
                {
                    var cacheData = JsonUtility.FromJson<Serialization.SerializableArray<PurchaseProductInfo>>(json);
                    if (cacheData?.items != null)
                    {
                        foreach (var info in cacheData.items)
                        {
                            productInfoCache[info.ProductId] = info;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (enableDebugLogging)
                {
                    Debug.LogError($"PurchaseManager: Failed to load product info cache: {e}");
                }
                productInfoCache.Clear();
            }
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void OnDestroy()
        {
            // Cancel any pending operations
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            
            // Save data
            SavePurchaseHistory();
            SaveProductInfoCache();
            
            // Clear events
            PurchaseEventManager.ClearAll();
            
            // Service will be unregistered by GameInitFlow if needed
            
            if (enableDebugLogging)
            {
                Debug.Log("PurchaseManager: Destroyed and cleaned up");
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // Save data when app is paused
                SavePurchaseHistory();
                SaveProductInfoCache();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                // Save data when app loses focus
                SavePurchaseHistory();
                SaveProductInfoCache();
            }
        }
        
        #endregion
    }
}