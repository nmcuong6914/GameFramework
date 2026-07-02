# Analytics Service System

A comprehensive analytics service system for Unity that supports multiple analytics providers with the ability to enable/disable logging in the editor.

## Features

- **Multi-Provider Support**: Currently supports Amplitude and Firebase Analytics
- **Interface-Based Design**: Easy to add new analytics providers
- **Editor Controls**: Enable/disable analytics in editor mode
- **Debug Logging**: Configurable debug logs for development
- **Automatic Event Tracking**: Integrates with your game's signal system
- **Settings Management**: ScriptableObject-based configuration

## Setup Instructions

### 1. Create Analytics Settings

1. Go to `Analytics > Create Analytics Settings` in the Unity menu
2. Configure the settings in the `Resources/AnalyticsSettings.asset` file:
   - **Enable In Editor**: Whether to send analytics events while in editor
   - **Enable Debug Logs**: Show detailed logs for debugging
   - **Enable Amplitude/Firebase**: Toggle individual services
   - **API Keys**: Set your Amplitude API key

### 2. Add Analytics Manager to Scene

1. Go to `Analytics > Setup Analytics Manager` in the Unity menu
2. This will create a GameObject with `AnalyticsManager` and `GameAnalyticsTracker` components

### 3. Install Required SDKs

#### For Amplitude:
1. Download the Amplitude Unity SDK from [Amplitude's GitHub](https://github.com/amplitude/Amplitude-Unity)
2. Import the package into your project
3. Add `AMPLITUDE_UNITY` to your project's Scripting Define Symbols

#### For Firebase:
1. Download Firebase Unity SDK from [Firebase Console](https://firebase.google.com/docs/unity/setup)
2. Import the Firebase Analytics package
3. Add `FIREBASE_ANALYTICS` to your project's Scripting Define Symbols
4. Add your `google-services.json` (Android) and `GoogleService-Info.plist` (iOS) files

### 4. Configure Scripting Define Symbols

In `Player Settings > Other Settings > Scripting Define Symbols`, add:
- `AMPLITUDE_UNITY` (if using Amplitude)
- `FIREBASE_ANALYTICS` (if using Firebase)

## Usage

### Basic Event Tracking

```csharp
// Track simple event
AnalyticsManager.Instance.TrackEvent("button_clicked");

// Track event with parameters
var parameters = new Dictionary<string, object>
{
    { "button_name", "play_button" },
    { "screen", "main_menu" }
};
AnalyticsManager.Instance.TrackEvent("ui_interaction", parameters);
```

### Using Predefined Events

```csharp
// Use predefined game events
var levelParams = GameAnalyticsEvents.CreateLevelParams(1, "Level_1", 1000, 45.5f, 1);
AnalyticsManager.Instance.TrackEvent(GameAnalyticsEvents.LEVEL_COMPLETED, levelParams);

// Currency events
var currencyParams = GameAnalyticsEvents.CreateCurrencyParams("coins", 100, "level_reward");
AnalyticsManager.Instance.TrackEvent(GameAnalyticsEvents.CURRENCY_EARNED, currencyParams);
```

### User Properties

```csharp
// Set user properties
AnalyticsManager.Instance.SetUserProperty("player_level", 5);
AnalyticsManager.Instance.SetUserId("unique_user_id");

// Set multiple properties
var userProps = new Dictionary<string, object>
{
    { "vip_status", true },
    { "total_playtime", 3600 }
};
AnalyticsManager.Instance.SetUserProperties(userProps);
```

## Automatic Event Tracking

The `GameAnalyticsTracker` component automatically tracks events based on your game signals:

- **Level Events**: Level loaded, completed, failed, restarted
- **Gameplay Events**: Block removed, gate passed, game won/lost
- **Session Events**: Game initialization

## Adding New Analytics Providers

1. Create a new class inheriting from `BaseAnalyticsService`:

```csharp
public class CustomAnalyticsService : BaseAnalyticsService
{
    public override string ServiceName => "CustomProvider";
    
    protected override void InitializeService()
    {
        // Initialize your analytics SDK
    }
    
    protected override void TrackEventInternal(string eventName, Dictionary<string, object> parameters)
    {
        // Implement event tracking
    }
    
    // Implement other abstract methods...
}
```

2. Add the service to `AnalyticsManager.InitializeServices()`:

```csharp
if (AnalyticsSettings.Instance.EnableCustomProvider)
{
    AddService(new CustomAnalyticsService());
}
```

3. Add corresponding settings to `AnalyticsSettings.cs`

## Configuration Options

### AnalyticsSettings Properties

- **EnableInEditor**: Send analytics events while in Unity editor
- **EnableDebugLogs**: Show detailed debug logs
- **EnableAmplitude**: Enable/disable Amplitude service
- **EnableFirebase**: Enable/disable Firebase service
- **AmplitudeApiKey**: Your Amplitude project API key

## Predefined Events

The system includes predefined events for common game actions:

### Level Events
- `level_started`
- `level_completed`
- `level_failed`
- `level_restarted`

### Gameplay Events
- `block_removed`
- `gate_passed`
- `game_won`
- `game_lost`

### UI Events
- `ui_button_clicked`
- `popup_opened`
- `popup_closed`

### Economy Events
- `currency_earned`
- `currency_spent`
- `reward_received`

## Testing

1. Use `Analytics > Test Analytics Event` to send a test event
2. Check the console logs to verify events are being sent
3. Enable debug logs in AnalyticsSettings to see detailed information

## Troubleshooting

### Events Not Sending
1. Check that analytics services are enabled in AnalyticsSettings
2. Verify that required SDKs are imported and scripting defines are set
3. Enable debug logs to see detailed error messages

### Editor Mode Issues
1. Make sure "Enable In Editor" is checked in AnalyticsSettings
2. Check console for initialization errors

### Missing Dependencies
1. Ensure Firebase/Amplitude SDKs are properly imported
2. Add the appropriate scripting define symbols
3. Check that API keys are correctly set

## Best Practices

1. **Event Naming**: Use consistent, descriptive event names
2. **Parameter Consistency**: Keep parameter names and types consistent across events
3. **Data Privacy**: Ensure compliance with data protection regulations
4. **Performance**: Avoid sending too many events in rapid succession
5. **Testing**: Always test analytics integration thoroughly before release

## Support

For issues or questions:
1. Check the Unity console for error messages
2. Verify SDK installation and configuration
3. Review analytics provider documentation
4. Check network connectivity for event transmission
