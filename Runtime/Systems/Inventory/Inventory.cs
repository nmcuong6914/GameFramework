using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Generic inventory system for managing currencies and other game resources
/// </summary>
[Serializable]
public class Inventory
{
    [SerializeField, JsonProperty] private List<Currency> currencies = new List<Currency>();
    
    [JsonIgnore] private Dictionary<CurrencyType, Currency> currencyLookup = new Dictionary<CurrencyType, Currency>();
    [JsonIgnore] private Dictionary<CurrencyType, CurrencyConfig> configLookup = new Dictionary<CurrencyType, CurrencyConfig>();
    
    // Events (not serialized)
    public event Action<Currency, int, int> CurrencyChanged; // currency, oldAmount, newAmount
    public event Action<CurrencyType> CurrencyAdded;
    public event Action<CurrencyType> CurrencyRemoved;
    
    /// <summary>
    /// Initialize the inventory with currency configurations
    /// </summary>
    public void Initialize(List<CurrencyConfig> currencyConfigs)
    {
        if (currencyConfigs == null)
        {
            Debug.LogError("Inventory.Initialize called with null currencyConfigs!");
            return;
        }
        
        configLookup.Clear();
        currencyLookup.Clear();
        
        // Store configurations
        foreach (var config in currencyConfigs)
        {
            if (config != null)
            {
                configLookup[config.currencyType] = config;
            }
        }
        
        // Initialize existing currencies or create new ones
        var existingTypes = currencies.Select(c => c.CurrencyType).ToHashSet();
        
        foreach (var config in currencyConfigs)
        {
            if (config == null) continue;
            
            var existingCurrency = currencies.FirstOrDefault(c => c.CurrencyType == config.currencyType);
            
            if (existingCurrency != null)
            {
                // Initialize existing currency with config
                existingCurrency.Initialize(config);
                currencyLookup[config.currencyType] = existingCurrency;
                existingCurrency.AmountChanged += OnCurrencyAmountChanged;
            }
            else
            {
                // Create new currency
                var newCurrency = new Currency(config.currencyType, config);
                currencies.Add(newCurrency);
                currencyLookup[config.currencyType] = newCurrency;
                newCurrency.AmountChanged += OnCurrencyAmountChanged;
                CurrencyAdded?.Invoke(config.currencyType);
            }
        }
        
        // Remove currencies that are no longer in the config
        var configTypes = currencyConfigs.Where(c => c != null).Select(c => c.currencyType).ToHashSet();
        var toRemove = currencies.Where(c => !configTypes.Contains(c.CurrencyType)).ToList();
        
        foreach (var currency in toRemove)
        {
            currencies.Remove(currency);
            currencyLookup.Remove(currency.CurrencyType);
            currency.AmountChanged -= OnCurrencyAmountChanged;
            CurrencyRemoved?.Invoke(currency.CurrencyType);
        }
    }
    
    /// <summary>
    /// Get a specific currency by type
    /// </summary>
    public Currency GetCurrency(CurrencyType type)
    {
        currencyLookup.TryGetValue(type, out var currency);
        return currency;
    }
    
    /// <summary>
    /// Get all currencies
    /// </summary>
    public IReadOnlyList<Currency> GetAllCurrencies()
    {
        return currencies.AsReadOnly();
    }
    
    /// <summary>
    /// Get currency amount
    /// </summary>
    public int GetAmount(CurrencyType type)
    {
        var currency = GetCurrency(type);
        if (currency != null)
        {
            return currency.Amount;
        }
        
        // If currency doesn't exist in lookup, try to create it from config
        if (TryCreateCurrencyFromConfig(type))
        {
            currency = GetCurrency(type);
            if (currency != null)
            {
                return currency.Amount;
            }
        }
        
        return 0;
    }
    
