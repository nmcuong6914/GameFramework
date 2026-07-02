using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Extension methods for PlayerDataManager to integrate with the reward system
/// This is a separate file to avoid compilation order issues
/// </summary>
public static class PlayerDataManagerRewardExtensions
{
    /// <summary>
    /// Reward player for completing a level using the reward configuration
    /// </summary>
    public static void RewardLevelCompletionWithConfig(this PlayerDataManager playerDataManager, int levelNumber, EndLevelRewardConfig rewardConfig)
    {
        Dictionary<CurrencyType, int> rewards;
        
        // Use configured rewards if available, otherwise fall back to default calculation
        if (rewardConfig != null)
        {
            rewards = rewardConfig.GetRewardsForLevel(levelNumber);
        }
        else
        {
            // Fallback to hardcoded rewards if no configuration is assigned
            rewards = GetDefaultLevelRewards(levelNumber);
        }
        
        playerDataManager.AddMultiple(rewards);
        
        // Log rewards for debugging
        var rewardStrings = rewards.Select(kvp => $"{kvp.Key}: {kvp.Value}").ToArray();
        Debug.Log($"Level {levelNumber} completion rewards: {string.Join(", ", rewardStrings)}");
    }
    
    /// <summary>
    /// Get preview of rewards for a specific level without actually giving them
    /// </summary>
    public static Dictionary<CurrencyType, int> GetLevelRewardPreviewWithConfig(this PlayerDataManager playerDataManager, int levelNumber, EndLevelRewardConfig rewardConfig)
    {
        if (rewardConfig != null)
        {
            return rewardConfig.GetRewardsForLevel(levelNumber);
        }
        else
        {
            return GetDefaultLevelRewards(levelNumber);
        }
    }
    
    /// <summary>
    /// Get reward preview for multiple levels
    /// </summary>
    public static Dictionary<int, Dictionary<CurrencyType, int>> GetMultipleLevelRewardPreviewWithConfig(this PlayerDataManager playerDataManager, int startLevel, int levelCount, EndLevelRewardConfig rewardConfig)
    {
        if (rewardConfig != null)
        {
            return rewardConfig.GetRewardPreview(startLevel, levelCount);
        }
        else
        {
            // Generate preview using default rewards
            var preview = new Dictionary<int, Dictionary<CurrencyType, int>>();
            for (int level = startLevel; level < startLevel + levelCount; level++)
            {
                preview[level] = GetDefaultLevelRewards(level);
            }
            return preview;
        }
    }
    
    /// <summary>
    /// Get default rewards for level completion (used as fallback)
    /// </summary>
    private static Dictionary<CurrencyType, int> GetDefaultLevelRewards(int levelNumber)
    {
        // Original hardcoded reward logic
        return new Dictionary<CurrencyType, int>
        {
            { CurrencyType.Coin, 50 + (levelNumber * 10) }, // Base 50 + 10 per level
            { CurrencyType.BoosterHammer, levelNumber % 5 == 0 ? 1 : 0 }, // 1 hammer every 5 levels
            { CurrencyType.BoosterHint, levelNumber % 10 == 0 ? 1 : 0 }    // 1 hint every 10 levels
        };
    }
}
