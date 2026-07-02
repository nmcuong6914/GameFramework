using System.Collections.Generic;

namespace Analytics
{
    /// <summary>
    /// Signal fired when a popup is opened for analytics tracking
    /// </summary>
    public class PopupOpenedSignal : Signal
    {
        public string PopupName { get; }
        
        public PopupOpenedSignal(string popupName)
        {
            PopupName = popupName;
        }
    }
    
    /// <summary>
    /// Signal fired when a reward video is shown for analytics tracking
    /// </summary>
    public class RewardVideoShownSignal : Signal
    {
        public string Placement { get; }
        
        public RewardVideoShownSignal(string placement)
        {
            Placement = placement;
        }
    }
    
    /// <summary>
    /// Signal fired when a shop purchase is completed
    /// </summary>
    public class ShopPurchaseCompletedSignal : Signal
    {
        public string ProductId { get; }
        public string PackageName { get; }
        public string PurchaseType { get; }
        public decimal Price { get; }
        public string CurrencyCode { get; }
        public int CoinCost { get; }
        
        public ShopPurchaseCompletedSignal(string productId, string packageName, string purchaseType, 
            decimal price, string currencyCode, int coinCost)
        {
            ProductId = productId;
            PackageName = packageName;
            PurchaseType = purchaseType;
            Price = price;
            CurrencyCode = currencyCode;
            CoinCost = coinCost;
        }
    }
    
    /// <summary>
    /// Signal fired when a shop purchase is attempted (before processing)
    /// </summary>
    public class ShopPurchaseAttemptSignal : Signal
    {
        public string ProductId { get; }
        public string PackageName { get; }
        public string PurchaseType { get; }
        
        public ShopPurchaseAttemptSignal(string productId, string packageName, string purchaseType)
        {
            ProductId = productId;
            PackageName = packageName;
            PurchaseType = purchaseType;
        }
    }
    
    /// <summary>
    /// Signal fired when a shop purchase fails
    /// </summary>
    public class ShopPurchaseFailedSignal : Signal
    {
        public string ProductId { get; }
        public string PackageName { get; }
        public string PurchaseType { get; }
        public string FailStatus { get; }
        
        public ShopPurchaseFailedSignal(string productId, string packageName, string purchaseType, string failStatus)
        {
            ProductId = productId;
            PackageName = packageName;
            PurchaseType = purchaseType;
            FailStatus = failStatus;
        }
    }
    
    /// <summary>
    /// Signal fired when currency is purchased via BuyCurrencyPopup
    /// </summary>
    public class BuyCurrencyCompletedSignal : Signal
    {
        public string CurrencyType { get; }
        public int Amount { get; }
        
        public BuyCurrencyCompletedSignal(string currencyType, int amount)
        {
            CurrencyType = currencyType;
            Amount = amount;
        }
    }
    
    /// <summary>
    /// Signal fired when player watches ad for lives
    /// </summary>
    public class WatchAdForLivesCompletedSignal : Signal
    {
        public string AdPlacement { get; }
        
        public WatchAdForLivesCompletedSignal(string adPlacement)
        {
            AdPlacement = adPlacement;
        }
    }
    
    /// <summary>
    /// Signal fired when a booster button is clicked
    /// </summary>
    public class BoosterButtonClickedSignal : Signal
    {
        public string BoosterType { get; }
        public bool HasBooster { get; }
        
        public BoosterButtonClickedSignal(string boosterType, bool hasBooster)
        {
            BoosterType = boosterType;
            HasBooster = hasBooster;
        }
    }
    
    /// <summary>
    /// Signal fired when a booster is used
    /// </summary>
    public class BoosterUsedSignal : Signal
    {
        public string BoosterType { get; }
        
        public BoosterUsedSignal(string boosterType)
        {
            BoosterType = boosterType;
        }
    }
    
    /// <summary>
    /// Signal fired when a booster mode is cancelled
    /// </summary>
    public class BoosterCancelledSignal : Signal
    {
        public string BoosterType { get; }
        
        public BoosterCancelledSignal(string boosterType)
        {
            BoosterType = boosterType;
        }
    }
}