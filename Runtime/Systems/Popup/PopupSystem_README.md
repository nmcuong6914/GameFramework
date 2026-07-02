# Popup System with Type-Safe Enum### 4. **PopupManager**
- Manages popup lifecycle and stack
- Supports both direct calls and signal-based opening
- Handles resource management with AssetReference and Addressables
- Integrates with your existing SignalBus and ServiceLocator

### 5. **SignalBus & PopupSignal**
- Uses your existing Signal system and ServiceLocator
- Event-driven system for decoupled popup triggering
- Now uses `PopupType` enum instead of stringsure

## Overview
This popup system provides a flexible, type-safe way to manage popups using enums and signals, with full integration into your existing SignalBus and ServiceLocator architecture.

**Current Implementation Status**: This system includes the core popup management functionality. Helper utilities and example classes have been removed to keep the codebase lean and focused on the essential components.

## ✅ Key Features
- **Type-safe popup keys** using `PopupType` enum - no more string typos!
- **Signal-based architecture** integrated with your existing SignalBus
- **ServiceLocator integration** for dependency injection
- **AssetReference integration** for type-safe addressable asset loading
- **Stack-based popup management** with proper lifecycle handling
- **Event system** for loose coupling between components

## Key Components

### 1. **PopupType Enum**
```csharp
public enum PopupType
{
    WinLevel = 0
}
```

### 2. **ScreenData (Abstract Base Class)**
- All popup data classes must inherit from `ScreenData`
- Provides type safety and structure for popup-specific data

### 3. **PopupConfig (ScriptableObject)**
- Central configuration for all popups
- Maps `PopupType` enum values to `AssetReference` objects
- Create via: `Assets > Create > UI > Popup Configuration`

### 5. **PopupManager**
- Manages popup lifecycle and stack
- Supports both direct calls and signal-based opening
- Handles resource management with Addressables
- Integrates with your existing SignalBus and ServiceLocator

### 6. **SignalBus & PopupSignal**
- Uses your existing Signal system and ServiceLocator
- Event-driven system for decoupled popup triggering
- Now uses `PopupType` enum instead of strings

## Setup Instructions

### 1. Create PopupConfig
```csharp
// Create a PopupConfig asset in your project
// Assets > Create > UI > Popup Configuration
// Add entries for each popup with PopupType enum and AssetReference
```

### 2. Configure PopupManager
```csharp
// Assign the PopupConfig to PopupManager in inspector
// Ensure PopupRoot is set (will auto-create if null)
// Make sure SignalBus is registered in ServiceLocator (usually in a bootstrap script):
// ServiceLocator.Register<SignalBus>(new SignalBus());
```

### 3. Create Popup Data Classes
```csharp
// Win Level popup data class
public class WinLevelPopupData : ScreenData
{
    public int currentLevel;
    public int score;
    public int rewardCoins;
    public System.Action onNextLevel;
    
    public WinLevelPopupData(int currentLevel, int score, int rewardCoins, System.Action onNextLevel)
    {
        this.currentLevel = currentLevel;
        this.score = score;
        this.rewardCoins = rewardCoins;
        this.onNextLevel = onNextLevel;
    }
}

// Example of other popup data classes (implement as needed)
public class ConfirmationPopupData : ScreenData
{
    public string title;
    public string message;
    public System.Action onConfirm;
    public System.Action onCancel;
    
    public ConfirmationPopupData(string title, string message, System.Action onConfirm = null, System.Action onCancel = null)
    {
        this.title = title;
        this.message = message;
        this.onConfirm = onConfirm;
        this.onCancel = onCancel;
    }
}
```

### 4. Create Popup Classes
```csharp
// Win Level popup implementation (extends BasePopup from Assets/Scripts/UI/)
public class WinLevelPopup : BasePopup
{
    [Header("UI References")]
    [SerializeField] private Text levelText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Button nextLevelButton;
    
    private WinLevelPopupData popupData;
    
    private void Awake()
    {
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);
        }
    }
    
    public override void SetData(ScreenData screenData)
    {
        popupData = screenData as WinLevelPopupData;
        
        if (popupData != null)
        {
            if (levelText != null)
                levelText.text = $"Level {popupData.currentLevel} Complete!";
                
            if (scoreText != null)
                scoreText.text = $"Score: {popupData.score}";
        }
    }
    
    private void OnNextLevelClicked()
    {
        popupData?.onNextLevel?.Invoke();
        // Close popup after callback
        ClosePopup();
    }
    
    private async void ClosePopup()
    {
        var popupManager = PopupManager.Instance;
        if (popupManager != null)
        {
            await popupManager.ClosePopup<WinLevelPopup>();
        }
    }
    
    protected override async Task DoTransitionIn()
    {
        // Simple show - no animation for now
        gameObject.SetActive(true);
        await Task.Yield();
    }
    
    protected override async Task DoTransitionOut()
    {
        // Simple hide - no animation for now
        await Task.Yield();
        gameObject.SetActive(false);
    }
}
```

## Usage Examples

### Direct Usage (Type-Safe)
```csharp
// Open win level popup directly using enum
var data = new WinLevelPopupData(1, 100, 50, () => Debug.Log("Next Level")); // level, score, rewardCoins, callback
await PopupManager.Instance.OpenPopupByKey<BasePopup>(PopupType.WinLevel, data);

// Close specific popup
await PopupManager.Instance.ClosePopup<WinLevelPopup>();

// Close all popups
await PopupManager.Instance.CloseAllPopups();
```

