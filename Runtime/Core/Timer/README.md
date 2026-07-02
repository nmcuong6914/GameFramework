# TimerManager Documentation

## Overview

The `TimerManager` is a comprehensive timer system for Unity that manages individual Timer instances. Each Timer runs itself using UniTask for efficient async operation without MonoBehaviour dependency. The system provides centralized management for multiple timers with rich event callbacks and UI integration.

## Key Features

- ✅ **Self-Running Timers**: Each Timer instance runs itself using UniTask threads
- ✅ **Event-Driven**: Rich event system for timer lifecycle (OnStart, OnTick, OnComplete, etc.)
- ✅ **Timer Management**: Create, start, stop, pause, resume, and reset timers by name
- ✅ **UI Integration**: Easy UI updates via timer event callbacks (no Update() polling)
- ✅ **Timer Constants**: Use TimerIDs class to avoid magic strings
- ✅ **Memory Efficient**: Automatic cleanup and disposal of timers
- ✅ **Flexible Direction**: Support for countdown and countup timers
- ✅ **Loop Support**: Timers can automatically restart when complete

## Quick Start

### Basic Timer Usage

```csharp
// Create a Timer instance that runs itself
var timer = new Timer(60f, TimerDirection.Down, autoStart: true);

// Subscribe to events
timer.OnTimerComplete += () => Debug.Log("Timer completed!");
timer.OnTimerTick += (currentTime) => UpdateUI(currentTime);

// Manual control
timer.Start();
timer.Pause();
timer.Resume();
timer.Stop();
```

### Using TimerManager with Named Timers

```csharp
// Get TimerManager from ServiceLocator (registered in GameInitFlow)
var timerManager = ServiceLocator.Resolve<TimerManager>();

// Create and start a named timer using TimerIDs constants
const string LEVEL_TIMER = "LevelTimer"; // TimerIDs.LEVEL_TIMER
var timer = timerManager.CreateNamedTimer(
    LEVEL_TIMER, 
    120f, 
    onComplete: () => Debug.Log("Level time expired!"),
    onTick: (currentTime) => UpdateTimerUI(currentTime)
);

// Start the timer (it will run itself)
timer.Start();

// Control timers by name
timerManager.PauseTimer(LEVEL_TIMER);
timerManager.ResumeTimer(LEVEL_TIMER);
timerManager.StopTimer(LEVEL_TIMER);

// Get timer information
float remaining = timerManager.GetRemainingTime(LEVEL_TIMER);
float total = timerManager.GetTotalDuration(LEVEL_TIMER);
bool isRunning = timerManager.IsTimerRunning(LEVEL_TIMER);
```

### UI Integration Example

```csharp
public class TimePanelUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeText;
    private TimerManager timerManager;

    private void Start()
    {
        timerManager = ServiceLocator.Resolve<TimerManager>();
        
        // Subscribe to timer manager events
        timerManager.OnNamedTimerStart += OnTimerStarted;
    }

    private void OnTimerStarted(string timerName)
    {
        const string LEVEL_TIMER = "LevelTimer"; // TimerIDs.LEVEL_TIMER
        
        if (timerName == LEVEL_TIMER)
        {
            var timer = timerManager.GetTimer(LEVEL_TIMER);
            if (timer != null)
            {
                // Subscribe to timer tick events for UI updates
                timer.OnTimerTick += OnTimerTick;
                timer.OnTimerComplete += OnTimerComplete;
            }
        }
    }

    private void OnTimerTick(float currentTime)
    {
        const string LEVEL_TIMER = "LevelTimer"; // TimerIDs.LEVEL_TIMER
        var timer = timerManager.GetTimer(LEVEL_TIMER);
        if (timer != null)
        {
            // Update UI with formatted time
            int minutes = Mathf.FloorToInt(timer.RemainingTime / 60);
            int seconds = Mathf.FloorToInt(timer.RemainingTime % 60);
            timeText.text = $"{minutes:00}:{seconds:00}";
            
            // Change color when time is critical
            if (timer.RemainingTime <= 10f)
            {
                timeText.color = Color.red;
            }
        }
    }

    private void OnTimerComplete()
    {
        timeText.text = "00:00";
        timeText.color = Color.red;
    }
}
```

## Timer System Architecture

### Timer Class
- **Self-Running**: Uses UniTask to run its own update loop
- **Events**: OnTimerStart, OnTimerTick, OnTimerComplete, OnTimerPause, OnTimerResume, OnTimerReset
- **Properties**: RemainingTime, ElapsedTime, Progress, IsRunning, IsPaused, IsComplete
- **Control**: Start(), Stop(), Pause(), Resume(), Reset()

