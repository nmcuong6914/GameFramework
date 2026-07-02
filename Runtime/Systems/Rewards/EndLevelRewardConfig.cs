using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a custom reward for a specific level
/// </summary>
[Serializable]
public class LevelReward
{
    [Tooltip("The level number this reward applies to")]
    public int levelNumber = 1;
    
    [Tooltip("List of rewards to give for this level")]
    public List<Reward> rewards = new List<Reward>();
    
    /// <summary>
    /// Get all valid rewards from this level reward
    /// </summary>
    public List<Reward> GetValidRewards()
    {
        var validRewards = new List<Reward>();
        foreach (var reward in rewards)
        {
            if (reward.IsValid)
                validRewards.Add(reward);
        }
        return validRewards;
    }
}

/// <summary>
/// ScriptableObject that holds end level reward configuration
/// Simple system with default rewards for all levels and custom rewards for specific levels
/// </summary>
[CreateAssetMenu(fileName = "EndLevelRewardConfig", menuName = "BlockSort/End Level Reward Config", order = 2)]
public class EndLevelRewardConfig : ScriptableObject
{
    [Header("Default Rewards")]
    [Tooltip("Default rewards given for completing any level")]
    public List<Reward> defaultRewards = new List<Reward>();
    
    [Header("Custom Level Rewards")]
    [Tooltip("Custom rewards for specific levels (overrides default rewards)")]
    public List<LevelReward> customLevelRewards = new List<LevelReward>();
    
    /// <summary>
    /// Get all rewards for a specific level
    /// </summary>
    /// <param name="level">The level number (1-based)</param>
    /// <returns>Dictionary of currency types and total amounts to reward</returns>
    public Dictionary<CurrencyType, int> GetRewardsForLevel(int level)
    {
        var totalRewards = new Dictionary<CurrencyType, int>();
        
        // Check if there's a custom reward for this level
        var customReward = customLevelRewards.Find(lr => lr.levelNumber == level);
        
        List<Reward> rewardsToApply;
        if (customReward != null)
        {
            // Use custom rewards if available
            rewardsToApply = customReward.GetValidRewards();
        }
        else
        {
            // Use default rewards
            rewardsToApply = GetValidDefaultRewards();
        }
        
        // Convert rewards to dictionary
        foreach (var reward in rewardsToApply)
        {
            if (totalRewards.ContainsKey(reward.currencyType))
            {
                totalRewards[reward.currencyType] += reward.amount;
            }
            else
            {
                totalRewards[reward.currencyType] = reward.amount;
            }
        }
        
        return totalRewards;
    }
    
    /// <summary>
    /// Get a preview of rewards for multiple levels
    /// </summary>
    /// <param name="startLevel">Starting level</param>
    /// <param name="count">Number of levels to preview</param>
    /// <returns>Dictionary with level numbers as keys and reward dictionaries as values</returns>
    public Dictionary<int, Dictionary<CurrencyType, int>> GetRewardPreview(int startLevel, int count)
    {
        var preview = new Dictionary<int, Dictionary<CurrencyType, int>>();
        
        for (int level = startLevel; level < startLevel + count; level++)
        {
            preview[level] = GetRewardsForLevel(level);
        }
        
        return preview;
    }
    
