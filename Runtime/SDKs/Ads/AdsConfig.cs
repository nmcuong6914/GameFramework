using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BlockSort.Ads
{
    /// <summary>
    /// Configuration for ads system including IronSource settings and ad placements
    /// </summary>
    [CreateAssetMenu(fileName = "AdsConfig", menuName = "BlockSort/Ads/Ads Config", order = 1)]
    public class AdsConfig : ScriptableObject
    {
        [Header("General Settings")]
        [SerializeField] public bool adsEnabled = true;
        [SerializeField] public bool testMode = true;
        [SerializeField] public bool enableDebugLogs = true;

        [Header("Cooldown Settings")]
        [Tooltip("If enabled, showing any interstitial ad triggers a cooldown for ALL interstitial placements")]
        [SerializeField] public bool useSharedInterstitialCooldown = true;
        [Tooltip("Global cooldown time in seconds for all interstitial ads when shared cooldown is enabled")]
        [SerializeField] public float sharedInterstitialCooldown = 60f;

        [Header("Platform Settings")]
        [SerializeField] public PlatformSettings iosSettings = new PlatformSettings();
        [SerializeField] public PlatformSettings androidSettings = new PlatformSettings();

        [Header("Ad Placements")]
        [SerializeReference] public List<InterstitialAdPlacement> interstitialPlacements = new List<InterstitialAdPlacement>();
        [SerializeReference] public List<RewardAdPlacement> rewardPlacements = new List<RewardAdPlacement>();

        private void OnEnable()
        {
            InitializeDefaultPlacements();
        }

        /// <summary>
        /// Initialize default ad placements if lists are empty
        /// </summary>
        private void InitializeDefaultPlacements()
        {
            if (interstitialPlacements.Count == 0)
            {
                // Add default interstitial placements
                interstitialPlacements.Add(new LevelCompleteInterstitialAd 
                { 
                    name = "LevelComplete",
                    description = "Show after completing levels",
                    unlockLevel = 1,
                    isEnabled = true,
                    cooldown = 60f
                });

                interstitialPlacements.Add(new RestartLevelInterstitialAd 
                { 
                    name = "RestartLevel",
                    description = "Show when restarting levels",
                    unlockLevel = 5,
                    isEnabled = true,
                    cooldown = 120f
                });
            }

            if (rewardPlacements.Count == 0)
            {
                // Add default reward placements
                rewardPlacements.Add(new MultipleCoinRewardAd 
                { 
                    name = "MultipleCoin",
                    description = "Get multiple coins reward",
                    unlockLevel = 1,
                    isEnabled = true,
                    cooldown = 30f
                });

                rewardPlacements.Add(new ExtraLiveRewardAd 
                { 
                    name = "ExtraLive",
                    description = "Get extra lives",
                    unlockLevel = 1,
                    isEnabled = true,
                    cooldown = 300f // 5 minutes
                });

                rewardPlacements.Add(new BonusTimeRewardAd 
                { 
                    name = "bonus_time",
                    description = "Get bonus time to continue level",
                    unlockLevel = 1,
                    isEnabled = true,
                    cooldown = 0f, // No cooldown for bonus time
                    maxShowPerDay = 1 // Once per day
                });
            }
        }

        /// <summary>
        /// Get platform-specific settings based on current platform
        /// </summary>
        public PlatformSettings GetCurrentPlatformSettings()
        {
#if UNITY_IOS
            return iosSettings;
#elif UNITY_ANDROID
            return androidSettings;
#else
            return null; // Default settings for editor/other platforms
#endif
        }

        /// <summary>
        /// Get the app ID for the current platform
        /// </summary>
        public string GetCurrentAppId()
        {
            var platformSettings = GetCurrentPlatformSettings();
            return !string.IsNullOrEmpty(platformSettings.platformAppId) 
                ? platformSettings.platformAppId 
                : "YOUR_IRONSOURCE_APP_KEY_HERE";
        }

        /// <summary>
        /// Get interstitial ad unit ID for the current platform
        /// </summary>
        public string interstitialAdUnitId
        {
            get
            {
                var platformSettings = GetCurrentPlatformSettings();
                return !string.IsNullOrEmpty(platformSettings?.interstitialAdUnitId) 
                    ? platformSettings.interstitialAdUnitId 
                    : "DefaultInterstitial";
            }
        }

        /// <summary>
        /// Get rewarded ad unit ID for the current platform
        /// </summary>
        public string rewardedAdUnitId
        {
            get
            {
                var platformSettings = GetCurrentPlatformSettings();
                return !string.IsNullOrEmpty(platformSettings?.rewardedAdUnitId) 
                    ? platformSettings.rewardedAdUnitId 
                    : "DefaultRewarded";
            }
        }

        /// <summary>
        /// Get interstitial ad placement by name
        /// </summary>
        public InterstitialAdPlacement GetInterstitialPlacement(string placementName)
        {
            return interstitialPlacements.Find(p => p.name == placementName);
        }

        /// <summary>
        /// Get reward ad placement by name
        /// </summary>
        public RewardAdPlacement GetRewardPlacement(string placementName)
        {
            return rewardPlacements.Find(p => p.name == placementName);
        }

        /// <summary>
        /// Get any ad placement by name (searches both lists)
        /// </summary>
        public AdPlacement GetPlacement(string placementName)
        {
            var interstitial = GetInterstitialPlacement(placementName);
            if (interstitial != null) return interstitial;
            
            return GetRewardPlacement(placementName);
        }

        /// <summary>
        /// Check if interstitial ads are allowed for a specific placement
        /// </summary>
        public bool CanShowInterstitial(string placementName, int currentLevel = 0)
        {
            if (!adsEnabled) return false;
            
            var placement = GetInterstitialPlacement(placementName);
            if (placement == null) return false;
            
            return placement.isEnabled && currentLevel >= placement.unlockLevel && placement.IsPassedCondition(currentLevel);
        }

        /// <summary>
        /// Check if rewarded ads are allowed for a specific placement
        /// </summary>
        public bool CanShowRewarded(string placementName, int currentLevel = 0)
        {
            if (!adsEnabled) return false;
            
            var placement = GetRewardPlacement(placementName);
            if (placement == null) return false;
            
            return placement.isEnabled && currentLevel >= placement.unlockLevel && placement.IsPassedCondition(currentLevel);
        }

        /// <summary>
        /// Add a new interstitial ad placement
        /// </summary>
        public void AddInterstitialPlacement(InterstitialAdPlacement placement)
        {
            if (placement == null) return;
            interstitialPlacements.Add(placement);
        }

        /// <summary>
        /// Add a new reward ad placement
        /// </summary>
        public void AddRewardPlacement(RewardAdPlacement placement)
        {
            if (placement == null) return;
            rewardPlacements.Add(placement);
        }

        /// <summary>
        /// Remove an interstitial ad placement by name
        /// </summary>
        public bool RemoveInterstitialPlacement(string placementName)
        {
            var placement = GetInterstitialPlacement(placementName);
            if (placement == null) return false;
            return interstitialPlacements.Remove(placement);
        }

        /// <summary>
        /// Remove a reward ad placement by name
        /// </summary>
        public bool RemoveRewardPlacement(string placementName)
        {
            var placement = GetRewardPlacement(placementName);
            if (placement == null) return false;
            return rewardPlacements.Remove(placement);
        }

        /// <summary>
        /// Get all available placement names for interstitial ads
        /// </summary>
        public string[] GetInterstitialPlacementNames()
        {
            return interstitialPlacements.ConvertAll(p => p.name).ToArray();
        }

        /// <summary>
        /// Get all available placement names for reward ads
        /// </summary>
        public string[] GetRewardPlacementNames()
        {
            return rewardPlacements.ConvertAll(p => p.name).ToArray();
        }
    }

    [System.Serializable]
    public class PlatformSettings
    {
        [SerializeField] public string platformAppId = "YOUR_PLATFORM_SPECIFIC_APP_ID";
        [SerializeField] public string interstitialAdUnitId = "DefaultInterstitial";
        [SerializeField] public string rewardedAdUnitId = "DefaultRewarded";
        [SerializeField] public bool platformTestMode = false;
        [SerializeField] public bool coppaCompliant = false;
        [SerializeField] public bool gdprCompliant = true;
        [SerializeField] public bool ccpaCompliant = false;
    }

    [System.Serializable]
    public abstract class AdPlacement
    {
        [SerializeField] public string name = "";
        [SerializeField] public string description = "";
        [SerializeField] public int unlockLevel = 0;
        [SerializeField] public bool isEnabled = true;
        [SerializeField] public float cooldown = 0f;
        [SerializeField] public int maxShowPerDay = -1; // -1 means unlimited

        public abstract AdType GetAdType();
        
        /// <summary>
        /// Check if this placement's specific conditions are met to show the ad
        /// </summary>
        /// <param name="currentLevel">Current player level</param>
        /// <returns>True if conditions are passed and ad should be shown</returns>
        public abstract bool IsPassedCondition(int currentLevel);
    }

    [System.Serializable]
    public abstract class InterstitialAdPlacement : AdPlacement
    {
        public override AdType GetAdType() => AdType.Interstitial;
    }

    [System.Serializable]
    public abstract class RewardAdPlacement : AdPlacement
    {
        public override AdType GetAdType() => AdType.Rewarded;
    }

    // Specific Interstitial Ad Types
    [System.Serializable]
    public class LevelCompleteInterstitialAd : InterstitialAdPlacement
    {
        public override bool IsPassedCondition(int currentLevel)
        {
            // Level complete ads can always be shown (subject to cooldown and other checks)
            return true;
        }
    }

    [System.Serializable]
    public class RestartLevelInterstitialAd : InterstitialAdPlacement
    {
        // No additional properties needed for restart level ads
        
        public override bool IsPassedCondition(int currentLevel)
        {
            // Restart level ads can always be shown (subject to basic enabled/unlock level checks)
            return true;
        }
    }

    // Specific Reward Ad Types
    [System.Serializable]
    public class MultipleCoinRewardAd : RewardAdPlacement
    {
        public override bool IsPassedCondition(int currentLevel)
        {
            // Multiple coin reward ads can always be shown (subject to basic enabled/unlock level checks)
            return true;
        }
    }

    [System.Serializable]
    public class ExtraLiveRewardAd : RewardAdPlacement
    {
        public override bool IsPassedCondition(int currentLevel)
        {
            // Extra live reward ads can always be shown (subject to basic enabled/unlock level checks)
            return true;
        }
    }

    [System.Serializable]
    public class BonusTimeRewardAd : RewardAdPlacement
    {
        public override bool IsPassedCondition(int currentLevel)
        {
            // Bonus time reward ads can always be shown (subject to basic enabled/unlock level checks)
            return true;
        }
    }

    public enum AdType
    {
        Interstitial,
        Rewarded
    }
}
