# Currency Regeneration - Timer System Removal

## Overview
Updated the currency regeneration system to completely avoid conflicts with the timer system. Currency regeneration now relies purely on the internal `Currency.UpdateRegeneration()` mechanism, with `LivesManager` acting as a monitoring layer rather than a competing timer system.

## Changes Made

### 1. **Removed TimerManager Dependency**
- Removed `TimerManager` references from `LivesManager`
- Removed `TimerIDs.LIVES_REGENERATION` usage
- Removed `regenCancellation` (was for timer cancellation)

### 2. **Replaced Timer-Based Regeneration**

#### Before (Timer-Based System):
```csharp
// Old approach - competing with Currency.UpdateRegeneration()
var timer = timerManager.CreateNamedTimer(
    TimerIDs.LIVES_REGENERATION,
    livesConfig.regenerationInterval,
    OnRegenerationTick,
    null
);

private void OnRegenerationTick()
{
    livesCurrency.Add(livesConfig.regenerationAmount); // Manual addition
}
```

#### After (Currency-Based System):
```csharp
// New approach - leverages Currency.UpdateRegeneration()
private async UniTaskVoid RegenerationUpdateLoop(CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested && isRegenerationActive)
    {
        int previousAmount = livesCurrency.Amount;
        livesCurrency.UpdateRegeneration(); // Uses Currency's internal timing
        
        if (livesCurrency.Amount != previousAmount)
        {
            // Lives regenerated naturally through Currency system
        }
        
        await UniTask.Delay(TimeSpan.FromSeconds(updateInterval), cancellationToken);
    }
}
```

### 3. **New Architecture: Monitoring vs Control**

#### Previous Architecture (Conflicting):
```
┌─────────────────┐    ┌─────────────────┐
│   TimerManager  │    │ Currency.Update │
│   (External)    │    │ Regeneration    │
│                 │    │ (Internal)      │
│ Controls timing │◄──►│ Controls timing │  ❌ CONFLICT
│ Calls Add()     │    │ Updates amount  │
└─────────────────┘    └─────────────────┘
```

#### New Architecture (Harmonious):
```
┌─────────────────┐    ┌─────────────────┐
│   LivesManager  │    │ Currency.Update │
│   (Monitor)     │    │ Regeneration    │
│                 │    │ (Controller)    │
│ Detects changes │◄───│ Controls timing │  ✅ NO CONFLICT
│ Fires events    │    │ Updates amount  │
└─────────────────┘    └─────────────────┘
```

## Key Implementation Details

### 1. **Update Loop with Monitoring**
```csharp
// Check every second for regeneration changes
private float updateInterval = 1f;

// Store previous amount to detect changes
int previousAmount = livesCurrency.Amount;
livesCurrency.UpdateRegeneration();

if (livesCurrency.Amount != previousAmount)
{
    // Natural regeneration occurred - fire events
    signalBus?.Fire(new CurrencyChangedSignal(CurrencyType.Lives, previousAmount, livesCurrency.Amount));
}
```

### 2. **Proper Cancellation Handling**
```csharp
private CancellationTokenSource updateCancellation;

// Start monitoring
updateCancellation = new CancellationTokenSource();
RegenerationUpdateLoop(updateCancellation.Token).Forget();

// Stop monitoring
updateCancellation?.Cancel();
updateCancellation?.Dispose();
```

### 3. **Event-Driven UI Updates**
- `LivesManager` still fires `OnLivesChanged` events
- UI systems continue to work without changes
- Signal system continues to broadcast currency changes

## Benefits

### ✅ **No Timer Conflicts**
- Only one system controls regeneration timing: `Currency.UpdateRegeneration()`
- No competing timer logic
- No synchronization issues

### ✅ **Maintains Offline Generation**
- `Currency.UpdateRegeneration()` continues to handle offline time calculation
- No interference with offline generation system
- Saved state timing remains accurate

### ✅ **Preserved Functionality**
- All existing features continue to work
- UI updates through events
- Debug logging and monitoring
- Force start/stop capabilities

### ✅ **Simplified Architecture**
- Clear separation of concerns
- `Currency` = timing controller
- `LivesManager` = change monitor/event broadcaster
- No ambiguity about ownership

## Flow Comparison

### Old Flow (Conflicting):
1. `LivesManager` starts timer
2. Timer fires → calls `livesCurrency.Add()`
3. Meanwhile, `Currency.UpdateRegeneration()` also runs
4. Both systems affect `lastUpdateTicks`
5. **Timing conflicts** cause regeneration to stop

### New Flow (Harmonious):
1. `LivesManager` starts monitoring loop
2. Loop calls `livesCurrency.UpdateRegeneration()`
3. `Currency` handles all timing internally
4. If amount changes, `LivesManager` detects and fires events
5. **No conflicts** - continuous regeneration

## Testing Scenarios

### Scenario 1: Normal Regeneration
- Player uses lives (amount < max)
- `LivesManager` starts monitoring
- `Currency.UpdateRegeneration()` generates lives based on internal timing
- `LivesManager` detects changes and fires events
- Process continues until max capacity

### Scenario 2: Offline Generation
- Player closes game with lives < max
- On restart, `Currency.UpdateRegeneration()` processes offline time
- `LivesManager` resumes monitoring from current state
- No timing conflicts or double-processing

### Scenario 3: Game Restart with Partial Timer
- Player has lives < max with saved regeneration timer
- `Currency.GetTimeToNextRegeneration()` provides accurate remaining time
- `LivesManager` monitoring begins, detects when regeneration occurs
- Timing preserved perfectly through `Currency` system

## Files Modified

### LivesManager.cs Changes:
- **Removed**: `TimerManager` dependency, timer creation/management
- **Added**: `RegenerationUpdateLoop()` with update interval monitoring
- **Changed**: All timer methods replaced with monitoring methods
- **Preserved**: All public interfaces, events, and debugging features

### No Changes Needed:
- `Currency.cs` - existing regeneration system works perfectly
- `PlayerData.cs` - offline generation continues to work
- UI systems - continue to receive events as before

## Performance Impact

### ✅ **Improved Performance**
- No timer creation/destruction overhead
- Single update loop vs multiple timer callbacks
- Reduced memory allocations (no timer objects)
- More predictable execution timing

### ✅ **Reduced Complexity**
- Single async loop vs complex timer management
- Cleaner cancellation handling
- No timer state synchronization needed

The new system is simpler, more reliable, and completely avoids timer conflicts while maintaining all existing functionality.
