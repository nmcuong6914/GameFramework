using System;

/// <summary>
/// Enum representing different shop package sizes
/// </summary>
public enum ShopPackageSize
{
    Small = 0,
    Medium = 1,
    Large = 2,
    Mega = 3
}

/// <summary>
/// Enum representing different purchase methods for shop packages
/// </summary>
public enum ShopPurchaseType
{
    IAP = 0,           // In-App Purchase (real money)
    Coin = 1,          // Purchase with in-game coins
    Ad = 2,            // Watch ad to get the package
    Free = 3           // Free package (like daily rewards)
}

/// <summary>
/// Enum representing different shop sections/categories
/// </summary>
public enum ShopSectionType
{
    None = 0,      // Featured/special offers
    Coins = 1,      // Coin packages
    Boosters = 2,      // Booster packages
    Lives = 3,         // Lives packages
    Bundles = 4,       // Mixed bundles
    Currency = 5    // Time-limited offers
}

/// <summary>
/// Represents the availability status of a shop package
/// </summary>
public enum ShopPackageStatus
{
    Available = 0,     // Package is available for purchase
    Disabled = 1,      // Package is disabled
    SoldOut = 2,       // Package is sold out (stock depleted)
    Locked = 3,        // Package is locked (level requirement not met)
    Expired = 4        // Package has expired (time-limited)
}