### TimerManager Class
- **Timer Creation**: CreateNamedTimer() creates and registers Timer instances
- **Timer Control**: StartTimer(), StopTimer(), PauseTimer(), ResumeTimer(), ResetTimer()
- **Information**: GetTimer(), GetRemainingTime(), GetTotalDuration(), IsTimerRunning()
- **Events**: OnNamedTimerStart, OnNamedTimerComplete

### TimerIDs Class
- **Constants**: LEVEL_TIMER, HINT_COOLDOWN, SHUFFLE_COOLDOWN, LIVES_REGENERATION
- **Purpose**: Avoid magic strings and provide centralized timer ID management

## Migration from Old System

### Before (Manual Update)
```csharp
// Old way - manual polling in Update()
private void Update()
{
    if (timerActive)
    {
        remainingTime -= Time.deltaTime;
        UpdateUI(remainingTime);
        
        if (remainingTime <= 0)
        {
            OnTimerExpired();
        }
    }
}
```

### After (Event-Driven)
```csharp
// New way - event-driven, no Update() needed
private void Start()
{
    var timer = timerManager.CreateNamedTimer("LevelTimer", 120f, OnTimerExpired);
    timer.OnTimerTick += UpdateUI;
    timer.Start(); // Timer runs itself
}
```

## Key Features

- ✅ **UniTask-Based**: Efficient async timing without MonoBehaviour overhead
- ✅ **Dependency Injectable**: No singleton pattern, works with dependency injection
- ✅ **Automatic Callbacks**: Timers execute callbacks when completed, no manual status checking
- ✅ **Cancellation Support**: Full cancellation token support for clean timer management
- ✅ **Memory Efficient**: Automatic cleanup and disposal of completed timers
- ✅ **Thread Safe**: Uses CancellationTokenSource for safe async operations
- ✅ **Generic & Reusable**: No game-specific logic, pure timer functionality

## Quick Start

### Basic Timer Usage

```csharp
// Create TimerManager instance (dependency injectable)
var timerManager = new TimerManager(debugTimers: true);

// Simple timer with callback
await timerManager.CreateTimerAsync(5f, () => 
{
    Debug.Log("Timer completed!");
});

// Named timer for easy cancellation
var cancellationToken = new CancellationTokenSource();
timerManager.CreateNamedTimerAsync("MyTimer", 10f, () => 
{
    Debug.Log("Named timer completed!");
}, cancellationToken.Token).Forget();

// Cancel the named timer
timerManager.CancelNamedTimer("MyTimer");
```

### Dependency Injection Setup

```csharp
public class GameManager : MonoBehaviour
{
    private TimerManager timerManager;
    
    private void Start()
    {
        // Initialize and register as dependency
        timerManager = new TimerManager(debugTimers: true);
        ServiceLocator.Register(timerManager);
    }
    
    private void OnDestroy()
    {
        // Cleanup when done
        timerManager?.Dispose();
    }
}
```

## Core Architecture

### TimerManager Class

```csharp
public class TimerManager : IDisposable
{
    // Constructor
    public TimerManager(bool debugTimers = false)
    
    // Core Methods
    public async UniTask CreateTimerAsync(float duration, Action onComplete = null, CancellationToken cancellationToken = default)
    public async UniTask CreateNamedTimerAsync(string name, float duration, Action onComplete = null, CancellationToken cancellationToken = default)
    
    // Management Methods
    public void CancelNamedTimer(string name)
    public bool IsTimerRunning(string name)
    public void CancelAllTimers()
    
    // Specialized Methods
    public async UniTask CreateCooldownTimerAsync(string itemId, float cooldownDuration, Action onCooldownComplete = null, CancellationToken cancellationToken = default)
    public bool IsOnCooldown(string itemId)
    public void CancelCooldown(string itemId)
    
    public async UniTask CreateRegenerationTimerAsync(string resourceType, float regenerationInterval, Action onRegenerate = null, CancellationToken cancellationToken = default)
    public void StopRegeneration(string resourceType)
    
    // Utilities
    public List<string> GetAllActiveTimers()
    public void Dispose()
    
    // Events
    public event Action<string> OnNamedTimerComplete;
    public event Action<string> OnNamedTimerStart;
    public event Action<string> OnNamedTimerCreated;
}
```

## API Reference

### Core Timer Methods

#### CreateTimerAsync
```csharp
async UniTask CreateTimerAsync(float duration, Action onComplete = null, CancellationToken cancellationToken = default)
```
Creates an anonymous timer that executes a callback when completed.

