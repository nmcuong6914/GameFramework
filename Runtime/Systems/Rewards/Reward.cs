using System;
using UnityEngine;

/// <summary>
/// Represents a single reward containing currency type and amount
/// </summary>
[Serializable]
public class Reward
{
    [Tooltip("Type of currency to reward")]
    public CurrencyType currencyType;
    
    [Tooltip("Amount to reward")]
    public int amount;
    
    public Reward()
    {
        currencyType = CurrencyType.Coin;
        amount = 0;
    }
    
    public Reward(CurrencyType type, int value)
    {
        currencyType = type;
        amount = value;
    }
    
    /// <summary>
    /// Check if this reward is valid
    /// </summary>
    public bool IsValid => amount > 0;
    
    /// <summary>
    /// Get display string for this reward
    /// </summary>
    public override string ToString()
    {
        return $"{currencyType}: {amount}";
    }
}
