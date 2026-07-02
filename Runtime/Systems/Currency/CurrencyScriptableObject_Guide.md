# Currency ScriptableObject Setup Guide

## Overview

The currency system now uses ScriptableObjects for configuration, making it easier to manage and modify currency settings in the Unity Inspector.

## Setup Steps

### 1. Create Currency Configuration Asset

1. **Right-click in Project window** → Create → BlockSort → Currency Config Data
2. **Name it** "DefaultCurrencyConfig" (or your preferred name)
3. **Configure currencies** in the Inspector

### 2. Initialize Default Currencies

1. **Select your CurrencyConfigData asset**
2. **Right-click in Inspector** → Context Menu → "Initialize Default Currencies"
3. This will populate the list with default configurations for all 6 currency types

### 3. Assign to PlayerDataManager

1. **Select GameObject with PlayerDataManager**
2. **In Inspector**, find "Currency Configuration" section
3. **Drag your CurrencyConfigData asset** to the "Currency Config Data" field

## Currency Configuration Options

### Basic Settings
- **Currency Type**: The type of currency (Coin, Lives, Booster, etc.)
- **Display Name**: Name shown in UI
- **Icon Asset Key**: Reference to the icon sprite (restricted to currency assets only - shows only Currency_* options)
- **Default Amount**: Starting amount for new players
- **Max Amount**: Maximum amount that can be held (999999 recommended)

### Regeneration Settings  
- **Regeneration Type**: 
  - `None`: No regeneration (coins, boosters)
  - `OverTime`: Regenerates periodically (lives)
- **Regeneration Interval**: Time in seconds between regenerations
- **Regeneration Amount**: How much to regenerate each time

## Default Configuration Values

| Currency | Default | Max | Regeneration |
|----------|---------|-----|--------------|
| Coins | 1000 | 999999 | None |
| Lives | 5 | 5 | Every 5 minutes |
| Hammer | 3 | 999999 | None |
| Shuffle | 3 | 999999 | None |
| Undo | 3 | 999999 | None |
| Hint | 3 | 999999 | None |

## Benefits of ScriptableObject Approach

✅ **Easy Configuration**: Modify currency settings in Inspector without code changes
✅ **Designer Friendly**: Non-programmers can adjust values
✅ **Runtime Safe**: Changes don't require recompilation
✅ **Version Control**: Settings are stored in asset files
✅ **Reusable**: Same config can be used across different scenes
✅ **Validation**: Automatic validation prevents invalid configurations

## Fallback System

If no CurrencyConfigData is assigned to PlayerDataManager:
- System will use hardcoded fallback values
- Warning message will be logged
- System continues to work normally

## Advanced Usage

### Creating Custom Currency Configurations

```csharp
// Create new currency config at runtime
var customConfig = new CurrencyConfig(
    CurrencyType.Coin,
    "Special Coins", 
    AssetKey.Currency_Coin,
    RegenerationType.None,
    defaultAmt: 500,
    maxAmt: 999999
);

// Add to existing config data (editor only)
#if UNITY_EDITOR
yourConfigData.currencyConfigs.Add(customConfig);
UnityEditor.EditorUtility.SetDirty(yourConfigData);
#endif
```

### Loading Config at Runtime

```csharp
// Load config from Resources folder
var configData = Resources.Load<CurrencyConfigData>("Currency/DefaultCurrencyConfig");
playerDataManager.currencyConfigData = configData;
```

## Validation Features

The CurrencyConfigData automatically validates:
- **No duplicate currency types**
- **MaxAmount ≥ DefaultAmount**
- **Valid regeneration settings** for OverTime currencies
- **Proper configuration values**

Warnings are shown in Console if validation fails.

## Migration from Hardcoded Config

If you were using the old hardcoded system:
1. Create new CurrencyConfigData asset
2. Use "Initialize Default Currencies" context menu
3. Assign to PlayerDataManager
4. Remove any hardcoded currency initialization code

The new system is fully backward compatible and will fallback to hardcoded values if no ScriptableObject is assigned.
