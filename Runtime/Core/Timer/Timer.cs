using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

/// <summary>
/// A flexible timer system that can handle countdowns, timers, and cooldowns.
/// Can be used for level timers, booster cooldowns, lives regeneration, etc.
/// Runs itself using UniTask for efficient async operation.
/// </summary>
[System.Serializable]
public class Timer
{
    [Header("Timer Configuration")]
    [SerializeField] private float duration;
    [SerializeField] private bool autoStart;
    [SerializeField] private bool loop;
    [SerializeField] private bool pauseWithTimeScale = true;

    // Runtime state
    private float currentTime;
    private bool isStarted; // Whether timer has been started (not stopped)
    private bool isPaused;
    private TimerDirection direction = TimerDirection.Down;
    
    // UniTask cancellation
    private CancellationTokenSource cancellationTokenSource;

    // Events
    public event Action OnTimerStart;
    public event Action OnTimerComplete;
    public event Action OnTimerPause;
    public event Action OnTimerResume;
    public event Action OnTimerReset;
    public event Action<float> OnTimerTick;

    // Properties
    public float Duration => duration;
    public float CurrentTime => currentTime;
    public float RemainingTime => direction == TimerDirection.Down ? currentTime : duration - currentTime;
    public float ElapsedTime => direction == TimerDirection.Down ? duration - currentTime : currentTime;
    public float Progress => Mathf.Clamp01(ElapsedTime / duration);
    public bool IsRunning => isStarted && !isPaused; // Running means started but not paused
    public bool IsPaused => isPaused;
    public bool IsStarted => isStarted; // Whether timer has been started (may be paused)
    public bool IsComplete => direction == TimerDirection.Down ? currentTime <= 0 : currentTime >= duration;

    /// <summary>
    /// Default constructor for countdown timer
    /// </summary>
    public Timer(float duration, bool autoStart = false, bool loop = false)
    {
        this.duration = duration;
        this.autoStart = autoStart;
        this.loop = loop;
        this.direction = TimerDirection.Down;
        this.currentTime = duration;

        if (autoStart)
        {
            Start();
        }
    }

    /// <summary>
    /// Constructor with direction control
    /// </summary>
    public Timer(float duration, TimerDirection direction, bool autoStart = false, bool loop = false)
    {
        this.duration = duration;
        this.direction = direction;
        this.autoStart = autoStart;
        this.loop = loop;
        this.currentTime = direction == TimerDirection.Down ? duration : 0;

        if (autoStart)
        {
            Start();
        }
    }

    /// <summary>
    /// Start the timer
    /// </summary>
    public void Start()
    {
        if (IsRunning) return; // Already running

        isStarted = true;
        isPaused = false;
        
        // Cancel any existing timer task
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
        
        // Start new timer task
        cancellationTokenSource = new CancellationTokenSource();
        RunTimerAsync(cancellationTokenSource.Token).Forget();
        
        OnTimerStart?.Invoke();
    }