**Parameters:**
- `duration`: Timer duration in seconds
- `onComplete`: Callback executed when timer finishes
- `cancellationToken`: Token for manual cancellation

**Usage:**
```csharp
await timerManager.CreateTimerAsync(3f, () => Debug.Log("3 seconds elapsed!"));
```

#### CreateNamedTimerAsync
```csharp
async UniTask CreateNamedTimerAsync(string name, float duration, Action onComplete = null, CancellationToken cancellationToken = default)
```
Creates a named timer that can be cancelled by name.

**Parameters:**
- `name`: Unique identifier for the timer
- `duration`: Timer duration in seconds
- `onComplete`: Callback executed when timer finishes
- `cancellationToken`: Token for manual cancellation

**Usage:**
```csharp
// Fire and forget
timerManager.CreateNamedTimerAsync("LevelTimer", 120f, OnLevelTimeUp).Forget();

// Or await it
await timerManager.CreateNamedTimerAsync("BossPhase", 30f, StartNextPhase);
```

### Management Methods

#### CancelNamedTimer
```csharp
void CancelNamedTimer(string name)
```
Cancels a named timer before completion.

#### IsTimerRunning
```csharp
bool IsTimerRunning(string name)
```
Checks if a named timer is currently active.

#### CancelAllTimers
```csharp
void CancelAllTimers()
```
Cancels all active timers immediately.

### Specialized Timer Methods

#### CreateCooldownTimerAsync
```csharp
async UniTask CreateCooldownTimerAsync(string itemId, float cooldownDuration, Action onCooldownComplete = null, CancellationToken cancellationToken = default)
```
Creates a cooldown timer for items/abilities.

#### IsOnCooldown / CancelCooldown
```csharp
bool IsOnCooldown(string itemId)
void CancelCooldown(string itemId)
```
Check cooldown status or cancel cooldown for an item.

#### CreateRegenerationTimerAsync
```csharp
async UniTask CreateRegenerationTimerAsync(string resourceType, float regenerationInterval, Action onRegenerate = null, CancellationToken cancellationToken = default)
```
Creates a looping timer for resource regeneration.

## Usage Examples

### Example 1: Level Timer (Modern Approach)
```csharp
public class GameplayManager : MonoBehaviour
{
    private TimerManager timerManager;
    private CancellationTokenSource levelTimerCancellation;
    
    private void Start()
    {
        timerManager = ServiceLocator.Get<TimerManager>();
    }
    
    private void StartLevel()
    {
        levelTimerCancellation = new CancellationTokenSource();
        
        // Start level timer with automatic callback
        timerManager.CreateNamedTimerAsync(
            "LevelTimer", 
            120f, 
            OnLevelTimeExpired,
            levelTimerCancellation.Token
        ).Forget();
    }
    
    private void OnLevelTimeExpired()
    {
        Debug.Log("Level time expired!");
        // Handle level failure automatically
        HandleLevelFailure();
    }
    
    private void CompleteLevel()
    {
        // Cancel timer when level is completed
        timerManager.CancelNamedTimer("LevelTimer");
        levelTimerCancellation?.Dispose();
    }
    
    private void OnDestroy()
    {
        levelTimerCancellation?.Cancel();
        levelTimerCancellation?.Dispose();
    }
}
```

### Example 2: Booster Cooldown System
```csharp
public class BoosterManager : MonoBehaviour
{
    private TimerManager timerManager;
    
    private void Start()
    {
        timerManager = ServiceLocator.Get<TimerManager>();
    }
    
    public async void UseBooster(string boosterId)
    {
        if (timerManager.IsOnCooldown(boosterId))
        {
            Debug.Log($"Booster {boosterId} is on cooldown!");
            return;
        }
        
        // Use the booster
        ApplyBoosterEffect(boosterId);
        
        // Start cooldown with automatic callback
        await timerManager.CreateCooldownTimerAsync(
            boosterId, 
            30f, 
            () => OnBoosterReady(boosterId)
        );
    }
    
    private void OnBoosterReady(string boosterId)
    {
        Debug.Log($"Booster {boosterId} is ready!");
        UpdateBoosterUI(boosterId, true);
    }
}
```

