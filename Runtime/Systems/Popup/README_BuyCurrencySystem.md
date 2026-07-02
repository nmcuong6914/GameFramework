# Buy Currency System - Simplified

A streamlined Unity system for purchasing in-game currencies using coins, integrated with signals and GameUI.

## Features

- **Simple Configuration**: ScriptableObject-based purchase options
- **Currency-Based**: Direct mapping to CurrencyType enum
- **Signal Integration**: Works with existing popup signal system
- **GameUI Integration**: Easy-to-use methods in GameUI
- **Purchase Logic**: Coin spending with automatic currency addition

## Core Components

### 1. PurchaseItemConfig
```csharp
[Serializable]
public class PurchaseItemConfig
{
    public CurrencyType targetCurrency;
    public int amount = 1;
    public int coinCost = 100;
}
```

### 2. PurchaseConfig (ScriptableObject)
```csharp
[CreateAssetMenu(fileName = "PurchaseConfig", menuName = "BlockSort/Purchase Config")]
public class PurchaseConfig : ScriptableObject
{
    public List<PurchaseItemConfig> GetOptionsForCurrency(CurrencyType currencyType);
}
```

### 3. PopupBuyCurrency
The main popup component that:
- References PurchaseConfig for options
- Creates UI dynamically based on currency type
- Handles purchase transactions
- Integrates with PlayerDataManager

### 4. BuyCurrencyPopupData (Simplified)
```csharp
public class BuyCurrencyPopupData : ScreenData
{
    public CurrencyType targetCurrency;
    public string title;
    
    public BuyCurrencyPopupData(CurrencyType currency, string popupTitle = null);
}
```

## Usage

### Basic Usage (GameUI)
```csharp
// Show buy lives popup
gameUI.ShowBuyLivesPopup();

// Show buy popup for any currency
gameUI.ShowBuyCurrencyPopup(CurrencyType.BoosterHammer);
```

### Signal Usage
```csharp
// Fire signal to show buy currency popup
var signal = new ShowBuyCurrencyPopupSignal(CurrencyType.Lives);
signalBus.Fire(signal);
```

### Setup Configuration
1. Create PurchaseConfig asset: `Assets → Create → BlockSort → Purchase Config`
2. Add purchase options for each currency type:
   ```csharp
   // Example configuration
   {
       targetCurrency = CurrencyType.Lives,
       amount = 1,
       coinCost = 100
   },
   {
       targetCurrency = CurrencyType.Lives,
       amount = 5,
       coinCost = 450
   }
   ```
3. Assign PurchaseConfig to PopupBuyCurrency component

## Architecture Benefits

- **Simplified**: Removed complex mode system and pack types
- **Currency-Focused**: Direct integration with existing CurrencyType enum
- **Maintainable**: Single configuration file for all purchase options
- **Extensible**: Easy to add new currency types or adjust pricing
- **Integrated**: Works seamlessly with GameUI and signal systems

## Dependencies

- PlayerDataManager (for currency transactions)
- SignalBus (for popup signals)
- GameUI (for easy access methods)
- CurrencyType enum (existing currency system)
