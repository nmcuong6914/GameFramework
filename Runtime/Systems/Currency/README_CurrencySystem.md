# Currency and Inventory System Documentation

## Overview

This is a comprehensive, generic currency and inventory system for Unity games. It features:

- **Generic Currency System**: Supports different types of currencies (coins, lives, boosters, etc.)
- **Regenerating Currencies**: Currencies that regenerate over time with max caps (like lives)
- **Inventory Management**: Generic inventory system to store and manage all currencies
- **Save/Load System**: Local persistence using JSON serialization
- **PlayerData Management**: Complete player data container with automatic save/load
- **Service Integration**: Integrates with existing ServiceLocator pattern
- **UI Components**: Ready-to-use UI components for displaying currencies

## Core Components

### 1. Currency Types (`CurrencyType.cs`)
```csharp
public enum CurrencyType
{
    Coin = 0,
    Lives = 1,
    BoosterHammer = 2,
    BoosterShuffle = 3,
    BoosterUndo = 4,
    BoosterHint = 5
}
```

### 2. Currency Configuration (`CurrencyConfig`)
Defines how each currency behaves:
- Display name and icon
- Default and maximum amounts
- Regeneration settings (interval, amount)

### 3. Currency Class (`Currency.cs`)
Individual currency with:
- Amount tracking
- Regeneration logic
- Events for amount changes
- Display formatting

### 4. Inventory System (`Inventory.cs`)
- Manages multiple currencies
- Batch operations (spend/add multiple)
- Currency lookup and validation
- Regeneration updates

### 5. PlayerData (`PlayerData.cs`)
- Player identification (ID, device ID)
- Inventory container
- Timestamps (creation, last save)
- JSON serialization

### 6. Save System (`SaveSystem.cs`)
- Async save/load operations
- Backup file support
- Error handling
- Atomic saves

### 7. PlayerDataManager (`PlayerDataManager.cs`)
- Main coordinator component
- Auto-save functionality
- Service registration
- Event management

## Setup Instructions

### 1. Add PlayerDataManager to Scene
1. Create an empty GameObject named "PlayerDataManager"
2. Add the `PlayerDataManager` script
3. The component will auto-register itself with ServiceLocator

### 2. Configure Currencies
The system comes with default currencies, but you can customize them in `PlayerDataManager`:

```csharp
private void InitializeDefaultCurrencies()
{
    defaultCurrencyConfigs = new List<CurrencyConfig>
    {
        new CurrencyConfig(CurrencyType.Coin, "Coins", AssetKey.Currency_Coin, RegenerationType.None, 100),
        new CurrencyConfig(CurrencyType.Lives, "Lives", AssetKey.Currency_Lives, RegenerationType.OverTime, 5, 5, 300f, 1), // 5 minutes per life
        // Add more currencies...
    };
}
```

### 3. Register Services
The PlayerDataManager automatically registers itself with ServiceLocator.

### 4. Integration with Existing Code
Use the PlayerDataManager directly for common operations:

```csharp
// Get the PlayerDataManager from ServiceLocator
var playerDataManager = ServiceLocator.TryResolve<PlayerDataManager>();

// Use in gameplay
bool canPlay = playerDataManager.HasLivesToPlay();
if (canPlay)
{
    playerDataManager.TrySpendLife();
}

// Reward player
playerDataManager.RewardLevelCompletion(levelNumber);

// Use boosters
bool hammerUsed = playerDataManager.TryUseBooster(CurrencyType.BoosterHammer);
```

## UI Integration

### Currency Display UI
1. Create a UI GameObject
2. Add `CurrencyDisplayUI` script
3. Assign UI references (icon, text, regeneration slider)
4. Set currency type

### Currency Panel UI
1. Create a container GameObject
2. Add `CurrencyPanelUI` script
3. Assign currency display prefab and container
4. Configure which currencies to show

### Example UI Hierarchy:
```
CurrencyPanel
├── CurrencyContainer (Horizontal Layout Group)
├── CoinDisplay (CurrencyDisplayUI)
├── LivesDisplay (CurrencyDisplayUI)
└── BoosterDisplay (CurrencyDisplayUI)
```

## API Reference