### Example 3: Lives Regeneration
```csharp
public class LivesManager : MonoBehaviour
{
    [SerializeField] private int maxLives = 5;
    [SerializeField] private float regenInterval = 300f; // 5 minutes
    
    private int currentLives;
    private TimerManager timerManager;
    private CancellationTokenSource regenCancellation;
    
    private void Start()
    {
        timerManager = ServiceLocator.Get<TimerManager>();
        currentLives = maxLives;
    }
    
    private void StartLivesRegeneration()
    {
        if (currentLives >= maxLives) return;
        
        regenCancellation = new CancellationTokenSource();
        
        // Start regeneration loop
        timerManager.CreateRegenerationTimerAsync(
            "Lives",
            regenInterval,
            OnLifeRegenerated,
            regenCancellation.Token
        ).Forget();
    }
    
    private void OnLifeRegenerated()
    {
        currentLives++;
        Debug.Log($"Life regenerated! Current lives: {currentLives}");
        
        if (currentLives >= maxLives)
        {
            timerManager.StopRegeneration("Lives");
        }
        
        UpdateLivesUI();
    }
    
    public void UseLive()
    {
        if (currentLives > 0)
        {
            currentLives--;
            UpdateLivesUI();
            
            // Start regeneration if needed
            if (currentLives < maxLives)
            {
                StartLivesRegeneration();
            }
        }
    }
    
    private void OnDestroy()
    {
        regenCancellation?.Cancel();
        regenCancellation?.Dispose();
    }
}
```

### Example 4: Multiple Sequential Timers
```csharp
public class GameSequenceManager : MonoBehaviour
{
    private TimerManager timerManager;
    
    private async void StartGameSequence()
    {
        timerManager = ServiceLocator.Get<TimerManager>();
        
        // Sequential timers with automatic flow
        await timerManager.CreateTimerAsync(3f, () => Debug.Log("Get Ready!"));
        await timerManager.CreateTimerAsync(2f, () => Debug.Log("Set!"));
        await timerManager.CreateTimerAsync(1f, () => Debug.Log("Go!"));
        
        StartGameplay();
    }
}
```

## Best Practices

### 1. Use Dependency Injection
```csharp
// Good: Inject TimerManager
public class GameSystem
{
    private readonly TimerManager timerManager;
    
    public GameSystem(TimerManager timerManager)
    {
        this.timerManager = timerManager;
    }
}

// Avoid: Creating multiple instances
var timer1 = new TimerManager();
var timer2 = new TimerManager(); // Unnecessary duplication
```

### 2. Proper Cancellation Token Management
```csharp
// Good: Proper cancellation token lifecycle
private CancellationTokenSource timerCancellation;

private void StartTimer()
{
    timerCancellation = new CancellationTokenSource();
    timerManager.CreateNamedTimerAsync("MyTimer", 5f, OnComplete, timerCancellation.Token).Forget();
}

private void OnDestroy()
{
    timerCancellation?.Cancel();
    timerCancellation?.Dispose(); // Important!
}
```

### 3. Use Callbacks for Automatic Execution
```csharp
// Good: Timer executes callback automatically
await timerManager.CreateTimerAsync(5f, () => 
{
    // This executes automatically when timer finishes
    OnTimerComplete();
});

// Avoid: Manual polling (old approach)
while (someTimer.IsRunning)
{
    await UniTask.Yield();
}
```

### 4. Named Timers for Important Systems
```csharp
// Good: Use descriptive names for system timers
timerManager.CreateNamedTimerAsync("LevelTimer", 120f, OnLevelFailed).Forget();
timerManager.CreateNamedTimerAsync("BossInvulnerability", 3f, EndInvulnerability).Forget();

// Good: Easy cancellation
timerManager.CancelNamedTimer("LevelTimer");
```

## Migration from Old Timer Systems

### Before (MonoBehaviour + Update)
```csharp
public class OldTimerManager : MonoBehaviour
{
    private float timer;
    private bool isRunning;
    
    private void Update()
    {
        if (isRunning)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                isRunning = false;
                OnTimerComplete();
            }
        }
    }
}
```

### After (UniTask + Callbacks)
```csharp
public class NewTimerManager : IDisposable
{
    public async UniTask CreateTimerAsync(float duration, Action onComplete = null)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(duration));
        onComplete?.Invoke(); // Automatic callback execution
    }
}
```

## Performance Benefits

1. **No Update() Overhead**: Uses UniTask instead of MonoBehaviour.Update()
2. **Memory Efficient**: Automatic cleanup of completed timers
3. **Async/Await**: Modern C# patterns, no coroutine overhead
4. **Cancellation Tokens**: Proper async cancellation support
5. **Event-Driven**: Callbacks execute automatically, no polling needed

## Integration Notes

- Works with any dependency injection system
- Requires UniTask package (Cysharp.Threading.Tasks)
- No Unity-specific dependencies in core logic
- Fully async/await compatible
- Supports .NET cancellation tokens

---

*This modern TimerManager provides efficient, callback-based timing for Unity games while being completely generic and dependency-injectable.*