### Signal-Based Usage (Recommended)
```csharp
// Get SignalBus from ServiceLocator
var signalBus = ServiceLocator.TryResolve<SignalBus>();
if (signalBus == null)
{
    Debug.LogError("SignalBus not found in ServiceLocator");
    return;
}

// Win level popup
var winLevelData = new WinLevelPopupData(
    currentLevel: 1,
    score: 100,
    rewardCoins: 50,
    onNextLevel: () => Debug.Log("Moving to next level")
);

// Fire signal using enum for type safety
signalBus.Fire(new PopupSignal(PopupType.WinLevel, winLevelData));
```

### Creating Helper Methods (Optional)
If you want to simplify popup usage, you can create your own helper methods:

```csharp
public static class PopupUtility
{
    public static void ShowWinLevelPopup(int currentLevel, int score, int rewardCoins, System.Action onNextLevel)
    {
        var signalBus = ServiceLocator.TryResolve<SignalBus>();
        if (signalBus != null)
        {
            var data = new WinLevelPopupData(currentLevel, score, rewardCoins, onNextLevel);
            signalBus.Fire(new PopupSignal(PopupType.WinLevel, data));
        }
    }
    
    public static void ShowConfirmationPopup(string title, string message, System.Action onConfirm = null, System.Action onCancel = null)
    {
        var signalBus = ServiceLocator.TryResolve<SignalBus>();
        if (signalBus != null)
        {
            var data = new ConfirmationPopupData(title, message, onConfirm, onCancel);
            signalBus.Fire(new PopupSignal(PopupType.ConfirmationPopup, data));
        }
    }
}
```



## Events
```csharp
// Subscribe to popup events
PopupManager.Instance.OnPopupOpened += (popup) => Debug.Log($"Popup opened: {popup.name}");
PopupManager.Instance.OnPopupClosed += (popup) => Debug.Log($"Popup closed: {popup.name}");
PopupManager.Instance.OnAllPopupsClosed += () => Debug.Log("All popups closed");

// Subscribe to specific popup transition events
myPopup.OnPopupTransitionInComplete += (popup) => Debug.Log("Transition in complete");
myPopup.OnPopupTransitionOutComplete += (popup) => Debug.Log("Transition out complete");
```

## Best Practices

1. **Use PopupType enum** instead of strings for complete type safety
2. **Always inherit from ScreenData** for popup-specific data
3. **Use PopupConfig** to manage popup mappings centrally
4. **Prefer signal-based approach** for decoupled architecture
5. **Implement proper transitions** in your BasePopup subclasses
6. **Handle async operations** properly when opening/closing popups
7. **Use events** for loose coupling between systems
8. **Clean up resources** - the system handles this automatically

## Type Safety Benefits

- **Compile-time checking**: No more runtime errors from typos
- **IntelliSense support**: Auto-completion when selecting popup types
- **Refactoring safety**: Rename enum values and update all references easily
- **Better maintainability**: Clear contract for available popup types

## Example PopupConfig Setup
```
Key: PopupType.WinLevel
AssetReference: [Drag your WinLevelPopup prefab here]
PopupTypeName: "WinLevelPopup"
```

This maps the WinLevel enum to an AssetReference pointing to your WinLevelPopup prefab. The AssetReference provides type safety and better integration with the Addressables system.

## Migration from String-Based System

If you're migrating from a string-based popup system:

1. **Update PopupConfig**: Change string keys to PopupType enum values
2. **Update calls**: Replace string literals with enum values
3. **Compile**: The compiler will help you find all places that need updates
4. **Test**: Verify all popups still work correctly

```csharp
// Old (string-based):
await PopupManager.Instance.OpenPopupByKey<BasePopup>("WinLevelPopup", data);

// New (type-safe):
await PopupManager.Instance.OpenPopupByKey<BasePopup>(PopupType.WinLevel, data);
```

## Notes

- **BasePopup Location**: The `BasePopup` class is located in `Assets/Scripts/UI/BasePopup.cs`, not in the Popup services folder
- **Core Implementation**: This system focuses on the core popup management functionality
- **Extensibility**: You can easily add your own helper methods and utilities as needed
- **Type Safety**: All popup operations use the `PopupType` enum for compile-time safety
- **AssetReference Benefits**: Using `AssetReference` instead of string paths provides:
  - Type safety at design time
  - Automatic validation in the Inspector
  - Better integration with Addressables system
  - Prevents runtime errors from invalid paths
- **Input Blocking**: The WinLevel popup automatically disables block input when shown and re-enables it when moving to the next level
- **Level Progression**: The popup handles level progression through the GameplayManager integration

## WinLevel Popup Integration

The WinLevel popup is integrated with the game flow as follows:

1. **Level Completion**: When `GameplayManager.OnLevelCompleted()` is called
2. **Input Disabled**: All block dragging is disabled via `BoardController.SetAllBlocksInputEnabled(false)`
3. **Popup Shown**: WinLevel popup is displayed using the signal system
4. **User Interaction**: Player clicks "Next Level" button
5. **Level Progression**: GameplayManager loads the next level
6. **Input Enabled**: Block dragging is re-enabled for the new level

This ensures a smooth game flow where players cannot interact with blocks while the popup is shown, preventing any unwanted actions during level transitions.

## File Structure
```
Assets/Scripts/Services/Popup/
├── PopupType.cs                    // Enum definition and extensions
├── PopupConfig.cs                  // ScriptableObject configuration
├── PopupManager.cs                 // Main popup management
├── PopupSignal.cs                  // Signal class for events
├── ScreenData.cs                   // Abstract base class for popup data
├── WinLevelPopupData.cs            // Win level popup data class
└── PopupSystem_README.md           // This documentation

Assets/Scripts/UI/
├── BasePopup.cs                    // Base popup class (located in UI folder)
└── WinLevelPopup.cs                // Win level popup implementation

Assets/Scripts/Gameplay/
└── GameplayManager.cs              // Handles level completion and popup triggering
```

This system provides a clean, type-safe, and extensible way to manage popups in your Unity project! 🎉
