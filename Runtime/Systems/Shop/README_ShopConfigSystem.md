# Shop Config System Documentation

## Overview

The Shop Config System provides a comprehensive data structure for managing in-game shop packages, sections, and purchases. It supports both IAP (In-App Purchases) and coin-based purchases, with flexible loot rewards, sale systems, and time-limited offers.

## Core Components

### 1. ShopConfig (Main ScriptableObject)
- **File**: `ShopConfig.cs`
- **Purpose**: Central configuration holding all shop data
- **Features**:
  - Manages shop sections and packages
  - Provides query methods for filtering packages
  - Validation and initialization helpers
  - Auto-refresh functionality

### 2. ShopPackage (Individual Items)
- **File**: `ShopPackage.cs`
- **Purpose**: Represents a single purchasable item (ScriptableObject)
- **Key Properties**:
  - Unique ID and display information
  - Purchase type (IAP, Coin, Ad, Free)
  - Platform-specific IAP IDs (Apple/Google)
  - Loot rewards (currency bundles)
  - Stock management and purchase limits
  - Sale percentage (visual only)
  - Time-limited availability
  - Special badges (Featured, Best Value, etc.)
  - GameObject prefab reference

### 3. ShopSection (Categories)
- **File**: `ShopSection.cs`
- **Purpose**: Organizes packages into categories (ScriptableObject)
- **Types**: Featured, Currency, Boosters, Lives, Bundles, Time-Limited
- **Features**:
  - Level-based unlocking
  - Time-limited sections
  - GameObject prefab reference for visual assets
  - Notification badges
  - Individual ScriptableObject assets for easy management

### 4. LootReward System
- **File**: `LootReward.cs`
- **Purpose**: Defines what players receive from packages
- **Features**:
  - Multiple currency types in one package
  - Value calculation for sorting
  - Display text generation
  - Validation helpers

### 5. Enums and Types
- **File**: `ShopPackageType.cs`
- **Defines**:
  - `ShopPackageSize`: Small, Medium, Large, Mega
  - `ShopPurchaseType`: IAP, Coin, Ad, Free
  - `ShopSectionType`: Featured, Currency, Boosters, etc.
  - `ShopPackageStatus`: Available, Disabled, SoldOut, etc.

### 6. AssetKey Integration
- **File**: `AssetKey.cs` (updated)
- **New Range**: Shop Assets 800-900
- **Includes**: Package prefabs, section prefabs, badge prefabs

## Key Features

### Purchase Types
1. **IAP (In-App Purchase)**: Real money purchases
   - Separate Apple and Google Play product IDs
   - Platform-specific ID resolution
2. **Coin Purchase**: Use in-game currency
3. **Ad Watch**: Watch advertisement for reward
4. **Free**: No cost (daily rewards, etc.)

### Package Properties
- **ID System**: Unique identifiers for all packages
- **Title & Description**: Display text for UI
- **GameObject Prefabs**: Direct GameObject prefab references
- **Size Categories**: Small, Medium, Large, Mega
- **Stock Management**: Limited quantity support
- **Purchase Limits**: Per-player purchase restrictions
- **Level Requirements**: Unlock at specific levels
- **Section References**: Packages reference section IDs

### Sale System (Visual Only)
- **Sale Percentage**: Display discount (e.g., "50% OFF")
- **Original Price Text**: Show crossed-out original price
- **Visual indicator only**: Doesn't affect actual IAP pricing

### Time-Limited Offers
- **Package Level**: Individual packages can be time-limited
- **Section Level**: Entire sections can be time-limited
- **UTC Timestamps**: Start and end times
- **Remaining Time Display**: Formatted countdown strings

### Special Badges
- **Featured**: Highlighted packages
- **Best Value**: Value recommendation
- **Most Popular**: Popularity indicator
- **Custom Badge**: Any custom text (e.g., "NEW", "LIMITED")

### Loot Rewards
- **Multi-Currency**: One package can contain multiple currency types
- **Flexible Amounts**: Any amount of each currency
- **Value Calculation**: Total package value for sorting
- **Display Generation**: Automatic UI text generation

## Usage Examples

### Creating Shop Packages and Sections

Since ShopPackage and ShopSection are now ScriptableObjects, you create them using Unity's Create menu:

**For Shop Sections:**
1. Right-click in Project window
2. Create > BlockSort > Shop > Shop Section
3. Configure the section properties in Inspector

**For Shop Packages:**
1. Right-click in Project window
2. Create > BlockSort > Shop > Shop Package
3. Configure the package properties in Inspector

**For Shop Config:**
1. Right-click in Project window
2. Create > BlockSort > Shop > Shop Config
3. Assign your ShopSection and ShopPackage assets to the lists

### Creating a Bundle Package

```csharp
var starterBundle = new ShopPackage
{
    packageId = "starter_bundle",
    title = "Starter Bundle",
    description = "Perfect for new players!",
    purchaseType = ShopPurchaseType.IAP,
    iapProductId = "com.mygame.starter_bundle",
    packageSize = ShopPackageSize.Large,
    sectionId = "featured",
    displayOrder = 0,
    isFeatured = true,
    isBestValue = true,
    isOnSale = true,
    salePercentage = 50,
    originalPriceText = "$9.99",
    badgeText = "BEST DEAL",
    lootReward = new LootReward
    {
        rewardName = "Starter Bundle",
        currencyRewards = new List<CurrencyReward>
        {
            new CurrencyReward(CurrencyType.Coin, 2000),
            new CurrencyReward(CurrencyType.Lives, 10),
            new CurrencyReward(CurrencyType.BoosterHammer, 5),
            new CurrencyReward(CurrencyType.BoosterShuffle, 3)
        }
    }
};
```

