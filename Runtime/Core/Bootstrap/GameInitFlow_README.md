# GameInitFlow System Documentation

## Overview

The GameInitFlow system provides controlled initialization of all game services in the correct order. This ensures that services which depend on other services (like LivesManager depending on PlayerDataManager) are initialized properly without race conditions.

## Key Features

- ✅ **Controlled Initialization Order**: Services are initialized in the correct dependency order
- ✅ **PlayerDataManager First**: Ensures data is loaded before dependent services start
- ✅ **Dependency Waiting**: Services wait for their dependencies to be ready
- ✅ **Async Initialization**: All initialization is properly async with UniTask
- ✅ **Debug Logging**: Clear logging shows initialization progress
- ✅ **Error Handling**: Graceful error handling with fallbacks

## Initialization Order

1. **Core Services** (Awake)
   - SignalBus
   - TimerManager
   - ActionViewManager
   - Basic service registration

2. **PlayerDataManager** (Start - Phase 1)
   - Loads/creates player data
   - Initializes currency system
   - Sets up save system

3. **Data-Dependent Services** (Start - Phase 2)
   - LivesManager (waits for PlayerDataManager)
   - Any other services that need player data

4. **Remaining Services** (Start - Phase 3)
   - AdsManager
   - PoolManager verification
   - Other independent services

5. **Completion**
   - Logs all initialized services
   - Game is ready for gameplay

## Usage

### For GameInitFlow Setup

```csharp
public class GameInitFlow : MonoBehaviour
{
    [Header("Core Services")]
    [SerializeField] private AssetManager assetManager;
    [SerializeField] private PoolManager poolManager;
    [SerializeField] private VFXController vFXController;
    [SerializeField] private PlayerDataManager playerDataManager;
    [SerializeField] private LivesManager livesManager;
    [SerializeField] private GameUI gameUI;
    
    [Header("Ads Configuration")]
    [SerializeField] private AdsConfig adsConfig;
}
```

### For Other Systems Waiting for Initialization

```csharp
public class MyGameSystem : MonoBehaviour
{
    private async void Start()
    {
        // Wait for GameInitFlow to complete
        await WaitForGameInitialization();
        
        // Now safely use any service
        InitializeMySystem();
    }

    private async UniTask WaitForGameInitialization()
    {
        var gameInitFlow = FindObjectOfType<GameInitFlow>();
        while (!gameInitFlow.IsInitializationComplete)
        {
            await UniTask.NextFrame();
        }
    }
}
```

### Service Registration Pattern

Services should follow this pattern:

```csharp
public class MyService : MonoBehaviour
{
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    private void Awake()
    {
        // Only basic setup here
        // No dependencies or async operations
    }

    private void Start()
    {
        // NOTE: Don't auto-initialize here anymore
        // Let GameInitFlow control initialization
    }

    public async UniTask InitializeAsync()
    {
        // Wait for dependencies
        var dependency = ServiceLocator.TryResolve<SomeDependency>();
        while (!dependency.IsInitialized)
        {
            await UniTask.NextFrame();
        }

        // Initialize this service
        // ... initialization code ...

        isInitialized = true;
    }
}
```

## Service Dependencies

### Current Dependency Chain

```
PlayerDataManager (no dependencies)
    ↓
LivesManager (depends on PlayerDataManager)
    ↓
GameplayManager (depends on PlayerDataManager + LivesManager)
```

### Services That Don't Need Controlled Initialization

- **SignalBus**: Core communication system
- **TimerManager**: Core timing system
- **AssetManager**: Self-contained asset loading
- **VFXController**: Uses PoolManager but can work without it
- **PoolManager**: Self-contained object pooling

### Services That Need Controlled Initialization

- **PlayerDataManager**: Must initialize first (loads save data)
- **LivesManager**: Needs PlayerDataManager for currency configuration
- **GameplayManager**: Needs PlayerDataManager for level progression
- **Any UI systems**: Need PlayerDataManager for currency display

## Benefits

### Before GameInitFlow
- Services initialized in random order (Unity Start() calls)
- Race conditions between dependent services
- Manual waiting loops in each service
- Hard to debug initialization issues
- No central control over startup sequence

### After GameInitFlow
- Predictable initialization order
- No race conditions
- Central control and logging
- Easy to debug and modify
- Clean service dependencies

## Debug Features

Enable `debugLogs` in GameInitFlow to see:

```
[GameInitFlow] Registering core services...
[GameInitFlow] ✓ SignalBus registered
[GameInitFlow] ✓ TimerManager registered
[GameInitFlow] Starting game systems initialization...
[GameInitFlow] Initializing PlayerDataManager...
[GameInitFlow] ✓ PlayerDataManager initialized
[GameInitFlow] Initializing LivesManager...
[GameInitFlow] ✓ LivesManager initialized
[GameInitFlow] === Game Systems Initialization Complete ===
[GameInitFlow] Initialized services: PlayerDataManager, LivesManager, AdsManager
```

## Integration with Existing Code

### GameplayManager Changes

GameplayManager already follows the correct pattern:

```csharp
private async UniTaskVoid InitializeGameplayAsync()
{
    // Wait for PlayerDataManager if it exists
    var playerDataManager = ServiceLocator.TryResolve<PlayerDataManager>();
    if (playerDataManager != null)
    {
        while (!playerDataManager.IsInitialized)
        {
            await UniTask.NextFrame();
        }
        // Use playerDataManager safely
    }
}
```

### No Breaking Changes

- Existing services continue to work
- ServiceLocator pattern unchanged
- Event system unchanged
- Only initialization order is now controlled

## Best Practices

1. **Service Registration**: Register services in Awake() or GameInitFlow
2. **Initialization**: Use InitializeAsync() methods for complex setup
3. **Dependencies**: Always check IsInitialized before using dependencies
4. **Error Handling**: Graceful fallbacks if dependencies aren't available
5. **Logging**: Use debug logging to track initialization progress

## Troubleshooting

### Service Not Initialized

```csharp
var service = ServiceLocator.TryResolve<MyService>();
if (service == null || !service.IsInitialized)
{
    Debug.LogWarning("Service not ready, waiting...");
    // Wait or implement fallback
}
```

### Missing Dependencies

- Check GameInitFlow has all required services assigned
- Verify services implement IsInitialized property
- Check initialization order in GameInitFlow phases

### Timing Issues

- Use await instead of polling where possible
- Implement proper IsInitialized checks
- Follow the GameInitFlow pattern for new services

## Future Enhancements

- [ ] Scene-based initialization profiles
- [ ] Initialization progress UI
- [ ] Service health monitoring
- [ ] Hot-reload support for development
- [ ] Dependency injection container integration
