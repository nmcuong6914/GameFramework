# Firebase Remote Config Integration

This document describes the Firebase Remote Config integration for the Block Sort game, including setup, usage, and configuration management.

## Overview

The Firebase Remote Config system allows you to:
- Control app configuration remotely without app updates
- A/B test different features and settings
- Force app updates when necessary
- Enable/disable features dynamically
- Manage game balance parameters

## Components

### 1. **RemoteConfigService**
- Main service that handles Firebase Remote Config loading
- Manages local caching for offline scenarios
- Provides configuration access throughout the game
- Located: `Assets/Scripts/Services/RemoteConfig/RemoteConfigService.cs`

### 2. **RemoteConfigData**
- Data model representing the configuration structure
- Contains all configurable parameters
- Includes validation and utility methods
- Located: `Assets/Scripts/Services/RemoteConfig/RemoteConfigData.cs`

### 3. **RemoteConfigManager**
- Static utility class for easy access to config values
- Provides convenient methods with fallback values
- Handles service availability checks
- Located: `Assets/Scripts/Services/RemoteConfig/RemoteConfigManager.cs`

### 4. **VersionUpdatePopup**
- UI popup that appears when app version is outdated
- Forces users to update when necessary
- Integrates with app store links
- Located: `Assets/Scripts/UI/Popups/VersionUpdatePopup.cs`

## Firebase Setup

### 1. Firebase Console Configuration

1. **Navigate to Remote Config** in your Firebase console
2. **Create a parameter** named `config_json`
3. **Set the value** as a JSON object with your configuration:

```json
{
    "versionCode": "1.0.0",
    "forceUpdateMessage": "Please update to the latest version to continue playing!",
    "enableNewFeature": false,
    "maxLives": 5,
    "livesRegenerationTime": 1800,
    "adsEnabled": true,
    "interstitialAdInterval": 3,
    "showRewardedAdOnFail": true,
    "iapEnabled": true,
    "specialOfferDiscount": 50,
    "enableLevelSkip": true,
    "dailyFreeSkips": 1,
    "holidayEventEnabled": false,
    "eventStartTime": 0,
    "eventEndTime": 0,
    "detailedAnalyticsEnabled": true,
    "analyticsSamplingRate": 1.0,
    "debugFeaturesEnabled": false,
    "showDebugUI": false
}
```

4. **Publish the changes** in Firebase console

### 2. Unity Setup

1. **Add RemoteConfigService** to your GameInitFlow prefab
2. **Configure the service** in the inspector:
   - Set `debugLogs` to true for development
   - Adjust `fetchTimeoutSeconds` if needed (default: 30s)
3. **The service** will be automatically registered and initialized

## Usage Examples

### Basic Usage with RemoteConfigManager

```csharp
// Check if ads should be shown
if (RemoteConfigManager.AreAdsEnabled())
{
    ShowInterstitialAd();
}

// Get maximum lives with fallback
int maxLives = RemoteConfigManager.GetMaxLives(fallback: 5);

// Check for version updates
if (RemoteConfigManager.IsVersionOutdated())
{
    // Version update popup will be shown automatically by GameInitFlow
}

// Check if a feature is enabled
if (RemoteConfigManager.IsNewFeatureEnabled())
{
    EnableNewGameFeature();
}
```

### Direct Service Access

```csharp
// Get the service
var remoteConfig = ServiceLocator.TryResolve<RemoteConfigService>();

if (remoteConfig != null && remoteConfig.IsInitialized)
{
    // Access raw config data
    var configData = remoteConfig.ConfigData;
    
    // Get specific values
    string version = remoteConfig.GetString("versionCode", "1.0.0");
    int maxLives = remoteConfig.GetInt("maxLives", 5);
    bool adsEnabled = remoteConfig.GetBool("adsEnabled", true);
}
```

### Listening to Config Updates

```csharp
void Start()
{
    var remoteConfig = ServiceLocator.TryResolve<RemoteConfigService>();
    if (remoteConfig != null)
    {
        remoteConfig.OnConfigLoaded += OnConfigLoaded;
        remoteConfig.OnConfigLoadFailed += OnConfigLoadFailed;
    }
}

private void OnConfigLoaded(RemoteConfigData configData)
{
    Debug.Log($"Remote config loaded. Version: {configData.versionCode}");
    
    // Update UI or game features based on new config
    UpdateGameFeatures();
}

private void OnConfigLoadFailed(string error)
{
    Debug.LogError($"Failed to load remote config: {error}");
    // Continue with cached/default config
}
```

## Configuration Parameters

### App Version Management
- **versionCode**: Minimum required app version
- **forceUpdateMessage**: Message shown to users when update is required

### Game Features
- **enableNewFeature**: Toggle for experimental features
- **maxLives**: Maximum number of lives players can have
- **livesRegenerationTime**: Time in seconds for lives to regenerate
- **enableLevelSkip**: Allow players to skip levels
- **dailyFreeSkips**: Number of free level skips per day

### Ads Configuration
- **adsEnabled**: Master switch for all ads
- **interstitialAdInterval**: Show interstitial after X level completions
- **showRewardedAdOnFail**: Show rewarded ad option on level fail

