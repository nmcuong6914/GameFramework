using System;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Represents a single currency amount with regeneration capabilities
/// </summary>
[Serializable]
public class Currency
{
    [SerializeField, JsonProperty] private CurrencyType currencyType;
    [SerializeField, JsonProperty] private int amount;
    [SerializeField, JsonProperty] private long lastUpdateTicks;
    
    [JsonIgnore] private CurrencyConfig config;
    
    // Events (not serialized)
    public event Action<Currency, int, int> AmountChanged; // currency, oldAmount, newAmount
    
    // Properties
    [JsonIgnore] public CurrencyType CurrencyType => currencyType;
    [JsonIgnore] public int Amount => amount;
    [JsonIgnore] public CurrencyConfig Config => config;
    [JsonIgnore] public bool CanRegenerate => config != null && config.regenerationType == RegenerationType.OverTime;
    [JsonIgnore] public bool IsAtMaxCapacity => config != null && amount >= config.maxAmount;
    
    // Constructors
    public Currency()
    {
        lastUpdateTicks = DateTime.UtcNow.Ticks;
    }
    
    public Currency(CurrencyType type, CurrencyConfig config, int initialAmount = -1)
    {
        this.currencyType = type;
        this.config = config;
        this.amount = initialAmount >= 0 ? initialAmount : config.defaultAmount;
        this.lastUpdateTicks = DateTime.UtcNow.Ticks;
    }
    
    /// <summary>
    /// Initialize or update the currency configuration
    /// </summary>
    public void Initialize(CurrencyConfig config)
    {
        if (config == null)
        {
            Debug.LogError("Currency.Initialize called with null config!");
            return;
        }
        
        this.config = config;
        // For regenerating currencies (like Lives), only set to default on very first initialization
        // when lastUpdateTicks hasn't been set yet (brand new player data)
        // This prevents overriding saved values including 0 lives
        if (config.regenerationType == RegenerationType.OverTime)
        {
            // Only set to default for completely new currency instances
            if (amount == 0 && lastUpdateTicks == 0 && config.defaultAmount > 0)
            {
                amount = config.defaultAmount;
            }
        }
        else
        {
            // For non-regenerating currencies, use the original logic
            if (amount == 0 && config.defaultAmount > 0)
            {
                amount = config.defaultAmount;
            }
        }
        UpdateRegeneration();
    }
    
    /// <summary>
    /// Add amount to the currency
    /// </summary>
    public bool Add(int value)
    {
        if (value <= 0) return false;
        
        UpdateRegeneration();
        
        int oldAmount = amount;
        int newAmount = config != null ? Mathf.Min(amount + value, config.maxAmount) : amount + value;
        
        if (newAmount != amount)
        {
            amount = newAmount;
            AmountChanged?.Invoke(this, oldAmount, amount);
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Spend/subtract amount from the currency
    /// </summary>
    public bool Spend(int value)
    {
        if (value <= 0 || amount < value) return false;
        
        UpdateRegeneration();
        
        // Check if we were at max capacity before spending
        bool wasAtMaxCapacity = IsAtMaxCapacity;
        
        int oldAmount = amount;
        amount -= value;
        
        // If we were at max capacity and now we're not, reset the regeneration timer
        // This ensures cooldown starts fresh from full duration
        if (wasAtMaxCapacity && !IsAtMaxCapacity && CanRegenerate)
        {
            ResetRegenerationTimer();
        }
        
        AmountChanged?.Invoke(this, oldAmount, amount);
        return true;
    }
    
    /// <summary>
    /// Set the currency amount directly
    /// </summary>
    public void SetAmount(int newAmount)
    {
        UpdateRegeneration();
        
        // Check if we were at max capacity before setting
        bool wasAtMaxCapacity = IsAtMaxCapacity;
        
        int oldAmount = amount;
        amount = config != null ? Mathf.Clamp(newAmount, 0, config.maxAmount) : Mathf.Max(0, newAmount);
        
        // If we were at max capacity and now we're not, reset the regeneration timer
        // This ensures cooldown starts fresh from full duration
        if (wasAtMaxCapacity && !IsAtMaxCapacity && CanRegenerate && amount < oldAmount)
        {
            ResetRegenerationTimer();
        }
        
        if (amount != oldAmount)
        {
            AmountChanged?.Invoke(this, oldAmount, amount);
        }
    }
    
    /// <summary>
    /// Check if we have enough of this currency
    /// </summary>
    public bool HasEnough(int requiredAmount)
    {
        UpdateRegeneration();
        return amount >= requiredAmount;
    }
    
    /// <summary>
    /// Get time until next regeneration (in seconds)
    /// </summary>
    public float GetTimeToNextRegeneration()
    {
        if (!CanRegenerate || IsAtMaxCapacity || config.regenerationInterval <= 0) 
            return 0f;
            
        var timeSinceLastUpdate = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - lastUpdateTicks);
        float elapsedSeconds = (float)timeSinceLastUpdate.TotalSeconds;
        float timeToNext = config.regenerationInterval - (elapsedSeconds % config.regenerationInterval);
        
        return timeToNext;
    }
    
    /// <summary>
    /// Update regeneration if applicable
    /// </summary>
    public void UpdateRegeneration()
    {
        if (!CanRegenerate || IsAtMaxCapacity || config == null) return;
        
        var now = DateTime.UtcNow.Ticks;
        var timeDifference = TimeSpan.FromTicks(now - lastUpdateTicks);
        
        if (timeDifference.TotalSeconds >= config.regenerationInterval)
        {
            int regenCycles = (int)(timeDifference.TotalSeconds / config.regenerationInterval);
            int regenAmount = regenCycles * config.regenerationAmount;
            
            if (regenAmount > 0)
            {
                int oldAmount = amount;
                amount = Mathf.Min(amount + regenAmount, config.maxAmount);
                
                // Update the last update time to the last completed regeneration cycle
                var completedCycles = (long)(regenCycles * config.regenerationInterval * TimeSpan.TicksPerSecond);
                lastUpdateTicks += completedCycles;
                
                if (amount != oldAmount)
                {
                    AmountChanged?.Invoke(this, oldAmount, amount);
                }
            }
        }
    }
    
    /// <summary>
    /// Get display text for this currency
    /// </summary>
    public string GetDisplayText()
    {
        UpdateRegeneration();
        
        if (config != null && config.maxAmount != 999999)
        {
            return $"{amount}";
        }
        
        return amount.ToString();
    }
    
    /// <summary>
    /// Reset regeneration timer (useful when player makes purchases or watches ads)
    /// </summary>
    public void ResetRegenerationTimer()
    {
        lastUpdateTicks = DateTime.UtcNow.Ticks;
    }
}
