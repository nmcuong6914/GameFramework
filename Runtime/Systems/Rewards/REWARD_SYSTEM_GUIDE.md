# End Level Reward System Setup Guide

## Overview

The End Level Reward System allows you to configure rewards that players receive when completing levels. It supports:

- ✅ **Default rewards for all levels**
- ✅ **Custom rewards for specific levels or level ranges**
- ✅ **Priority system for rule precedence**
- ✅ **Additive and override reward rules**
- ✅ **Easy-to-use custom editor**
- ✅ **Preview system for testing**
- ✅ **Integration with existing currency system**

## Quick Setup (3 Steps)

### 1. Create EndLevelRewardConfig Asset
1. Right-click in Project window
2. Create → BlockSort → End Level Reward Config
3. Name it `EndLevelRewardConfig`
4. Click "Initialize Default Config" button in inspector

### 2. Assign to PlayerDataManager
1. Select your PlayerDataManager GameObject in scene
2. Drag the EndLevelRewardConfig asset to the "End Level Reward Config" field

### 3. Test the System
1. Add the `RewardSystemDemo` script to any GameObject
2. Use the context menu options to test rewards

## Components

### 1. Reward Class (`Reward.cs`)
Represents a single reward:
```csharp
public class Reward
{
    public CurrencyType currencyType;
    public int amount;
}
```

### 2. LevelRewardRule Class
Defines reward rules for level ranges:
- **startLevel**: Starting level (-1 for "any level")
- **endLevel**: Ending level (-1 for "from startLevel onwards")
- **rewards**: List of rewards to give
- **additive**: Whether to add to or override other rules
- **priority**: Higher numbers = higher priority

### 3. EndLevelRewardConfig (`EndLevelRewardConfig.cs`)
ScriptableObject that holds all reward rules:
- Processes rules by priority
- Supports additive and override behaviors
- Provides preview functionality

## Usage Examples

### Basic Usage (PlayerDataManager)
```csharp
// Reward player for completing level 5
playerDataManager.RewardLevelCompletion(5);

// Preview rewards for level 10 without giving them
var rewards = playerDataManager.GetLevelRewardPreview(10);
foreach (var reward in rewards)
{
    Debug.Log($"{reward.Key}: {reward.Value}");
}

// Preview multiple levels
var preview = playerDataManager.GetMultipleLevelRewardPreview(1, 20);
```

### Direct Configuration Usage
```csharp
// Get rewards directly from configuration
var rewards = endLevelRewardConfig.GetRewardsForLevel(15);

// Add custom rules programmatically
endLevelRewardConfig.AddSingleLevelRule(25, new List<Reward>
{
    new Reward(CurrencyType.Coin, 1000),
    new Reward(CurrencyType.Lives, 5)
});
```

## Configuration Examples

### Default Configuration
The "Initialize Default Config" creates:
- **Default**: 50 coins for all levels
- **Every 5th level**: +50 coins, +1 hammer
- **Every 10th level**: +100 coins, +1 shuffle, +1 hint
- **Milestones**: Special rewards at levels 25, 50, 100

### Custom Configurations

#### Simple Progression
```csharp
// Default: 50 coins per level
// Levels 11-20: Extra 25 coins
// Levels 21+: Extra 50 coins
```

#### Event-Based Rewards
```csharp
// Weekend bonus: Double coins for all levels
// Special event: Extra boosters on specific levels
```

#### Difficulty-Based Rewards
```csharp
// Easy levels (1-10): Standard rewards
// Medium levels (11-30): +50% rewards
// Hard levels (31+): Double rewards
```

## Custom Editor Features

### Rule Management
- **Add/Delete Rules**: Easy rule management
- **Reorder Rules**: Drag to change priority
- **Rule Types**: Default, level range, or single level

### Preview System
- **Live Preview**: See rewards for any level range
- **Visual Feedback**: Quickly validate configuration
- **Testing**: Test without affecting game state

