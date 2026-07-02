using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject that holds all currency configurations
/// This allows easy editing of currency settings in the Unity Inspector
/// </summary>
[CreateAssetMenu(fileName = "CurrencyConfigData", menuName = "BlockSort/Currency Config Data", order = 1)]
public class CurrencyConfigData : ScriptableObject
{
    [Header("Currency Configurations")]
    [Tooltip("List of all currency configurations. Use 'Initialize Default Currencies' context menu to populate with defaults.")]
    [SerializeField] public List<CurrencyConfig> currencyConfigs = new List<CurrencyConfig>();

    /// <summary>
    /// Get all currency configurations
    /// </summary>
    public List<CurrencyConfig> GetCurrencyConfigs()
    {
        return new List<CurrencyConfig>(currencyConfigs);
    }
    
    /// <summary>
    /// Get configuration for a specific currency type
    /// </summary>
    public CurrencyConfig GetCurrencyConfig(CurrencyType currencyType)
    {
        return currencyConfigs.Find(config => config.currencyType == currencyType);
    }
    
}
