using UnityEngine;
using Purchase;

namespace Purchase
{
    /// <summary>
    /// Example service that demonstrates how to use PurchaseManager
    /// This shows integration patterns for other systems
    /// </summary>
    public class PurchaseServiceExample : MonoBehaviour
    {
        [Header("Example Configuration")]
        [SerializeField] private string exampleProductId = "com.example.coins_100";
        
        private PurchaseManager purchaseManager;
        
        private void Start()
        {
            // Get PurchaseManager from ServiceLocator
            purchaseManager = ServiceLocator.TryResolve<PurchaseManager>();
            
            if (purchaseManager != null)
            {
                // Subscribe to purchase events
                PurchaseEventManager.OnPurchaseCompleted += OnPurchaseCompleted;
                PurchaseEventManager.OnPurchaseFailed += OnPurchaseFailed;
                
                Debug.Log("PurchaseServiceExample: Connected to PurchaseManager");
            }
            else
            {
                Debug.LogWarning("PurchaseServiceExample: PurchaseManager not found in ServiceLocator");
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            PurchaseEventManager.OnPurchaseCompleted -= OnPurchaseCompleted;
            PurchaseEventManager.OnPurchaseFailed -= OnPurchaseFailed;
        }
        
        /// <summary>
        /// Example method showing how to initiate a purchase
        /// </summary>
        [ContextMenu("Test Purchase")]
        public void TestPurchase()
        {
            if (purchaseManager == null || !purchaseManager.IsInitialized)
            {
                Debug.LogWarning("PurchaseServiceExample: PurchaseManager not ready");
                return;
            }
            
            // Purchase by product ID
            purchaseManager.PurchaseProduct(exampleProductId);
        }
        
        /// <summary>
        /// Example method showing how to purchase a shop package
        /// </summary>
        public void PurchaseShopPackage(ShopPackage package)
        {
            if (purchaseManager == null || !purchaseManager.IsInitialized)
            {
                Debug.LogWarning("PurchaseServiceExample: PurchaseManager not ready");
                return;
            }
            
            purchaseManager.PurchasePackage(package);
        }
        
        /// <summary>
        /// Example method showing how to get product info
        /// </summary>
        public void ShowProductInfo(string productId)
        {
            if (purchaseManager == null) return;
            
            var productInfo = purchaseManager.GetProductInfo(productId);
            if (productInfo != null)
            {
                Debug.Log($"Product: {productInfo.Title} - Price: {productInfo.PriceString}");
            }
            else
            {
                Debug.Log($"Product {productId} not found or not available");
            }
        }
        
        /// <summary>
        /// Handle successful purchase
        /// </summary>
        private void OnPurchaseCompleted(PurchaseEventArgs args)
        {
            Debug.Log($"Purchase completed: {args.ProductId} for {args.Price} {args.CurrencyCode}");
            
            // Here you would typically:
            // 1. Grant the purchased items to the player
            // 2. Update UI
            // 3. Track analytics
            // 4. Show success feedback
            
            if (args.ShopPackage != null)
            {
                // Grant rewards from shop package
                var lootReward = args.ShopPackage.lootReward;
                // Process loot reward...
                
                Debug.Log($"Granted rewards from package: {args.ShopPackage.packageId}");
            }
        }
        
        /// <summary>
        /// Handle failed purchase
        /// </summary>
        private void OnPurchaseFailed(PurchaseEventArgs args)
        {
            Debug.LogError($"Purchase failed: {args.ProductId} - {args.ErrorMessage}");
            
            // Here you would typically:
            // 1. Show error message to user
            // 2. Track analytics
            // 3. Potentially retry or offer alternative
            
            // Example of showing user-friendly error messages
            string userMessage = GetUserFriendlyErrorMessage(args.FailureReason);
            Debug.Log($"User message: {userMessage}");
        }
        
        /// <summary>
        /// Convert failure reason to user-friendly message
        /// </summary>
        private string GetUserFriendlyErrorMessage(UnityEngine.Purchasing.PurchaseFailureReason? reason)
        {
            if (reason == null) return "Purchase failed. Please try again.";
            
            switch (reason.Value)
            {
                case UnityEngine.Purchasing.PurchaseFailureReason.UserCancelled:
                    return "Purchase was cancelled.";
                case UnityEngine.Purchasing.PurchaseFailureReason.PaymentDeclined:
                    return "Payment was declined. Please check your payment method.";
                case UnityEngine.Purchasing.PurchaseFailureReason.DuplicateTransaction:
                    return "This item has already been purchased.";
                case UnityEngine.Purchasing.PurchaseFailureReason.ProductUnavailable:
                    return "This item is currently unavailable.";
                case UnityEngine.Purchasing.PurchaseFailureReason.SignatureInvalid:
                    return "Purchase verification failed. Please contact support.";
                case UnityEngine.Purchasing.PurchaseFailureReason.PurchasingUnavailable:
                    return "Purchasing is not available. Please try again later.";
                case UnityEngine.Purchasing.PurchaseFailureReason.ExistingPurchasePending:
                    return "A purchase is already in progress. Please wait.";
                default:
                    return "Purchase failed. Please try again.";
            }
        }
    }
}