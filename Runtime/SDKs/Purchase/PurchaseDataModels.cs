using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Purchase
{
    /// <summary>
    /// Represents a purchase transaction record
    /// Used for tracking purchase history and validation
    /// </summary>
    [Serializable]
    public class PurchaseTransaction
    {
        [SerializeField] private string productId;
        [SerializeField] private string transactionId;
        [SerializeField] private string receipt;
        [SerializeField] private float price;
        [SerializeField] private string currencyCode;
        [SerializeField] private long purchaseTimeUtc;
        [SerializeField] private bool isValid;
        [SerializeField] private bool isRestored;
        [SerializeField] private string packageId; // Link to ShopPackage
        
        // Properties
        public string ProductId => productId;
        public string TransactionId => transactionId;
        public string Receipt => receipt;
        public float Price => price;
        public string CurrencyCode => currencyCode;
        public DateTime PurchaseTime => DateTimeOffset.FromUnixTimeSeconds(purchaseTimeUtc).DateTime;
        public bool IsValid => isValid;
        public bool IsRestored => isRestored;
        public string PackageId => packageId;
        
        public PurchaseTransaction(string productId, string transactionId, string receipt, float price, 
            string currencyCode, DateTime purchaseTime, bool isValid = true, bool isRestored = false, 
            string packageId = null)
        {
            this.productId = productId;
            this.transactionId = transactionId;
            this.receipt = receipt;
            this.price = price;
            this.currencyCode = currencyCode;
            this.purchaseTimeUtc = ((DateTimeOffset)purchaseTime).ToUnixTimeSeconds();
            this.isValid = isValid;
            this.isRestored = isRestored;
            this.packageId = packageId;
        }
        
        /// <summary>
        /// Create from Unity Purchase Product
        /// </summary>
        public static PurchaseTransaction FromProduct(Product product, ShopPackage shopPackage = null, 
            bool isRestored = false)
        {
            return new PurchaseTransaction(
                product.definition.id,
                product.transactionID,
                product.receipt,
                (float)product.metadata.localizedPrice,
                product.metadata.isoCurrencyCode,
                DateTime.UtcNow,
                true,
                isRestored,
                shopPackage?.packageId
            );
        }
        
        /// <summary>
        /// Mark transaction as invalid
        /// </summary>
        public void MarkInvalid()
        {
            isValid = false;
        }
        
        /// <summary>
        /// Get formatted price string
        /// </summary>
        public string GetFormattedPrice()
        {
            return $"{price:F2} {currencyCode}";
        }
    }

    /// <summary>
    /// Product information retrieved from store
    /// Cached for quick access without store queries
    /// </summary>
    [Serializable]
    public class PurchaseProductInfo
    {
        [SerializeField] private string productId;
        [SerializeField] private string title;
        [SerializeField] private string description;
        [SerializeField] private string priceString;
        [SerializeField] private float price;
        [SerializeField] private string currencyCode;
        [SerializeField] private bool isAvailable;
        [SerializeField] private long lastUpdatedUtc;
        [SerializeField] private ProductType productType;
        
        // Properties
        public string ProductId => productId;
        public string Title => title;
        public string Description => description;
        public string PriceString => priceString;
        public float Price => price;
        public string CurrencyCode => currencyCode;
        public bool IsAvailable => isAvailable;
        public DateTime LastUpdated => DateTimeOffset.FromUnixTimeSeconds(lastUpdatedUtc).DateTime;
        public ProductType ProductType => productType;
        
        public PurchaseProductInfo(Product product)
        {
            productId = product.definition.id;
            title = product.metadata.localizedTitle;
            description = product.metadata.localizedDescription;
            priceString = product.metadata.localizedPriceString;
            price = (float)product.metadata.localizedPrice;
            currencyCode = product.metadata.isoCurrencyCode;
            isAvailable = product.availableToPurchase;
            lastUpdatedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            productType = product.definition.type;
        }
        
        /// <summary>
        /// Update from Unity Product
        /// </summary>
        public void UpdateFromProduct(Product product)
        {
            title = product.metadata.localizedTitle;
            description = product.metadata.localizedDescription;
            priceString = product.metadata.localizedPriceString;
            price = (float)product.metadata.localizedPrice;
            currencyCode = product.metadata.isoCurrencyCode;
            isAvailable = product.availableToPurchase;
            lastUpdatedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        
        /// <summary>
        /// Check if product info is stale and needs refresh
        /// </summary>
        public bool IsStale(TimeSpan maxAge)
        {
            return DateTime.UtcNow - LastUpdated > maxAge;
        }
    }

    /// <summary>
    /// Purchase history and analytics data
    /// Tracks user purchase behavior for analytics and business logic
    /// </summary>
    [Serializable]
    public class PurchaseHistory
    {
        [SerializeField] private List<PurchaseTransaction> transactions = new List<PurchaseTransaction>();
        [SerializeField] private Dictionary<string, int> productPurchaseCounts = new Dictionary<string, int>();
        [SerializeField] private float totalSpent = 0f;
        [SerializeField] private int totalPurchases = 0;
        [SerializeField] private long firstPurchaseUtc = 0;
        [SerializeField] private long lastPurchaseUtc = 0;
        
        // Properties
        public List<PurchaseTransaction> Transactions => new List<PurchaseTransaction>(transactions);
        public Dictionary<string, int> ProductPurchaseCounts => new Dictionary<string, int>(productPurchaseCounts);
        public float TotalSpent => totalSpent;
        public int TotalPurchases => totalPurchases;
        public DateTime? FirstPurchase => firstPurchaseUtc > 0 ? DateTimeOffset.FromUnixTimeSeconds(firstPurchaseUtc).DateTime : null;
        public DateTime? LastPurchase => lastPurchaseUtc > 0 ? DateTimeOffset.FromUnixTimeSeconds(lastPurchaseUtc).DateTime : null;
        
        /// <summary>
        /// Add a purchase transaction to history
        /// </summary>
        public void AddTransaction(PurchaseTransaction transaction)
        {
            if (transaction == null) return;
            
            transactions.Add(transaction);
            
            // Update counts
            if (!productPurchaseCounts.ContainsKey(transaction.ProductId))
                productPurchaseCounts[transaction.ProductId] = 0;
            productPurchaseCounts[transaction.ProductId]++;
            
            // Update totals
            totalSpent += transaction.Price;
            totalPurchases++;
            
            // Update timestamps
            var purchaseTime = ((DateTimeOffset)transaction.PurchaseTime).ToUnixTimeSeconds();
            if (firstPurchaseUtc == 0 || purchaseTime < firstPurchaseUtc)
                firstPurchaseUtc = purchaseTime;
            if (purchaseTime > lastPurchaseUtc)
                lastPurchaseUtc = purchaseTime;
        }
        
        /// <summary>
        /// Get purchase count for a specific product
        /// </summary>
        public int GetProductPurchaseCount(string productId)
        {
            return productPurchaseCounts.GetValueOrDefault(productId, 0);
        }
        
        /// <summary>
        /// Get transactions for a specific product
        /// </summary>
        public List<PurchaseTransaction> GetProductTransactions(string productId)
        {
            return transactions.FindAll(t => t.ProductId == productId);
        }
        
        /// <summary>
        /// Get transactions within a time range
        /// </summary>
        public List<PurchaseTransaction> GetTransactionsInRange(DateTime startTime, DateTime endTime)
        {
            return transactions.FindAll(t => t.PurchaseTime >= startTime && t.PurchaseTime <= endTime);
        }
        
        /// <summary>
        /// Get spending in a specific currency
        /// </summary>
        public float GetSpendingInCurrency(string currencyCode)
        {
            return transactions
                .Where(t => t.CurrencyCode == currencyCode)
                .Sum(t => t.Price);
        }
        
        /// <summary>
        /// Check if user is a paying customer
        /// </summary>
        public bool IsPayingCustomer()
        {
            return totalPurchases > 0;
        }
        
        /// <summary>
        /// Get average purchase value
        /// </summary>
        public float GetAveragePurchaseValue()
        {
            return totalPurchases > 0 ? totalSpent / totalPurchases : 0f;
        }
        
        /// <summary>
        /// Get days since first purchase
        /// </summary>
        public int GetDaysSinceFirstPurchase()
        {
            if (FirstPurchase == null) return 0;
            return (DateTime.UtcNow - FirstPurchase.Value).Days;
        }
        
        /// <summary>
        /// Get days since last purchase
        /// </summary>
        public int GetDaysSinceLastPurchase()
        {
            if (LastPurchase == null) return int.MaxValue;
            return (DateTime.UtcNow - LastPurchase.Value).Days;
        }
        
        /// <summary>
        /// Clear all history data
        /// </summary>
        public void Clear()
        {
            transactions.Clear();
            productPurchaseCounts.Clear();
            totalSpent = 0f;
            totalPurchases = 0;
            firstPurchaseUtc = 0;
            lastPurchaseUtc = 0;
        }
    }



    /// <summary>
    /// Purchase request data
    /// Contains all information needed to initiate a purchase
    /// </summary>
    [Serializable]
    public class PurchaseRequest
    {
        public string ProductId { get; set; }
        public ShopPackage ShopPackage { get; set; }
        public string UserId { get; set; }
        public Dictionary<string, object> CustomData { get; set; }
        public DateTime RequestTime { get; set; }
        
        public PurchaseRequest(string productId, ShopPackage shopPackage = null, string userId = null)
        {
            ProductId = productId;
            ShopPackage = shopPackage;
            UserId = userId;
            CustomData = new Dictionary<string, object>();
            RequestTime = DateTime.UtcNow;
        }
    }


}