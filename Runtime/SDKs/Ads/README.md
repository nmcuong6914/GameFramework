# IronSource Ads Manager System

## Overview

This ads system provides a complete integration with IronSource LevelPlay SDK for Unity, supporting both interstitial and rewarded video ads with placement-based configuration.

## Components

### 1. **AdsConfig (ScriptableObject)**
- **File**: `AdsConfig.cs`
- **Purpose**: Configuration for all ads settings including placements, cooldowns, and platform settings
- **Features**:
  - General ads settings (enable/disable, app ID, test mode)
  - Interstitial ads configuration (cooldowns, level intervals)
  - Rewarded ads configuration (auto-load settings)
  - Platform-specific settings (iOS/Android)
  - Ad placement definitions with rewards

### 2. **AdsManager (MonoBehaviour)**
- **File**: `AdsManager.cs`
- **Purpose**: Core ads functionality and IronSource SDK integration
- **Features**:
  - IronSource SDK initialization
  - Interstitial and rewarded ad management
  - Placement-based cooldown and session limits
  - Event system for ad callbacks
  - Automatic ad loading

### 3. **Supporting Files**
- **AdsSignals.cs**: Signal system integration for ads events
- **AdsConstants.cs**: Common constants and helper utilities
- **AdsIntegrationExample.cs**: Example usage and integration patterns
- **Editor/AdsConfigEditor.cs**: Custom inspector for AdsConfig

## Setup Instructions

### 1. **Configure IronSource App ID**
1. Open the `DefaultAdsConfig` asset in `Assets/Scripts/Services/Ads/`
2. Set your platform-specific App IDs in the iOS and Android platform settings
3. Configure platform-specific settings if needed
4. Adjust ad placements and rewards as needed

### 2. **Initialize AdsManager**
The AdsManager is registered with the ServiceLocator during game initialization in GameInitFlow.

Add the AdsConfig to your GameInitFlow component and it will automatically:
1. Create an AdsManager instance
2. Register it with ServiceLocator
3. Initialize it with your configuration

```csharp
// Access AdsManager from any script
var adsManager = ServiceLocator.TryResolve<AdsManager>();

## Usage Examples

### Basic Usage

```csharp
// Get the ads manager from ServiceLocator
var adsManager = ServiceLocator.TryResolve<AdsManager>();

// Show interstitial ad
adsManager?.ShowInterstitial("level_complete", () => {
    Debug.Log("Interstitial closed");
});

// Show rewarded ad with automatic reward granting
adsManager?.ShowRewardedWithAutoGrant("extra_lives", 
    onCompleted: () => Debug.Log("Player received extra life!"),
    onFailed: () => Debug.Log("Ad failed"));

// Check if ads are available
if (adsManager != null && adsManager.CanShowRewarded("double_coins"))
{
    // Show button to watch ad
}
```

### Level Progression Integration

```csharp
public void OnLevelCompleted(int levelNumber)
{
    var adsManager = ServiceLocator.TryResolve<AdsManager>();
    
    // Show interstitial every 3rd level (configurable)
    adsManager?.ShowInterstitialAfterLevel(levelNumber, () => {
        // Continue to next level
        LoadNextLevel();
    });
}
```

### Reward Integration

```csharp
public void OnWatchAdForDoubleCoins()
{
    var adsManager = ServiceLocator.TryResolve<AdsManager>();
    
    adsManager?.ShowRewarded("double_coins",
        onCompleted: (placement, currencyType, amount) => {
            // Reward is automatically granted to player
            // Show celebration UI
            ShowRewardCelebration(currencyType, amount);
        },
        onFailed: () => {
            ShowMessage("Ad not available right now");
        });
}
```

## Configuration

### Ad Placements

The system comes with predefined placements:

- **level_complete**: After completing a level
- **game_over**: When player runs out of lives  
- **extra_lives**: To get extra lives
- **double_coins**: To double level rewards
- **hint_booster**: To get hint booster
- **hammer_booster**: To get hammer booster

Each placement can be configured with:
- Allowed ad types (interstitial/rewarded)
- Minimum level requirement
- Session limits
- Cooldown periods
- Reward configuration

### Cooldown System

- **Global Interstitial Cooldown**: Prevents showing interstitials too frequently
- **Placement Cooldowns**: Per-placement cooldown timers
- **Session Limits**: Maximum ads per placement per session

### Platform Settings

- **iOS/Android specific settings**
- **COPPA/GDPR/CCPA compliance**
- **Test mode configuration**

## Events System

The ads system integrates with the project's signal system:

```csharp
// Listen for ads events
SignalBus.Subscribe<AdsInitializedSignal>(OnAdsInitialized);
SignalBus.Subscribe<RewardedAdCompletedSignal>(OnRewardedCompleted);

private void OnAdsInitialized(AdsInitializedSignal signal)
{
    if (signal.Success)
    {
        // Enable ad-related UI
    }
}
```

## Testing

### Development Testing
1. Set "Test Mode" to true in AdsConfig
2. Use the "Context Menu" options in AdsIntegrationExample for testing
3. Check console logs for ad events

### Production Testing
1. Set "Test Mode" to false
2. Configure proper IronSource App ID
3. Test on actual devices

## Integration with Existing Systems

### PlayerDataManager Integration
- Automatic currency granting for rewarded ads
- Uses existing CurrencyType enum
- Integrates with player progression system

### Popup System Integration
- Ready for integration with existing popup system
- Can be added to WinLevelPopup and other UI elements
- Supports the existing async/await patterns

### ServiceLocator Integration
- Follows the project's service pattern
- Automatic registration/unregistration
- Easy dependency resolution

## File Structure

```
Assets/Scripts/Services/Ads/
├── AdsConfig.cs                 # Configuration ScriptableObject
├── AdsManager.cs               # Core ads manager (ServiceLocator registered)
├── AdsSignals.cs               # Signal definitions
├── AdsConstants.cs             # Constants and helpers
├── AdsIntegrationExample.cs    # Usage examples
├── DefaultAdsConfig.asset      # Default configuration
└── Editor/
    └── AdsConfigEditor.cs      # Custom inspector
```

## Requirements

- IronSource LevelPlay SDK (already installed in project)
- UniTask package (already in project)
- PlayerDataManager (for automatic reward granting)
- Signal system (already in project)

## Notes

- The system is registered with ServiceLocator during game initialization
- All ads functionality is optional and gracefully handles initialization failures  
- Comprehensive error handling and logging
- Editor tools for easy configuration
- Production-ready with proper cooldowns and limits
- Platform-specific app IDs for iOS and Android