### PlayerDataManager
```csharp
// Get currency amounts
int coins = playerDataManager.GetCurrencyAmount(CurrencyType.Coin);

// Add currency
bool success = playerDataManager.AddCurrency(CurrencyType.Coin, 100);

// Spend currency
bool success = playerDataManager.SpendCurrency(CurrencyType.Lives, 1);

// Multi-currency operations
var costs = new Dictionary<CurrencyType, int>
{
    { CurrencyType.Coin, 50 },
    { CurrencyType.BoosterHammer, 1 }
};
bool canAfford = playerDataManager.CanAfford(costs);
bool spent = playerDataManager.SpendMultiple(costs);

// Save operations
await playerDataManager.SaveAsync();
await playerDataManager.ForceSaveAsync();
```

### PlayerDataManager Gameplay Methods
```csharp
// Level completion
playerDataManager.RewardLevelCompletion(levelNumber);

// Lives management
bool hasLives = playerDataManager.HasLivesToPlay();
bool spentLife = playerDataManager.TrySpendLife();
float timeToNext = playerDataManager.GetTimeToNextLife();

// Boosters
bool hammerUsed = playerDataManager.TryUseBooster(CurrencyType.BoosterHammer);

// Purchases
var costs = new Dictionary<CurrencyType, int> { { CurrencyType.Coin, 100 } };
bool purchased = playerDataManager.MakePurchase(costs, "Extra Lives");

// Daily rewards
playerDataManager.AwardDailyBonus();
```

## Currency Configuration Examples

### Standard Coin
- No regeneration
- Unlimited maximum
- Earned through gameplay

### Lives System
- Regenerates over time
- Maximum cap (usually 5)
- Required to play levels
- 5-30 minutes per life

### Boosters
- No regeneration
- Earned rarely or purchased
- Consumable power-ups

## Save System Details

### Save Location
- **Path**: `Application.persistentDataPath/SaveData/playerdata.json`
- **Backup**: `playerdata_backup.json`
- **Format**: JSON with Newtonsoft.Json

### Auto-Save Features
- Configurable interval (default: 30 seconds)
- Save on application pause/focus lost
- Save on application quit
- Save when currency changes

### Error Handling
- Backup file fallback
- Corruption recovery
- Graceful degradation

## Events System

### PlayerDataManager Events
```csharp
playerDataManager.PlayerDataLoaded += OnPlayerDataLoaded;
playerDataManager.PlayerDataSaved += OnPlayerDataSaved;
playerDataManager.CurrencyChanged += OnCurrencyChanged;
```

### Currency Events
```csharp
currency.AmountChanged += (currency, oldAmount, newAmount) =>
{
    Debug.Log($"{currency.CurrencyType}: {oldAmount} -> {newAmount}");
};
```

### Inventory Events
```csharp
inventory.CurrencyChanged += OnCurrencyChanged;
inventory.CurrencyAdded += OnCurrencyAdded;
inventory.CurrencyRemoved += OnCurrencyRemoved;
```

## Testing and Debugging

### Debug Methods
Most components include context menu methods for testing:
- Add/spend currencies
- Force save/load
- Reset player data
- Test multi-currency operations

### Logging
Enable detailed logging by setting log levels in each component.

### Example Integration Script
See `CurrencySystemExample.cs` for complete usage examples.

## Best Practices

### 1. Initialization Order
1. Bootstrap system early in scene
2. Wait for PlayerDataManager initialization
3. Initialize game-specific systems
4. Register with ServiceLocator

### 2. Performance
- Currency regeneration updates every frame
- UI updates use events to avoid constant polling
- Save operations are asynchronous
- Consider caching frequently accessed values

### 3. Error Handling
- Always check if services are available
- Handle save/load failures gracefully
- Provide fallback values for missing currencies

### 4. Extensibility
- Add new currency types to `CurrencyType` enum
- Update `AssetKey` for new currency icons
- Create custom currency configurations
- Extend `PlayerDataManager` with additional gameplay methods as needed

## Troubleshooting

### Common Issues

**Q: PlayerDataManager not found**
A: Ensure `PlayerDataManager` component is in the scene and has been initialized

**Q: Currencies not updating**
A: Check that `PlayerDataManager.Update()` is being called

**Q: Save files not working**
A: Verify write permissions to `Application.persistentDataPath`

**Q: UI not updating**
A: Ensure UI components are subscribed to currency change events

**Q: Regeneration not working**
A: Check `CurrencyConfig.regenerationType` is set to `OverTime`

### Performance Tips
- Use events instead of polling for UI updates
- Cache PlayerDataManager reference
- Batch currency operations when possible
- Consider disabling auto-save in performance-critical moments

This system is designed to be reusable across different game types and easily extensible for specific game requirements.
