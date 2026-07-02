using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject containing all purchase configurations
/// </summary>
[CreateAssetMenu(fileName = "PurchaseConfig", menuName = "BlockSort/Purchase Config")]
public class PurchaseConfig : ScriptableObject
{
    [Header("Purchase Options")]
    [SerializeField] private List<PurchaseItemConfig> purchaseOptions = new List<PurchaseItemConfig>();

    /// <summary>
    /// Get purchase options for a specific currency type
    /// </summary>
    public List<PurchaseItemConfig> GetOptionsForCurrency(CurrencyType currencyType)
    {
        return purchaseOptions.FindAll(option => option.targetCurrency == currencyType);
    }
}

/// <summary>
/// Configuration for a single purchase option
/// </summary>
[System.Serializable]
public class PurchaseItemConfig
{
    [Header("Contents")]
    public CurrencyType targetCurrency;
    public int amount = 1;

    [Header("Cost")]
    public int coinCost = 100;
}