### Creating a Coin Purchase Package

```csharp
var boosterPack = new ShopPackage
{
    packageId = "booster_pack_coins",
    title = "Booster Pack",
    description = "Mixed boosters for tough levels",
    purchaseType = ShopPurchaseType.Coin,
    coinCost = 500,
    packageSize = ShopPackageSize.Small,
    sectionId = "boosters",
    displayOrder = 2,
    lootReward = new LootReward
    {
        rewardName = "Mixed Boosters",
        currencyRewards = new List<CurrencyReward>
        {
            new CurrencyReward(CurrencyType.BoosterHammer, 2),
            new CurrencyReward(CurrencyType.BoosterShuffle, 2),
            new CurrencyReward(CurrencyType.BoosterUndo, 2)
        }
    }
};
```

### Creating a Time-Limited Package

```csharp
var weekendSpecial = new ShopPackage
{
    packageId = "weekend_special",
    title = "Weekend Special",
    description = "Limited time offer!",
    purchaseType = ShopPurchaseType.IAP,
    iapProductId = "com.mygame.weekend_special",
    packageSize = ShopPackageSize.Medium,
    sectionId = "featured",
    displayOrder = 0,
    isFeatured = true,
    isTimeLimited = true,
    badgeText = "LIMITED",
    // Set time limits (would normally use actual dates)
    startTimeUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
    endTimeUtc = DateTimeOffset.UtcNow.AddDays(3).ToUnixTimeSeconds()
};
```

## Query Methods

### Get Available Packages for Player
```csharp
var shopConfig = // Load your ShopConfig asset
int playerLevel = 25;

// Get all available packages for this player level
var availablePackages = shopConfig.GetAvailablePackages(playerLevel);

// Get packages in specific section
var currencyPackages = shopConfig.GetPackagesInSection("currency", playerLevel);

// Get featured packages only
var featuredPackages = shopConfig.GetFeaturedPackages(playerLevel);

// Get packages on sale
var salePackages = shopConfig.GetSalePackages(playerLevel);

// Get IAP packages only
var iapPackages = shopConfig.GetPackagesByType(ShopPurchaseType.IAP, playerLevel);
```

### Get Sections for Player
```csharp
// Get all available sections for player level
var availableSections = shopConfig.GetAvailableSections(playerLevel);

// Get specific section
var featuredSection = shopConfig.GetSection("featured");
```

## Validation and Maintenance

### Auto-Validation
The ShopConfig automatically validates itself in the Unity Inspector:
- Checks for duplicate IDs
- Validates package configurations
- Ensures section references exist
- Reports configuration errors

### Initialization
Use the context menu "Initialize Default Configuration" to populate with example data.

### Statistics
```csharp
var stats = shopConfig.GetStatistics();
Debug.Log($"Total packages: {stats.totalPackages}");
Debug.Log($"IAP packages: {stats.iapPackages}");
Debug.Log($"Featured packages: {stats.featuredPackages}");
```

## Integration Points

### Currency System
- Uses existing `CurrencyType` enum
- Integrates with `CurrencyConfig` for display information
- Rewards delivered through currency management system

### Asset System
- Uses `AssetKey` for shop asset references (new range 800-900)
- Direct GameObject prefab references for sections and packages
- Integrates with existing asset management

### ScriptableObject Management
- ShopSection as ScriptableObjects for easy editing
- ShopPackage as ScriptableObjects for modular package management
- Create via Unity menu: Create > BlockSort > Shop > [Section/Package]
- Reference by dragging assets into ShopConfig lists

### Analytics
- Each package has optional `analyticsId` for tracking
- Supports purchase analytics and conversion tracking

## Best Practices

1. **Unique IDs**: Always use unique, descriptive package and section IDs
2. **ScriptableObject Organization**: Create sections and packages as separate ScriptableObject assets
3. **Prefab References**: Assign GameObject prefabs directly to sections and packages
4. **Logical Sections**: Group related packages together
5. **Progressive Pricing**: Use different package sizes for varied price points
6. **Clear Descriptions**: Write clear, enticing package descriptions
7. **Visual Consistency**: Use consistent prefabs and colors per section
8. **Time Limits**: Use sparingly for special events and urgency
9. **Stock Management**: Use for limited-edition items only
10. **Level Gating**: Unlock more expensive packages at higher levels
11. **Asset Organization**: Keep shop assets organized in project folders

## Performance Considerations

- All data is loaded from ScriptableObject (efficient)
- Query methods use LINQ (consider caching for heavy usage)
- Time calculations are lightweight
- Package validation is editor-only

## Future Extensibility

The system is designed to be easily extended:
- Add new purchase types to `ShopPurchaseType` enum
- Add new section types to `ShopSectionType` enum
- Extend `LootReward` for item types beyond currency
- Add new package properties as needed
- Implement seasonal/event-based configuration overrides