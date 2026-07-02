using UnityEngine;

namespace BlockSort.Ads
{
    /// <summary>
    /// Utility class for ads system constants and helpers
    /// </summary>
    public static class AdsConstants
    {
        // Common placement names
        public const string LEVEL_COMPLETE = "level_complete";
        public const string RESTART_LEVEL = "restart_level";
        public const string GAME_OVER = "game_over";
        public const string EXTRA_LIVES = "extra_lives";
        public const string DOUBLE_COINS = "double_coins";
        public const string PLACEMENT_BONUS_TIME = "bonus_time";
    
        
        // Default cooldowns (in seconds)
        public const float DEFAULT_INTERSTITIAL_COOLDOWN = 60f;
        public const float DEFAULT_PLACEMENT_COOLDOWN = 30f;
        
        // Default session limits
        public const int DEFAULT_MAX_INTERSTITIALS_PER_SESSION = 10;
        public const int DEFAULT_MAX_REWARDED_PER_PLACEMENT = 5;
    }

    /// <summary>
    /// Editor-only utility for creating default ads configurations
    /// </summary>
    public static class AdsConfigHelper
    {
        /// <summary>
        /// Create default interstitial ad placements
        /// </summary>
        public static InterstitialAdPlacement[] CreateDefaultInterstitialPlacements()
        {
            return new InterstitialAdPlacement[]
            {
                new LevelCompleteInterstitialAd
                {
                    name = AdsConstants.LEVEL_COMPLETE,
                    description = "After completing a level",
                    unlockLevel = 0,
                    isEnabled = true,
                    cooldown = 60f,
                    maxShowPerDay = -1
                },
                new RestartLevelInterstitialAd
                {
                    name = AdsConstants.GAME_OVER,
                    description = "When player runs out of lives",
                    unlockLevel = 0,
                    isEnabled = true,
                    cooldown = 30f,
                    maxShowPerDay = 3
                }
            };
        }

        /// <summary>
        /// Create default reward ad placements
        /// </summary>
        public static RewardAdPlacement[] CreateDefaultRewardPlacements()
        {
            return new RewardAdPlacement[]
            {
                new ExtraLiveRewardAd
                {
                    name = AdsConstants.EXTRA_LIVES,
                    description = "To get extra lives",
                    unlockLevel = 0,
                    isEnabled = true,
                    cooldown = 120f,
                    maxShowPerDay = 5
                },
                new MultipleCoinRewardAd
                {
                    name = AdsConstants.DOUBLE_COINS,
                    description = "To double level rewards",
                    unlockLevel = 5,
                    isEnabled = true,
                    cooldown = 300f,
                    maxShowPerDay = 2
                },
                new BonusTimeRewardAd
                {
                    name = AdsConstants.PLACEMENT_BONUS_TIME,
                    description = "Bonus time for continuing level",
                    unlockLevel = 1,
                    isEnabled = true,
                    cooldown = 0f, // No cooldown for bonus time
                    maxShowPerDay = 1 // Once per day
                }
            };
        }

        /// <summary>
        /// Create default ad placements for a new ads config (legacy support)
        /// </summary>
        [System.Obsolete("Use CreateDefaultInterstitialPlacements() and CreateDefaultRewardPlacements() instead")]
        public static AdPlacement[] CreateDefaultPlacements()
        {
            // Return empty array since AdPlacement is now abstract
            // Consumers should use the specific methods above
            return new AdPlacement[0];
        }
    }
}