    /// <summary>
    /// The main timer loop running on UniTask
    /// </summary>
    private async UniTaskVoid RunTimerAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (isStarted && !cancellationToken.IsCancellationRequested)
            {
                // Wait for next frame
                await UniTask.NextFrame(cancellationToken);
                
                // Skip if paused
                if (isPaused) continue;
                
                // Get delta time
                float deltaTime = pauseWithTimeScale ? Time.deltaTime : Time.unscaledDeltaTime;
                
                // Update timer based on direction
                if (direction == TimerDirection.Down)
                {
                    currentTime -= deltaTime;
                    if (currentTime <= 0)
                    {
                        currentTime = 0;
                        CompleteTimer();
                        
                        if (!loop) break; // Exit loop if not looping
                    }
                }
                else
                {
                    currentTime += deltaTime;
                    if (currentTime >= duration)
                    {
                        currentTime = duration;
                        CompleteTimer();
                        
                        if (!loop) break; // Exit loop if not looping
                    }
                }

                // Fire tick event
                OnTimerTick?.Invoke(RemainingTime);
            }
        }
        catch (OperationCanceledException)
        {
            // Timer was cancelled, which is normal
        }
    }

    /// <summary>
    /// Pause the timer
    /// </summary>
    public void Pause()
    {
        if (!isStarted || isPaused) return;

        isPaused = true;
        OnTimerPause?.Invoke();
    }

    /// <summary>
    /// Resume the timer
    /// </summary>
    public void Resume()
    {
        if (!isStarted || !isPaused) return;

        isPaused = false;
        OnTimerResume?.Invoke();
    }

    /// <summary>
    /// Stop the timer
    /// </summary>
    public void Stop()
    {
        isStarted = false;
        isPaused = false;
        
        // Cancel the timer task
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
        cancellationTokenSource = null;
    }

    /// <summary>
    /// Reset the timer to its initial state
    /// </summary>
    public void Reset()
    {
        isStarted = false;
        isPaused = false;
        currentTime = direction == TimerDirection.Down ? duration : 0;
        OnTimerReset?.Invoke();
    }

    /// <summary>
    /// Reset and start the timer
    /// </summary>
    public void Restart()
    {
        Reset();
        Start();
    }

    /// <summary>
    /// Set a new duration for the timer
    /// </summary>
    public void SetDuration(float newDuration)
    {
        duration = newDuration;
        if (!isStarted)
        {
            currentTime = direction == TimerDirection.Down ? duration : 0;
        }
    }

    /// <summary>
    /// Add time to the timer
    /// </summary>
    public void AddTime(float timeToAdd)
    {
        if (direction == TimerDirection.Down)
        {
            currentTime = Mathf.Max(0, currentTime + timeToAdd);
        }
        else
        {
            currentTime = Mathf.Min(duration, currentTime + timeToAdd);
        }
    }

    /// <summary>
    /// Set the current time directly
    /// </summary>
    public void SetCurrentTime(float time)
    {
        currentTime = Mathf.Clamp(time, 0, duration);
    }

    /// <summary>
    /// Force complete the timer
    /// </summary>
    public void ForceComplete()
    {
        currentTime = direction == TimerDirection.Down ? 0 : duration;
        CompleteTimer();
    }

    /// <summary>
    /// Get formatted time string
    /// </summary>
    public string GetFormattedTime(TimerFormat format = TimerFormat.MinutesSeconds)
    {
        float timeToFormat = RemainingTime;
        
        switch (format)
        {
            case TimerFormat.Seconds:
                return $"{timeToFormat:F0}s";
            case TimerFormat.SecondsDecimal:
                return $"{timeToFormat:F1}s";
            case TimerFormat.MinutesSeconds:
                int minutes = Mathf.FloorToInt(timeToFormat / 60);
                int seconds = Mathf.FloorToInt(timeToFormat % 60);
                return $"{minutes:00}:{seconds:00}";
            case TimerFormat.HoursMinutesSeconds:
                int hours = Mathf.FloorToInt(timeToFormat / 3600);
                minutes = Mathf.FloorToInt((timeToFormat % 3600) / 60);
                seconds = Mathf.FloorToInt(timeToFormat % 60);
                return $"{hours:00}:{minutes:00}:{seconds:00}";
            default:
                return $"{timeToFormat:F1}s";
        }
    }

    /// <summary>
    /// Create a UniTask that completes when the timer finishes
    /// </summary>
    public async UniTask WaitForCompletion()
    {
        while (isStarted && !IsComplete)
        {
            await UniTask.NextFrame();
        }
    }

    /// <summary>
    /// Dispose of timer resources
    /// </summary>
    public void Dispose()
    {
        Stop();
    }

    private void CompleteTimer()
    {
        OnTimerComplete?.Invoke();

        if (loop)
        {
            currentTime = direction == TimerDirection.Down ? duration : 0;
        }
        else
        {
            isStarted = false;
        }
    }
}

/// <summary>
/// Direction the timer counts
/// </summary>
public enum TimerDirection
{
    Up,     // Count from 0 to duration
    Down    // Count from duration to 0
}

/// <summary>
/// Format options for displaying timer
/// </summary>
public enum TimerFormat
{
    Seconds,                // "30s"
    SecondsDecimal,         // "30.5s"
    MinutesSeconds,         // "01:30"
    HoursMinutesSeconds     // "01:30:45"
}
