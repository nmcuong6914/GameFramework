using System;
using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace Purchase
{
    /// <summary>
    /// Event arguments for purchase-related events
    /// Contains all necessary information about purchase transactions
    /// </summary>
    [Serializable]
    public class PurchaseEventArgs : EventArgs
    {
        public string ProductId { get; set; }
        public string TransactionId { get; set; }
        public string Receipt { get; set; }
        public decimal Price { get; set; }
        public string CurrencyCode { get; set; }
        public DateTime PurchaseTime { get; set; }
        public PurchaseFailureReason? FailureReason { get; set; }
        public string ErrorMessage { get; set; }
        public ShopPackage ShopPackage { get; set; }
        
        public static PurchaseEventArgs FromProduct(Product product, ShopPackage shopPackage = null)
        {
            return new PurchaseEventArgs
            {
                ProductId = product.definition.id,
                TransactionId = product.transactionID,
                Receipt = product.receipt,
                Price = product.metadata.localizedPrice,
                CurrencyCode = product.metadata.isoCurrencyCode,
                PurchaseTime = DateTime.UtcNow,
                ShopPackage = shopPackage
            };
        }
        
        public static PurchaseEventArgs FromFailure(Product product, PurchaseFailureReason reason, string errorMessage = null, ShopPackage shopPackage = null)
        {
            return new PurchaseEventArgs
            {
                ProductId = product?.definition?.id,
                FailureReason = reason,
                ErrorMessage = errorMessage,
                PurchaseTime = DateTime.UtcNow,
                ShopPackage = shopPackage
            };
        }
    }

    /// <summary>
    /// Event arguments for initialization events
    /// </summary>
    [Serializable]
    public class InitializationEventArgs : EventArgs
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public InitializationFailureReason? FailureReason { get; set; }
        public int AvailableProductCount { get; set; }
        
        public static InitializationEventArgs Success(int availableProductCount)
        {
            return new InitializationEventArgs
            {
                IsSuccess = true,
                AvailableProductCount = availableProductCount
            };
        }
        
        public static InitializationEventArgs Failure(InitializationFailureReason reason, string errorMessage = null)
        {
            return new InitializationEventArgs
            {
                IsSuccess = false,
                FailureReason = reason,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Event arguments for product fetch events
    /// </summary>
    [Serializable]
    public class ProductFetchEventArgs : EventArgs
    {
        public Product[] Products { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsSuccess { get; set; }
        
        public static ProductFetchEventArgs Success(Product[] products)
        {
            return new ProductFetchEventArgs
            {
                Products = products,
                IsSuccess = true
            };
        }
        
        public static ProductFetchEventArgs Failure(string errorMessage)
        {
            return new ProductFetchEventArgs
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Interface for purchase event handlers
    /// Implement this to receive purchase lifecycle events
    /// </summary>
    public interface IPurchaseEventHandler
    {
        void OnPurchaseInitialized(InitializationEventArgs args);
        void OnPurchaseStarted(PurchaseEventArgs args);

        void OnPurchaseCompleted(PurchaseEventArgs args);
        void OnPurchaseFailed(PurchaseEventArgs args);
        void OnPurchaseRestored(PurchaseEventArgs args);
        void OnProductsFetched(ProductFetchEventArgs args);
    }

    /// <summary>
    /// Centralized event manager for purchase system
    /// Provides type-safe events and handler management
    /// </summary>
    public static class PurchaseEventManager
    {
        // Purchase lifecycle events
        public static event Action<InitializationEventArgs> OnPurchaseSystemInitialized;
        public static event Action<PurchaseEventArgs> OnPurchaseStarted;

        public static event Action<PurchaseEventArgs> OnPurchaseCompleted;
        public static event Action<PurchaseEventArgs> OnPurchaseFailed;
        public static event Action<PurchaseEventArgs> OnPurchaseRestored;
        public static event Action<ProductFetchEventArgs> OnProductsFetched;
        
        // Event handler management
        private static readonly HashSet<IPurchaseEventHandler> handlers = new HashSet<IPurchaseEventHandler>();
        
        /// <summary>
        /// Register an event handler to receive all purchase events
        /// </summary>
        public static void RegisterHandler(IPurchaseEventHandler handler)
        {
            if (handler == null) return;
            handlers.Add(handler);
        }
        
        /// <summary>
        /// Unregister an event handler
        /// </summary>
        public static void UnregisterHandler(IPurchaseEventHandler handler)
        {
            if (handler == null) return;
            handlers.Remove(handler);
        }
        
        /// <summary>
        /// Fire initialization event
        /// </summary>
        public static void FireInitialized(InitializationEventArgs args)
        {
            OnPurchaseSystemInitialized?.Invoke(args);
            foreach (var handler in handlers)
            {
                try
                {
                    handler.OnPurchaseInitialized(args);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"PurchaseEventManager: Error in handler {handler.GetType().Name}: {e}");
                }
            }
        }
        
        /// <summary>
        /// Fire purchase started event
        /// </summary>
        public static void FirePurchaseStarted(PurchaseEventArgs args)
        {
            OnPurchaseStarted?.Invoke(args);
            foreach (var handler in handlers)
            {
                try
                {
                    handler.OnPurchaseStarted(args);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"PurchaseEventManager: Error in handler {handler.GetType().Name}: {e}");
                }
            }
        }
        

        
        /// <summary>
        /// Fire purchase completed event
        /// </summary>
        public static void FirePurchaseCompleted(PurchaseEventArgs args)
        {
            OnPurchaseCompleted?.Invoke(args);
            foreach (var handler in handlers)
            {
                try
                {
                    handler.OnPurchaseCompleted(args);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"PurchaseEventManager: Error in handler {handler.GetType().Name}: {e}");
                }
            }
        }
        
        /// <summary>
        /// Fire purchase failed event
        /// </summary>
        public static void FirePurchaseFailed(PurchaseEventArgs args)
        {
            OnPurchaseFailed?.Invoke(args);
            foreach (var handler in handlers)
            {
                try
                {
                    handler.OnPurchaseFailed(args);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"PurchaseEventManager: Error in handler {handler.GetType().Name}: {e}");
                }
            }
        }
        
        /// <summary>
        /// Fire purchase restored event
        /// </summary>
        public static void FirePurchaseRestored(PurchaseEventArgs args)
        {
            OnPurchaseRestored?.Invoke(args);
            foreach (var handler in handlers)
            {
                try
                {
                    handler.OnPurchaseRestored(args);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"PurchaseEventManager: Error in handler {handler.GetType().Name}: {e}");
                }
            }
        }
        
        /// <summary>
        /// Fire products fetched event
        /// </summary>
        public static void FireProductsFetched(ProductFetchEventArgs args)
        {
            OnProductsFetched?.Invoke(args);
            foreach (var handler in handlers)
            {
                try
                {
                    handler.OnProductsFetched(args);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"PurchaseEventManager: Error in handler {handler.GetType().Name}: {e}");
                }
            }
        }
        
        /// <summary>
        /// Clear all event subscriptions and handlers
        /// </summary>
        public static void ClearAll()
        {
            OnPurchaseSystemInitialized = null;
            OnPurchaseStarted = null;

            OnPurchaseCompleted = null;
            OnPurchaseFailed = null;
            OnPurchaseRestored = null;
            OnProductsFetched = null;
            handlers.Clear();
        }
    }
}