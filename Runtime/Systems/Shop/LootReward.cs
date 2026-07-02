using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a loot reward that can contain multiple types of currencies and items
/// </summary>
[Serializable]
public class LootReward
{
    [Header("Currency Rewards")]
    [Tooltip("List of currency rewards in this loot package")]
    public List<CurrencyReward> currencyRewards = new List<CurrencyReward>();
    
}

/// <summary>
/// Represents a single currency reward within a loot package
/// </summary>
[Serializable]
public class CurrencyReward
{
    [Tooltip("Type of currency to reward")]
    public CurrencyType currencyType;
    
    [Tooltip("Amount of this currency to give")]
    public int amount;
    
    public CurrencyReward()
    {
        currencyType = CurrencyType.Coin;
        amount = 0;
    }
    
    public CurrencyReward(CurrencyType type, int amount)
    {
        this.currencyType = type;
        this.amount = amount;
    }
    
    /// <summary>
    /// Check if this reward is valid
    /// </summary>
    public bool IsValid => amount > 0;
}