    /// <summary>
    /// Add a custom reward for a specific level
    /// </summary>
    public void AddCustomLevelReward(int level, List<Reward> rewards)
    {
        // Remove existing custom reward for this level
        customLevelRewards.RemoveAll(lr => lr.levelNumber == level);
        
        // Add new custom reward
        var levelReward = new LevelReward
        {
            levelNumber = level,
            rewards = new List<Reward>(rewards)
        };
        
        customLevelRewards.Add(levelReward);
        
        // Sort by level number
        customLevelRewards.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));
    }
    
    /// <summary>
    /// Remove custom reward for a specific level (will fall back to default rewards)
    /// </summary>
    public void RemoveCustomLevelReward(int level)
    {
        customLevelRewards.RemoveAll(lr => lr.levelNumber == level);
    }
    
    /// <summary>
    /// Check if a level has custom rewards
    /// </summary>
    public bool HasCustomRewardForLevel(int level)
    {
        return customLevelRewards.Exists(lr => lr.levelNumber == level);
    }
    
    /// <summary>
    /// Get valid default rewards
    /// </summary>
    private List<Reward> GetValidDefaultRewards()
    {
        var validRewards = new List<Reward>();
        foreach (var reward in defaultRewards)
        {
            if (reward.IsValid)
                validRewards.Add(reward);
        }
        return validRewards;
    }
    
    /// <summary>
    /// Initialize with default configuration
    /// </summary>
    [ContextMenu("Initialize Default Configuration")]
    public void InitializeDefaultConfiguration()
    {
        // Clear existing data
        defaultRewards.Clear();
        customLevelRewards.Clear();
        
        // Set default rewards (50 coins for all levels)
        defaultRewards.Add(new Reward(CurrencyType.Coin, 50));
        
        // Add some custom level rewards as examples
        // Every 5th level gets extra rewards
        for (int level = 5; level <= 50; level += 5)
        {
            var customRewards = new List<Reward>
            {
                new Reward(CurrencyType.Coin, 100), // Double coins
                new Reward(CurrencyType.BoosterHammer, 1)
            };
            
            AddCustomLevelReward(level, customRewards);
        }
        
        // Every 10th level gets even more rewards
        for (int level = 10; level <= 50; level += 10)
        {
            var customRewards = new List<Reward>
            {
                new Reward(CurrencyType.Coin, 150), // Triple coins
                new Reward(CurrencyType.BoosterHammer, 1),
                new Reward(CurrencyType.BoosterShuffle, 1)
            };
            
            AddCustomLevelReward(level, customRewards);
        }
        
        // Special milestone rewards
        AddCustomLevelReward(25, new List<Reward> 
        { 
            new Reward(CurrencyType.Coin, 500), 
            new Reward(CurrencyType.Lives, 2),
            new Reward(CurrencyType.BoosterHammer, 2)
        });
        
        AddCustomLevelReward(50, new List<Reward> 
        { 
            new Reward(CurrencyType.Coin, 1000), 
            new Reward(CurrencyType.Lives, 5),
            new Reward(CurrencyType.BoosterHammer, 3),
            new Reward(CurrencyType.BoosterShuffle, 2)
        });
        
        Debug.Log("EndLevelRewardConfig initialized with default configuration!");
    }
    
    /// <summary>
    /// Validate the configuration
    /// </summary>
    private void OnValidate()
    {
        // Remove invalid default rewards
        for (int i = defaultRewards.Count - 1; i >= 0; i--)
        {
            if (defaultRewards[i].amount <= 0)
            {
                Debug.LogWarning($"Invalid default reward amount: {defaultRewards[i].amount}. Reward removed.");
                defaultRewards.RemoveAt(i);
            }
        }
        
        // Remove invalid custom level rewards
        for (int i = customLevelRewards.Count - 1; i >= 0; i--)
        {
            var levelReward = customLevelRewards[i];
            
            // Remove invalid level numbers
            if (levelReward.levelNumber <= 0)
            {
                Debug.LogWarning($"Invalid level number: {levelReward.levelNumber}. Level reward removed.");
                customLevelRewards.RemoveAt(i);
                continue;
            }
            
            // Remove rewards with invalid amounts
            for (int j = levelReward.rewards.Count - 1; j >= 0; j--)
            {
                if (levelReward.rewards[j].amount <= 0)
                {
                    Debug.LogWarning($"Invalid reward amount in level {levelReward.levelNumber}: {levelReward.rewards[j].amount}. Reward removed.");
                    levelReward.rewards.RemoveAt(j);
                }
            }
        }
        
        // Sort custom level rewards by level number
        customLevelRewards.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));
    }
}