### Validation
- **Automatic Validation**: Removes invalid rules
- **Warning Messages**: Clear feedback on issues
- **Default Values**: Sensible defaults for new rules

## Integration with GameplayManager

```csharp
public class GameplayManager : MonoBehaviour
{
    private PlayerDataManager playerDataManager;
    
    private void OnLevelComplete(int levelNumber)
    {
        // Give level completion rewards
        playerDataManager.RewardLevelCompletion(levelNumber);
        
        // Advance to next level
        playerDataManager.CompleteCurrentLevel();
        
        // Show rewards UI
        ShowRewardsUI(playerDataManager.GetLevelRewardPreview(levelNumber));
    }
}
```

## Advanced Usage

### Dynamic Rewards
```csharp
// Modify rewards based on player performance
var baseRewards = endLevelRewardConfig.GetRewardsForLevel(level);

// Bonus for perfect completion
if (isPerfectCompletion)
{
    foreach (var currency in baseRewards.Keys.ToList())
    {
        baseRewards[currency] = (int)(baseRewards[currency] * 1.5f);
    }
}

playerDataManager.AddMultiple(baseRewards);
```

### Conditional Rewards
```csharp
// Only give bonus rewards if player hasn't received daily bonus
if (!playerData.HasReceivedDailyBonus)
{
    var bonusRewards = new Dictionary<CurrencyType, int>
    {
        { CurrencyType.Lives, 2 },
        { CurrencyType.BoosterHammer, 1 }
    };
    
    playerDataManager.AddMultiple(bonusRewards);
}
```

## Testing

### Using RewardSystemDemo
1. Add `RewardSystemDemo` script to any GameObject
2. Use context menu options:
   - **Test Level Completion Reward**: Test single level
   - **Preview Level Rewards**: Preview multiple levels
   - **Simulate Level Progression**: Test progression through levels

### Debug Methods
```csharp
// Check if configuration is loaded
Debug.Log($"Has Config: {playerDataManager.HasRewardConfiguration}");

// Preview specific level
var rewards = playerDataManager.GetLevelRewardPreview(10);

// Test multiple levels
var preview = playerDataManager.GetMultipleLevelRewardPreview(1, 20);
```

## Best Practices

### 1. Rule Organization
- Use clear naming conventions
- Set appropriate priorities
- Keep rules simple and focused

### 2. Balance Considerations
- Start with modest default rewards
- Scale rewards with level difficulty
- Avoid inflation by capping maximum rewards

### 3. Performance
- Keep rule count reasonable (<50 rules)
- Use level ranges instead of individual level rules where possible
- Test with large level numbers

### 4. Flexibility
- Use additive rules for bonuses
- Use override rules for special events
- Plan for seasonal/event modifications

## Troubleshooting

### Common Issues

**No rewards given:**
- Check if EndLevelRewardConfig is assigned to PlayerDataManager
- Verify rules apply to the test level
- Check reward amounts are > 0

**Unexpected reward amounts:**
- Review rule priorities
- Check additive vs. override settings
- Use preview to validate configuration

**Editor not showing properly:**
- Ensure custom editor script is in Editor folder
- Check for compile errors
- Try reimporting scripts

### Debug Steps
1. Check `playerDataManager.HasRewardConfiguration`
2. Use `GetLevelRewardPreview()` to test without side effects
3. Enable detailed logging in PlayerDataManager
4. Use RewardSystemDemo for comprehensive testing

## Migration from Hardcoded Rewards

If you were using the old hardcoded reward system:

1. **Create EndLevelRewardConfig asset**
2. **Configure equivalent rules** (use preview to match old behavior)
3. **Assign to PlayerDataManager**
4. **Test thoroughly** with RewardSystemDemo
5. **Remove old hardcoded logic** (optional - it serves as fallback)

The new system is fully backward compatible and will use fallback rewards if no configuration is assigned.
