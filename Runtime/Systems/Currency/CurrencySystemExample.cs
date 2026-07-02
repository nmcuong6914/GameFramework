using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// Example script showing how to use the currency and inventory system
/// </summary>
public class CurrencySystemExample : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private CurrencyType testCurrencyType = CurrencyType.Coin;
    [SerializeField] private int testAmount = 100;
    
    private PlayerDataManager playerDataManager;
    
    private async void Start()
    {
        // Get PlayerDataManager from ServiceLocator
        playerDataManager = ServiceLocator.TryResolve<PlayerDataManager>();
        
        if (playerDataManager == null)
        {
            Debug.LogError("PlayerDataManager not found in ServiceLocator!");
            return;
        }
        
        // Wait for initialization
        while (!playerDataManager.IsInitialized)
        {
            await UniTask.NextFrame();
        }
        
        // Subscribe to currency changes
        playerDataManager.CurrencyChanged += OnCurrencyChanged;
        
        // Log initial currency amounts
        LogAllCurrencies();
    }
    
    private void OnCurrencyChanged(Currency currency, int oldAmount, int newAmount)
    {
        Debug.Log($"Currency {currency.CurrencyType} changed from {oldAmount} to {newAmount}");
    }
    
    [ContextMenu("Add Test Currency")]
    public void AddTestCurrency()
    {
        if (playerDataManager != null)
        {
            bool success = playerDataManager.AddCurrency(testCurrencyType, testAmount);
            Debug.Log($"Add {testAmount} {testCurrencyType}: {(success ? "Success" : "Failed")}");
        }
    }
    
    [ContextMenu("Spend Test Currency")]
    public void SpendTestCurrency()
    {
        if (playerDataManager != null)
        {
            bool success = playerDataManager.SpendCurrency(testCurrencyType, testAmount);
            Debug.Log($"Spend {testAmount} {testCurrencyType}: {(success ? "Success" : "Failed")}");
        }
    }
    
    [ContextMenu("Log All Currencies")]
    public void LogAllCurrencies()
    {
        if (playerDataManager?.Inventory != null)
        {
            var currencies = playerDataManager.Inventory.GetAllCurrencies();
            Debug.Log("=== Current Currencies ===");
            
            foreach (var currency in currencies)
            {
                string regenInfo = currency.CanRegenerate 
                    ? $" (Regens in {currency.GetTimeToNextRegeneration():F1}s)"
                    : "";
                    
                Debug.Log($"{currency.CurrencyType}: {currency.GetDisplayText()}{regenInfo}");
            }
        }
    }
    
    [ContextMenu("Test Multi-Currency Transaction")]
    public void TestMultiCurrencyTransaction()
    {
        if (playerDataManager != null)
        {
            var costs = new Dictionary<CurrencyType, int>
            {
                { CurrencyType.Coin, 50 },
                { CurrencyType.Lives, 1 },
                { CurrencyType.BoosterHammer, 1 }
            };
            
            bool canAfford = playerDataManager.CanAfford(costs);
            Debug.Log($"Can afford multi-currency cost: {canAfford}");
            
            if (canAfford)
            {
                bool success = playerDataManager.SpendMultiple(costs);
                Debug.Log($"Multi-currency spend: {(success ? "Success" : "Failed")}");
            }
        }
    }
    
    [ContextMenu("Add Rewards")]
    public void AddRewards()
    {
        if (playerDataManager != null)
        {
            var rewards = new Dictionary<CurrencyType, int>
            {
                { CurrencyType.Coin, 200 },
                { CurrencyType.BoosterHammer, 2 },
                { CurrencyType.BoosterShuffle, 1 }
            };
            
            playerDataManager.AddMultiple(rewards);
            Debug.Log("Rewards added!");
        }
    }
    
    [ContextMenu("Force Save")]
    public async void ForceSave()
    {
        if (playerDataManager != null)
        {
            bool success = await playerDataManager.ForceSaveAsync();
            Debug.Log($"Force save: {(success ? "Success" : "Failed")}");
        }
    }
    
    [ContextMenu("Reset Player Data")]
    public async void ResetPlayerData()
    {
        if (playerDataManager != null)
        {
            await playerDataManager.ResetPlayerDataAsync();
            Debug.Log("Player data reset!");
            LogAllCurrencies();
        }
    }
    
    private void OnDestroy()
    {
        if (playerDataManager != null)
        {
            playerDataManager.CurrencyChanged -= OnCurrencyChanged;
        }
    }
}
