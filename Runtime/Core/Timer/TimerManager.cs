using System;
using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;

/// <summary>
/// Manager for handling multiple timers and cooldowns in the game.
/// Creates and manages Timer instances rather than handling timing logic internally.
/// </summary>
public class TimerManager : IDisposable
{
    // Timer storage - stores actual Timer instances
    private readonly Dictionary<string, Timer> timers = new Dictionary<string, Timer>();

    // Settings
    private readonly bool debugTimers;

    // Events
    public event Action<string> OnNamedTimerComplete;
    public event Action<string> OnNamedTimerStart;
    public event Action<string> OnNamedTimerResume;
    public event Action<string> OnNamedTimerCreated;

    public TimerManager(bool debugTimers = false)
    {
        this.debugTimers = debugTimers;
    }

    /// <summary>
    /// Create a named timer
    /// </summary>
    public Timer CreateNamedTimer(string name, float duration, Action onComplete = null, Action<float> onTick = null)
    {
        // Remove existing timer with same name
        if (timers.ContainsKey(name))
        {
            if (debugTimers)
            {
                FDebug.LogWarning($"TimerManager: Timer with name '{name}' already exists. Removing old timer.");
            }
            RemoveTimer(name);
        }

        var timer = new Timer(duration);
        
        // Subscribe to timer events
        timer.OnTimerStart += () => 
        {
            if (debugTimers)
            {
                FDebug.Log($"TimerManager: Timer '{name}' started");
            }
            OnNamedTimerStart?.Invoke(name);
        };
        
        timer.OnTimerResume += () => 
        {
            if (debugTimers)
            {
                FDebug.Log($"TimerManager: Timer '{name}' resumed");
            }
            OnNamedTimerResume?.Invoke(name);
        };
        
        timer.OnTimerComplete += () => 
        {
            if (debugTimers)
            {
                FDebug.Log($"TimerManager: Timer '{name}' completed");
            }
            OnNamedTimerComplete?.Invoke(name);
            onComplete?.Invoke();
            
            // Remove timer after completion if it's not already removed
            if (timers.ContainsKey(name))
            {
                RemoveTimer(name);
            }
        };

        if (onTick != null)
        {
            timer.OnTimerTick += onTick;
        }

        timers[name] = timer;
        
        // Fire timer created event
        if (debugTimers)
        {
            FDebug.Log($"TimerManager: Timer '{name}' created with duration {duration}s");
        }
        OnNamedTimerCreated?.Invoke(name);
        
        return timer;
    }

    /// <summary>
    /// Get a timer by name
    /// </summary>
    public Timer GetTimer(string name)
    {
        return timers.TryGetValue(name, out Timer timer) ? timer : null;
    }

    /// <summary>
    /// Start a timer by name
    /// </summary>
    public void StartTimer(string name)
    {
        if (timers.TryGetValue(name, out Timer timer))
        {
            timer.Start();
        }
    }

    /// <summary>
    /// Stop a timer by name
    /// </summary>
    public void StopTimer(string name)
    {
        if (timers.TryGetValue(name, out Timer timer))
        {
            timer.Stop();
        }
    }

    /// <summary>
    /// Pause a timer by name
    /// </summary>
    public void PauseTimer(string name)
    {
        if (timers.TryGetValue(name, out Timer timer))
        {
            timer.Pause();
            
            if (debugTimers)
            {
                FDebug.Log($"TimerManager: Paused timer '{name}'");
            }
        }
    }

    /// <summary>
    /// Resume a timer by name
    /// </summary>
    public void ResumeTimer(string name)
    {
        if (timers.TryGetValue(name, out Timer timer))
        {
            timer.Resume();
            
            if (debugTimers)
            {
                FDebug.Log($"TimerManager: Resumed timer '{name}'");
            }
        }
    }

    /// <summary>
    /// Reset a timer by name
    /// </summary>
    public void ResetTimer(string name)
    {
        if (timers.TryGetValue(name, out Timer timer))
        {
            timer.Reset();
            
            if (debugTimers)
            {
                FDebug.Log($"TimerManager: Reset timer '{name}'");
            }
        }
    }

    /// <summary>
    /// Remove a timer by name
    /// </summary>
    public void RemoveTimer(string name)
    {
        if (timers.TryGetValue(name, out Timer timer))
        {
            timer.Stop();
            timers.Remove(name);
            
            if (debugTimers)
            {
                FDebug.Log($"TimerManager: Removed timer '{name}'");
            }
        }
    }

    /// <summary>
    /// Get remaining time for a named timer
    /// </summary>
    public float GetRemainingTime(string name)
    {
        if (timers.TryGetValue(name, out Timer timer))
        {
            return timer.RemainingTime;
        }
        return 0f;
    }

    /// <summary>
    /// Get total duration for a named timer
    /// </summary>
    public float GetTotalDuration(string name)
    {
        if (timers.TryGetValue(name, out Timer timer))
        {
            return timer.Duration;
        }
        return 0f;
    }

    /// <summary>
    /// Cancel a named timer (same as StopTimer but for backwards compatibility)
    /// </summary>
    public void CancelNamedTimer(string name)
    {
        StopTimer(name);
    }

    /// <summary>
    /// Check if a named timer is running
    /// </summary>
    public bool IsTimerRunning(string name)
    {
        return timers.TryGetValue(name, out Timer timer) && timer.IsRunning;
    }

    /// <summary>
    /// Cancel all timers
    /// </summary>
    public void CancelAllTimers()
    {
        foreach (var timer in timers.Values)
        {
            timer.Stop();
        }
        
        timers.Clear();
        
        if (debugTimers)
        {
            FDebug.Log("TimerManager: Cancelled all timers");
        }
    }

    /// <summary>
    /// Create a cooldown timer for boosters or other time-limited items
    /// </summary>
    public Timer CreateCooldownTimer(string itemId, float cooldownDuration, Action onCooldownComplete = null)
    {
        string timerName = $"Cooldown_{itemId}";
        
        return CreateNamedTimer(timerName, cooldownDuration, () =>
        {
            if (debugTimers)
            {
                FDebug.Log($"TimerManager: Cooldown for '{itemId}' completed");
            }
            onCooldownComplete?.Invoke();
        });
    }

    /// <summary>
    /// Check if an item is on cooldown
    /// </summary>
    public bool IsOnCooldown(string itemId)
    {
        string timerName = $"Cooldown_{itemId}";
        return IsTimerRunning(timerName);
    }

    /// <summary>
    /// Cancel cooldown for an item
    /// </summary>
    public void CancelCooldown(string itemId)
    {
        string timerName = $"Cooldown_{itemId}";
        RemoveTimer(timerName);
    }

    /// <summary>
    /// Get all active timer names (for debugging)
    /// </summary>
    public List<string> GetAllActiveTimers()
    {
        var activeTimers = new List<string>();
        
        foreach (var kvp in timers)
        {
            if (kvp.Value.IsRunning)
            {
                activeTimers.Add(kvp.Key);
            }
        }
        
        return activeTimers;
    }

    /// <summary>
    /// Dispose of all timers and cleanup resources
    /// </summary>
    public void Dispose()
    {
        CancelAllTimers();
        
        if (debugTimers)
        {
            FDebug.Log("TimerManager: Disposed");
        }
    }
}