    /// <summary>
    /// Add amount to a currency
    /// </summary>
    public bool AddCurrency(CurrencyType type, int amount)
    {
        var currency = GetCurrency(type);
        if (currency != null)
        {
            return currency.Add(amount);
        }
        
        // If currency doesn't exist in lookup, try to create it from config
        if (TryCreateCurrencyFromConfig(type))
        {
            currency = GetCurrency(type);
            if (currency != null)
            {
                return currency.Add(amount);
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Try to create a currency from configuration if it doesn't exist
    /// </summary>
    private bool TryCreateCurrencyFromConfig(CurrencyType type)
    {
        // Check if currency already exists
        if (currencyLookup.ContainsKey(type))
        {
            return true; // Already exists
        }
        
        // Look for config in the config lookup
        if (configLookup.TryGetValue(type, out var config))
        {
            // Create new currency with the config
            var newCurrency = new Currency(config.currencyType, config);
            currencies.Add(newCurrency);
            currencyLookup[config.currencyType] = newCurrency;
            newCurrency.AmountChanged += OnCurrencyAmountChanged;
            CurrencyAdded?.Invoke(config.currencyType);
            
            Debug.Log($"Created new currency: {config.currencyType} with default amount {config.defaultAmount}");
            return true;
        }
        
        Debug.LogWarning($"Cannot create currency {type} - no configuration found in configLookup!");
        return false;
    }
    
    /// <summary>
    /// Spend currency
    /// </summary>
    public bool SpendCurrency(CurrencyType type, int amount)
    {
        var currency = GetCurrency(type);
        if (currency != null)
        {
            return currency.Spend(amount);
        }
        
        // If currency doesn't exist in lookup, try to create it from config
        if (TryCreateCurrencyFromConfig(type))
        {
            currency = GetCurrency(type);
            if (currency != null)
            {
                return currency.Spend(amount);
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if we have enough of a currency
    /// </summary>
    public bool HasEnoughCurrency(CurrencyType type, int requiredAmount)
    {
        var currency = GetCurrency(type);
        if (currency != null)
        {
            return currency.HasEnough(requiredAmount);
        }
        
        // If currency doesn't exist in lookup, try to create it from config
        if (TryCreateCurrencyFromConfig(type))
        {
            currency = GetCurrency(type);
            if (currency != null)
            {
                return currency.HasEnough(requiredAmount);
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Set currency amount directly
    /// </summary>
    public void SetCurrency(CurrencyType type, int amount)
    {
        var currency = GetCurrency(type);
        if (currency != null)
        {
            currency.SetAmount(amount);
            return;
        }
        
        // If currency doesn't exist in lookup, try to create it from config
        if (TryCreateCurrencyFromConfig(type))
        {
            currency = GetCurrency(type);
            currency?.SetAmount(amount);
        }
    }
    
    /// <summary>
    /// Update all currencies for regeneration
    /// </summary>
    public void UpdateCurrencies()
    {
        foreach (var currency in currencies)
        {
            currency.UpdateRegeneration();
        }
    }
    
    /// <summary>
    /// Check if multiple currencies can be spent
    /// </summary>
    public bool CanAfford(Dictionary<CurrencyType, int> costs)
    {
        foreach (var cost in costs)
        {
            if (!HasEnoughCurrency(cost.Key, cost.Value))
            {
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// Spend multiple currencies at once
    /// </summary>
    public bool SpendMultiple(Dictionary<CurrencyType, int> costs)
    {
        if (!CanAfford(costs))
        {
            return false;
        }
        
        foreach (var cost in costs)
        {
            SpendCurrency(cost.Key, cost.Value);
        }
        
        return true;
    }
    
    /// <summary>
    /// Add multiple currencies at once
    /// </summary>
    public void AddMultiple(Dictionary<CurrencyType, int> rewards)
    {
        foreach (var reward in rewards)
        {
            AddCurrency(reward.Key, reward.Value);
        }
    }
    
    /// <summary>
    /// Get currencies that can regenerate
    /// </summary>
    public List<Currency> GetRegeneratingCurrencies()
    {
        return currencies.Where(c => c.CanRegenerate && !c.IsAtMaxCapacity).ToList();
    }
    
    /// <summary>
    /// Reset regeneration timers for all currencies
    /// </summary>
    public void ResetAllRegenerationTimers()
    {
        foreach (var currency in currencies)
        {
            currency.ResetRegenerationTimer();
        }
    }
    
    /// <summary>
    /// Get currency configuration
    /// </summary>
    public CurrencyConfig GetCurrencyConfig(CurrencyType type)
    {
        configLookup.TryGetValue(type, out var config);
        return config;
    }
    
    private void OnCurrencyAmountChanged(Currency currency, int oldAmount, int newAmount)
    {
        CurrencyChanged?.Invoke(currency, oldAmount, newAmount);
    }
    
    /// <summary>
    /// For serialization support - call this after deserialization
    /// </summary>
    public void OnAfterDeserialize()
    {
        currencyLookup.Clear();
        
        foreach (var currency in currencies)
        {
            currencyLookup[currency.CurrencyType] = currency;
            currency.AmountChanged += OnCurrencyAmountChanged;
        }
    }
    
    /// <summary>
    /// Rebuild config lookup after loading from save data
    /// </summary>
    public void RebuildConfigLookup(List<CurrencyConfig> currencyConfigs)
    {
        if (currencyConfigs == null)
        {
            Debug.LogError("RebuildConfigLookup called with null currencyConfigs!");
            return;
        }
        
        configLookup.Clear();
        
        foreach (var config in currencyConfigs)
        {
            if (config != null)
            {
                configLookup[config.currencyType] = config;
            }
        }
    }
}
