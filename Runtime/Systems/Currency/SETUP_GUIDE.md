# Simplified Currency System Setup Guide

## Overview

I've created a comprehensive currency system based on your requirements:

- ✅ **Multiple Currency Types**: Coins, Lives, Boosters (Hammer, Shuffle, Undo, Hint)
- ✅ **Regeneration System**: Lives regenerate every 5 minutes (configurable)
- ✅ **Generic Inventory**: Reusable across different game types
- ✅ **PlayerData with Save/Load**: Local JSON persistence with backup system
- ✅ **SignalBus Integration**: Uses your existing signal system
- ✅ **Service Locator Integration**: Registers with your DI system
- ✅ **Level Progress Tracking**: Tracks current and highest unlocked levels

## Quick Setup (3 Steps)

### 1. Add PlayerDataManager to Scene
1. Create empty GameObject named "PlayerDataManager"
2. Add `PlayerDataManager` component
3. **Recommended**: Create CurrencyConfigData ScriptableObject (Right-click → Create → BlockSort → Currency Config Data)
4. Use context menu "Initialize Default Currencies" on the ScriptableObject
5. Assign the ScriptableObject to PlayerDataManager's "Currency Config Data" field

### 2. Access Currency System in GameplayManager
Use PlayerDataManager directly from ServiceLocator to access currencies by type:

```csharp
var playerDataManager = ServiceLocator.TryResolve<PlayerDataManager>();
int coins = playerDataManager.GetCurrencyAmount(CurrencyType.Coin);
bool success = playerDataManager.TryUseBooster(CurrencyType.BoosterHammer);
```

### 3. Register PlayerDataManager (Optional - it self-registers)
The PlayerDataManager automatically registers itself with ServiceLocator in Awake().

## Default Currency Configuration

```csharp
// Automatically created:
- Coins: 100 starting, unlimited, no regeneration
- Lives: 5 starting, max 5, regenerates 1 every 5 minutes  
- Boosters: 3 each starting (Hammer, Shuffle, Undo, Hint)
```

## Usage Examples

### In Your Scripts:
```csharp
// Get PlayerDataManager
var playerDataManager = ServiceLocator.TryResolve<PlayerDataManager>();

// Check/spend currencies
if (playerDataManager.GetCurrencyAmount(CurrencyType.Lives) > 0)
{
    playerDataManager.SpendCurrency(CurrencyType.Lives, 1);
    // Start level
}

// Add rewards
playerDataManager.AddCurrency(CurrencyType.Coin, 100);

// Level progression
int currentLevel = playerDataManager.GetCurrentLevelIndex();
playerDataManager.CompleteCurrentLevel(); // Advances to next level
```

### With Signals:
```csharp
// Listen to currency changes
signalBus.Subscribe<CurrencyChangedSignal>(OnCurrencyChanged);
signalBus.Subscribe<PlayerDataLoadedSignal>(OnPlayerDataLoaded);

private void OnCurrencyChanged(CurrencyChangedSignal signal)
{
    Debug.Log($"{signal.CurrencyType}: {signal.OldAmount} -> {signal.NewAmount}");
}
```

### GameplayManager Integration:
```csharp
// Access PlayerDataManager directly in your GameplayManager:
var playerDataManager = ServiceLocator.TryResolve<PlayerDataManager>();

// Check if player has lives to play
bool canPlay = playerDataManager.HasLivesToPlay();

// Try to spend a life to start level
if (playerDataManager.TrySpendLife()) {
    // Start level
}

// Reward level completion
playerDataManager.RewardLevelCompletion(levelNumber);

// Use boosters
bool hammerUsed = playerDataManager.TryUseBooster(CurrencyType.BoosterHammer);
```

## Key Files Created

**Core System:**
- `CurrencyType.cs` - Currency definitions and config
- `CurrencyConfigData.cs` - ScriptableObject for easy configuration
- `Currency.cs` - Individual currency with regeneration
- `Inventory.cs` - Generic inventory system
- `PlayerData.cs` - Player data container with level tracking
- `SaveSystem.cs` - Local save/load system
- `PlayerDataManager.cs` - Main coordinator

**Integration:**
- `CurrencySignals.cs` - SignalBus integration
- UI components for currency display

**Removed:**
- `CurrencySystemBootstrap.cs` - PlayerDataManager self-registers
- `GameplayCurrencyIntegration.cs` - Methods moved to PlayerDataManager
- `GameplayManagerCurrencyIntegration.cs` - Access currencies directly by ID

## Features

### ✅ Regeneration System
Lives automatically regenerate over time:
```csharp
var livesCurrency = playerDataManager.Inventory.GetCurrency(CurrencyType.Lives);
float timeToNext = livesCurrency.GetTimeToNextRegeneration(); // For UI countdown
```

### ✅ Level Progress Tracking
```csharp
// GameplayManager automatically loads from saved level
int currentLevel = playerDataManager.GetCurrentLevelIndex();
bool canPlay = playerDataManager.IsLevelUnlocked(5);
```

### ✅ SignalBus Communication
```csharp
// Fired automatically when currencies change
CurrencyChangedSignal
PlayerDataLoadedSignal  
PlayerDataSaveSignal
```

### ✅ Auto-Save System
- Saves every 30 seconds when changes occur
- Saves on app pause/quit
- Backup file system for safety

## Testing

Use PlayerDataManager methods directly for testing:
```csharp
var playerDataManager = ServiceLocator.TryResolve<PlayerDataManager>();

// Test adding currencies
playerDataManager.AddCurrency(CurrencyType.Coin, 100);
playerDataManager.AddCurrency(CurrencyType.Lives, 1);

// Test using boosters
bool success = playerDataManager.TryUseBooster(CurrencyType.BoosterHammer);

// Test level completion
playerDataManager.RewardLevelCompletion(1);
```

## Integration with Your Game

### GameplayManager Changes:
- Now waits for PlayerDataManager initialization
- Loads current level from saved data instead of incrementing
- Updates level progress on completion via reflection (no direct dependency)

### No Breaking Changes:
- Your existing GameplayManager continues to work
- Currency features are additive
- Can be disabled by not adding PlayerDataManager

The system is production-ready and handles all your requirements while maintaining clean separation of concerns!
