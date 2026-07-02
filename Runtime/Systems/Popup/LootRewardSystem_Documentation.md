# Loot Reward Popup System

This system provides a comprehensive way to display loot rewards to players when they receive currency from shop purchases or other sources.

## 🎯 **Components Created**

### Core Classes
- **`LootRewardData`** - Screen data class for the popup containing rewards, source, and title
- **`LootRewardSignal`** - Signal fired when rewards should be displayed
- **`LootRewardPopup`** - Main popup script that displays rewards with animations
- **`LootRewardItem`** - Component for individual reward items in the popup
- **`LootRewardPopupHandler`** - Signal handler that shows the popup when signals are received
- **`LootRewardUtility`** - Utility class for easy popup display from anywhere

### Integration
- **`PopupType.LootReward`** - Added to the PopupType enum
- **`PurchaseManager`** - Modified to fire loot reward signals when shop purchases are completed

## 🚀 **How It Works**

### Automatic Display (Shop Purchases)
When a player completes an IAP purchase:
1. `PurchaseManager.ProcessLootRewards()` processes the shop package rewards
2. Rewards are added to player data via `PlayerDataManager.ApplyLootRewards()`
3. A `LootRewardSignal` is fired with the reward details
4. `LootRewardPopupHandler` receives the signal and shows the popup
5. `LootRewardPopup` displays rewards with currency icons and amounts

### Manual Display (From Code)
```csharp
// Show multiple rewards
var rewards = new Dictionary<CurrencyType, int>
{
    { CurrencyType.Coin, 1000 },
    { CurrencyType.BoosterHammer, 5 }
};
LootRewardUtility.ShowLootRewardPopup(rewards, "Daily Bonus", "Daily Reward!");

// Show single reward
LootRewardUtility.ShowSingleRewardPopup(CurrencyType.Coin, 500, "Level Complete");

// Show from LootReward object
LootRewardUtility.ShowLootRewardPopup(lootRewardObject, "Chest Opening");
```

## 🔧 **Setup Requirements**

### 1. PopupConfig Configuration
Add the LootReward popup to your PopupConfig ScriptableObject:
- **Popup Type**: `LootReward`
- **Asset Reference**: Reference to the LootRewardPopup prefab

### 2. Scene Setup
Add `LootRewardPopupHandler` component to a GameObject in your main scene to handle the signals.

### 3. Prefab Structure
The LootRewardPopup prefab should contain:
```
LootRewardPopup (Canvas)
├── Background (Image with dimmed background)
├── PopupContainer (UI container)
│   ├── TitleText (TextMeshPro)
│   ├── SourceText (TextMeshPro)
│   ├── RewardsContainer (Transform for reward items)
│   └── CollectButton (Button)
└── RewardItemPrefab (prefab reference)
    ├── CurrencyIcon (Image)
    └── AmountText (TextMeshPro)
```

## ✨ **Features**

### Visual Features
- **Sequential Animation**: Reward items appear one by one with scale animation
- **Currency Icons**: Automatically loads appropriate currency icons from AssetManager
- **Amount Formatting**: Smart formatting (1K, 1.5M) for large numbers
- **Responsive Layout**: Adapts to different numbers of rewards

### Audio Integration
- **Reward Sound**: Plays when each reward item appears
- **Collect Sound**: Plays when collect button is pressed
- **Configurable**: Sound names can be customized in the inspector

### Analytics Integration
- **Automatic Tracking**: All rewards are tracked with analytics
- **Detailed Data**: Tracks source, package ID, and individual reward amounts
- **Event Name**: `loot_reward_received`

## 🎮 **User Experience**

1. **Popup Appears**: After purchase completion or manual trigger
2. **Rewards Animate In**: Each reward appears sequentially with sound
3. **Player Reviews**: Can see all rewards with icons and amounts
4. **Collect**: Player taps collect button to close popup
5. **Popup Closes**: Smooth transition out

## 🔄 **Integration Points**

### Existing Systems
- **PopupManager**: Uses existing popup system for display management
- **SignalBus**: Uses existing signal system for communication
- **AssetManager**: Uses existing asset system for currency icons
- **SoundManager**: Uses existing audio system for sound effects
- **PlayerDataManager**: Integrates with existing currency system
- **Analytics**: Uses existing analytics system for tracking

### Purchase Flow Integration
```
Purchase Complete → Process Rewards → Apply to Player Data → Fire Signal → Show Popup
```

## 🛠️ **Customization**

### Animation Timing
- `itemAppearDelay`: Time between reward items appearing
- `itemAnimationDuration`: Duration of scale animation for each item

### Audio
- `rewardSoundName`: Sound played when rewards appear
- `collectSoundName`: Sound played when collect button is pressed

### Visual
- Currency icons are loaded from `CurrencyConfig.iconAssetKey`
- Popup appearance follows existing BasePopup conventions
- Supports Unity's UI animation system

## 📋 **Error Handling**

- **Missing Services**: Graceful degradation if services are unavailable
- **Invalid Data**: Validates reward data before display
- **Asset Loading**: Handles missing or failed currency icon loads
- **Exception Safety**: All operations wrapped in try-catch blocks

## 🎯 **Usage Examples**

### Shop Purchase (Automatic)
```csharp
// Happens automatically when PurchaseManager.ProcessLootRewards() is called
// No additional code needed
```

### Level Completion Rewards
```csharp
public void OnLevelComplete(int levelNumber)
{
    var rewards = new Dictionary<CurrencyType, int>
    {
        { CurrencyType.Coin, 100 + levelNumber * 10 },
        { CurrencyType.BoosterHammer, levelNumber % 5 == 0 ? 1 : 0 }
    };
    
    LootRewardUtility.ShowLootRewardPopup(rewards, $"Level {levelNumber} Complete", "Well Done!");
}
```

### Daily Bonus
```csharp
public void GiveDailyBonus()
{
    LootRewardUtility.ShowSingleRewardPopup(
        CurrencyType.Coin, 
        1000, 
        "Daily Login Bonus", 
        "Welcome Back!"
    );
}
```

This system provides a polished, extensible way to show rewards to players while maintaining consistency with the existing game architecture.