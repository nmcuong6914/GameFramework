using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Demo script showing how to use the reward system
/// </summary>
public class RewardSystemDemo : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private int testLevel = 5;
    [SerializeField] private int previewStartLevel = 1;
    [SerializeField] private int previewLevelCount = 20;
    [SerializeField] private EndLevelRewardConfig rewardConfig;
    
    private PlayerDataManager playerDataManager;
    
    private async void Start()
    {
        // Wait for PlayerDataManager to be available
        playerDataManager = ServiceLocator.TryResolve<PlayerDataManager>();
        
        if (playerDataManager == null)
        {
            Debug.LogError("PlayerDataManager not found in ServiceLocator!");
            return;
        }
        
        // Wait for initialization
        while (!playerDataManager.IsInitialized)
        {
            await Cysharp.Threading.Tasks.UniTask.NextFrame();
        }
        
        Debug.Log("Reward System Demo initialized!");
        LogRewardSystemStatus();
    }
    
    [ContextMenu("Test Level Completion Reward")]
    public void TestLevelCompletionReward()
    {
        if (playerDataManager == null) return;
        
        Debug.Log($"=== Testing Level {testLevel} Completion Reward ===");
        
        // Log current currencies
        LogCurrentCurrencies("Before reward:");
        
        // Give reward - PlayerDataManager will handle config automatically
        playerDataManager.RewardLevelCompletion(testLevel);
        
        // Log currencies after reward
        LogCurrentCurrencies("After reward:");
    }
    
    [ContextMenu("Preview Level Rewards")]
    public void PreviewLevelRewards()
    {
        if (playerDataManager == null) return;
        
        Debug.Log($"=== Previewing rewards for levels {previewStartLevel}-{previewStartLevel + previewLevelCount - 1} ===");
        
        var preview = playerDataManager.GetMultipleLevelRewardPreview(previewStartLevel, previewLevelCount);
        
        foreach (var kvp in preview)
        {
            int level = kvp.Key;
            var rewards = kvp.Value;
            
            if (rewards.Count > 0)
            {
                var rewardStrings = new List<string>();
                foreach (var reward in rewards)
                {
                    if (reward.Value > 0) // Only show rewards with positive amounts
                    {
                        rewardStrings.Add($"{reward.Key}: {reward.Value}");
                    }
                }
                
                if (rewardStrings.Count > 0)
                {
                    Debug.Log($"Level {level}: {string.Join(", ", rewardStrings)}");
                }
                else
                {
                    Debug.Log($"Level {level}: No rewards");
                }
            }
            else
            {
                Debug.Log($"Level {level}: No rewards");
            }
        }
    }
    
    [ContextMenu("Get Single Level Reward Preview")]
    public void GetSingleLevelRewardPreview()
    {
        if (playerDataManager == null) return;
        
        var rewards = playerDataManager.GetLevelRewardPreview(testLevel);
        
        Debug.Log($"=== Level {testLevel} Reward Preview ===");
        
        if (rewards.Count > 0)
        {
            foreach (var kvp in rewards)
            {
                if (kvp.Value > 0)
                {
                    Debug.Log($"{kvp.Key}: {kvp.Value}");
                }
            }
        }
        else
        {
            Debug.Log("No rewards for this level");
        }
    }
    
    [ContextMenu("Log Current Currencies")]
    public void LogCurrentCurrencies()
    {
        LogCurrentCurrencies("Current currencies:");
    }
    
    [ContextMenu("Log Reward System Status")]
    public void LogRewardSystemStatus()
    {
        if (playerDataManager == null)
        {
            Debug.LogWarning("PlayerDataManager not available");
            return;
        }
        
        Debug.Log($"=== Reward System Status ===");
        Debug.Log($"Has Reward Configuration: {playerDataManager.HasRewardConfiguration}");
        
        if (playerDataManager.HasRewardConfiguration)
        {
            Debug.Log("Reward system is using configured rewards from EndLevelRewardConfig");
        }
        else
        {
            Debug.Log("Reward system is using fallback default rewards (please assign EndLevelRewardConfig)");
        }
    }
    
    private void LogCurrentCurrencies(string prefix = "")
    {
        if (playerDataManager == null) return;
        
        var currencies = playerDataManager.GetCurrentCurrencies();
        
        if (!string.IsNullOrEmpty(prefix))
        {
            Debug.Log(prefix);
        }
        
        foreach (var kvp in currencies)
        {
            Debug.Log($"  {kvp.Key}: {kvp.Value}");
        }
    }
}