### IAP Configuration
- **iapEnabled**: Master switch for in-app purchases
- **specialOfferDiscount**: Discount percentage for special offers

### Events & Promotions
- **holidayEventEnabled**: Enable special events
- **eventStartTime**: Event start time (Unix timestamp)
- **eventEndTime**: Event end time (Unix timestamp)

### Analytics
- **detailedAnalyticsEnabled**: Enable detailed analytics tracking
- **analyticsSamplingRate**: Sampling rate (0.0 to 1.0)

### Debug Features
- **debugFeaturesEnabled**: Enable debug features in production
- **showDebugUI**: Show debug UI in production builds

## Version Update Flow

1. **RemoteConfigService** loads config at app startup
2. **GameInitFlow** checks if current version meets minimum requirement
3. **VersionUpdatePopup** is shown if update is required
4. **User clicks update** → App store opens
5. **User returns** → Popup remains (since app needs updating)

### Mandatory vs Optional Updates

```csharp
// Mandatory update (user cannot close popup)
PopupUtility.ShowVersionUpdatePopup(
    currentVersion: "1.0.0",
    requiredVersion: "1.1.0", 
    updateMessage: "Critical security update required",
    onUpdateClicked: OpenAppStore,
    onForceClose: null  // null = mandatory
);

// Optional update (user can dismiss)
PopupUtility.ShowVersionUpdatePopup(
    currentVersion: "1.0.0",
    requiredVersion: "1.1.0",
    updateMessage: "New features available!",
    onUpdateClicked: OpenAppStore,
    onForceClose: () => Debug.Log("User dismissed update")
);
```

## Local Caching

The system automatically caches the remote config locally:

- **Cache Location**: `Application.persistentDataPath/remote_config_cache.json`
- **Fallback Strategy**: Cached config → Default config
- **Cache Updates**: Automatically updated when new config is fetched
- **Offline Support**: Works offline using cached config

## Testing & Debug

### Debug Features

```csharp
// Force refresh config (useful in editor)
RemoteConfigManager.ForceRefresh();

// Log current configuration
RemoteConfigManager.LogConfiguration();

// Check service status
if (RemoteConfigManager.IsAvailable())
{
    Debug.Log("Remote config is ready");
}
```

### Editor Testing

1. **Enable debug logs** in RemoteConfigService inspector
2. **Use Force Refresh** context menu or debug UI button
3. **Check Unity Console** for detailed initialization logs
4. **Modify Firebase console** values and test refresh

### Testing Version Updates

1. **Set versionCode** in Firebase to a higher version than `Application.version`
2. **Force refresh** the config
3. **Restart the game** or trigger initialization
4. **Version popup** should appear

## Best Practices

### 1. Configuration Design
- **Use meaningful defaults** for all parameters
- **Group related settings** logically
- **Keep parameter names** clear and consistent
- **Document each parameter** with comments

### 2. Version Management
- **Use semantic versioning** (e.g., "1.2.3")
- **Test version comparisons** thoroughly
- **Provide clear update messages** to users
- **Consider gradual rollouts** for major updates

### 3. Feature Flags
- **Start with features disabled** by default
- **Test thoroughly** before enabling for all users
- **Have rollback plans** for problematic features
- **Monitor metrics** after enabling features

### 4. Performance
- **Cache aggressively** for offline scenarios
- **Use reasonable fetch timeouts** (30-60 seconds)
- **Don't block app startup** on config loading
- **Provide sensible fallbacks** for all values

### 5. Error Handling
- **Always provide fallback values**
- **Handle network failures gracefully**
- **Log errors** but don't crash
- **Continue with cached/default config** on errors

## Troubleshooting

### Common Issues

**Config not loading:**
- Check Firebase project configuration
- Verify internet connectivity
- Check Firebase console parameter names
- Enable debug logs for detailed error info

**Version popup not showing:**
- Verify `versionCode` parameter exists in Firebase
- Check current `Application.version` vs remote version
- Ensure popup prefab is configured in PopupConfig
- Check PopupManager is properly initialized

**Values not updating:**
- Call `ForceRefresh()` or restart app
- Check fetch timeout settings
- Verify JSON format in Firebase console
- Check local cache file for corruption

### Debug Commands

```csharp
// Clear local cache (for testing)
System.IO.File.Delete(System.IO.Path.Combine(Application.persistentDataPath, "remote_config_cache.json"));

// Force config refresh
var service = ServiceLocator.TryResolve<RemoteConfigService>();
service?.ForceRefreshConfig();

// Check initialization status
Debug.Log($"Service initialized: {service?.IsInitialized}");
Debug.Log($"Firebase initialized: {service != null}");
```

## Integration Points

- **GameInitFlow**: Handles service initialization and version checking
- **ServiceLocator**: Provides dependency injection for the service
- **PopupSystem**: Shows version update popups
- **Currency System**: Can use max lives configuration
- **Ads System**: Can check if ads are enabled
- **Analytics**: Can use sampling rate configuration

This system provides a robust foundation for remote configuration management in your Unity game! 🚀