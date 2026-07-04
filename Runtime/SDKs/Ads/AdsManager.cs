using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Unity.Services.LevelPlay;
using Analytics;

namespace BlockSort.Ads
{
    /// <summary>
    /// Complete ads manager for IronSource/LevelPlay integration
    /// Handles both interstitial and rewarded video ads with event callbacks
    /// Registered with ServiceLocator - use ServiceLocator.TryResolve<AdsManager>() to access
    /// </summary>
    public class AdsManager
    {
        private AdsConfig adsConfig;

        // State tracking
        private bool isInitialized = false;
        private bool isInterstitialReady = false;
        private bool isRewardedReady = false;
        
        // Cooldown tracking
        private float lastInterstitialShowTime = -1f; // -1 indicates no ad has been shown yet
        private Dictionary<string, float> placementCooldowns = new Dictionary<string, float>();
        
        // Current ad callbacks
        private Action onInterstitialClosed;
        private Action<string> onRewardedCompleted;
        private Action onRewardedFailed;
        
        // LevelPlay ad instances
        private LevelPlayInterstitialAd interstitialAd;
        private LevelPlayRewardedAd rewardedAd;
        
        // Services
        private IAdDataProvider dataProvider;
        private SignalBus signalBus;

        // Events
        public event Action<bool> OnInitializationComplete;
        public event Action OnInterstitialLoaded;
        public event Action OnInterstitialFailed;
        public event Action<string> OnInterstitialClosed;
        public event Action OnRewardedLoaded;
        public event Action OnRewardedFailed;
        public event Action<string> OnRewardedCompleted;

        // Properties
        public bool IsInitialized => isInitialized;
        public bool IsInterstitialReady => isInterstitialReady;
        public bool IsRewardedReady => isRewardedReady;
        public bool AdsEnabled => adsConfig != null && adsConfig.adsEnabled;

        // Public constructor for ServiceLocator registration
        public AdsManager() { }

        /// <summary>
        /// Subscribe to LevelPlay SDK initialization events
        /// </summary>
        private void SubscribeLevelPlayInitEvents()
        {
            LevelPlay.OnInitSuccess += OnLevelPlayInitSuccess;
            LevelPlay.OnInitFailed += OnLevelPlayInitFailed;
            Debug.Log("AdsManager: LevelPlay SDK initialization events subscribed");
        }

        /// <summary>
        /// Unsubscribe from LevelPlay SDK initialization events
        /// </summary>
        private void UnsubscribeLevelPlayInitEvents()
        {
            LevelPlay.OnInitSuccess -= OnLevelPlayInitSuccess;
            LevelPlay.OnInitFailed -= OnLevelPlayInitFailed;
            Debug.Log("AdsManager: LevelPlay SDK initialization events unsubscribed");
        }

        /// <summary>
        /// Subscribe to interstitial ad events
        /// </summary>
        private void SubscribeInterstitialEvents()
        {
            if (interstitialAd == null) return;

            interstitialAd.OnAdLoaded += OnInterstitialAdLoaded;
            interstitialAd.OnAdLoadFailed += OnInterstitialAdLoadFailed;
            interstitialAd.OnAdDisplayed += OnInterstitialAdDisplayed;
            interstitialAd.OnAdClosed += OnInterstitialAdClosed;
            interstitialAd.OnAdClicked += OnInterstitialAdClicked;
            interstitialAd.OnAdDisplayFailed += OnInterstitialAdDisplayFailed;
            interstitialAd.OnAdInfoChanged += OnInterstitialAdInfoChanged;
            
            Debug.Log("AdsManager: Interstitial ad events subscribed");
        }

        /// <summary>
        /// Unsubscribe from interstitial ad events
        /// </summary>
        private void UnsubscribeInterstitialEvents()
        {
            if (interstitialAd == null) return;

            interstitialAd.OnAdLoaded -= OnInterstitialAdLoaded;
            interstitialAd.OnAdLoadFailed -= OnInterstitialAdLoadFailed;
            interstitialAd.OnAdDisplayed -= OnInterstitialAdDisplayed;
            interstitialAd.OnAdClosed -= OnInterstitialAdClosed;
            interstitialAd.OnAdClicked -= OnInterstitialAdClicked;
            interstitialAd.OnAdDisplayFailed -= OnInterstitialAdDisplayFailed;
            interstitialAd.OnAdInfoChanged -= OnInterstitialAdInfoChanged;
            
            Debug.Log("AdsManager: Interstitial ad events unsubscribed");
        }

        /// <summary>
        /// Subscribe to rewarded ad events
        /// </summary>
        private void SubscribeRewardedEvents()
        {
            if (rewardedAd == null) return;

            rewardedAd.OnAdLoaded += OnRewardedAdLoaded;
            rewardedAd.OnAdLoadFailed += OnRewardedAdLoadFailed;
            rewardedAd.OnAdDisplayed += OnRewardedAdDisplayed;
            rewardedAd.OnAdDisplayFailed += OnRewardedAdDisplayFailed;
            rewardedAd.OnAdRewarded += OnRewardedAdRewarded;
            rewardedAd.OnAdClicked += OnRewardedAdClicked;
            rewardedAd.OnAdClosed += OnRewardedAdClosed;
            rewardedAd.OnAdInfoChanged += OnRewardedAdInfoChanged;
            
            Debug.Log("AdsManager: Rewarded ad events subscribed");
        }

        /// <summary>
        /// Unsubscribe from rewarded ad events
        /// </summary>
        private void UnsubscribeRewardedEvents()
        {
            if (rewardedAd == null) return;

            rewardedAd.OnAdLoaded -= OnRewardedAdLoaded;
            rewardedAd.OnAdLoadFailed -= OnRewardedAdLoadFailed;
            rewardedAd.OnAdDisplayed -= OnRewardedAdDisplayed;
            rewardedAd.OnAdDisplayFailed -= OnRewardedAdDisplayFailed;
            rewardedAd.OnAdRewarded -= OnRewardedAdRewarded;
            rewardedAd.OnAdClicked -= OnRewardedAdClicked;
            rewardedAd.OnAdClosed -= OnRewardedAdClosed;
            rewardedAd.OnAdInfoChanged -= OnRewardedAdInfoChanged;
            
            Debug.Log("AdsManager: Rewarded ad events unsubscribed");
        }

        #region LevelPlay SDK Event Handlers

        private void OnLevelPlayInitSuccess(LevelPlayConfiguration configuration)
        {
            Debug.Log("AdsManager: LevelPlay SDK initialized successfully");
            isInitialized = true;
            
            // Create and initialize ad instances
            CreateAdInstances();
            
            OnInitializationComplete?.Invoke(true);
        }

        private void OnLevelPlayInitFailed(LevelPlayInitError error)
        {
            Debug.LogError($"AdsManager: LevelPlay initialization failed: {error.ErrorMessage}");
            OnInitializationComplete?.Invoke(false);
        }

        /// <summary>
        /// Create interstitial and rewarded ad instances
        /// </summary>
        private void CreateAdInstances()
        {
            // Create interstitial ad instance if any placement is enabled
            if (adsConfig.interstitialPlacements.Any(p => p.isEnabled))
            {
                // Use the first enabled placement's ad unit ID, or a default one
                string adUnitId = adsConfig.interstitialAdUnitId ?? "DefaultInterstitial";
                interstitialAd = new LevelPlayInterstitialAd(adUnitId);
                SubscribeInterstitialEvents();
                LoadInterstitial();
                Debug.Log("AdsManager: Interstitial ad instance created");
            }

            // Create rewarded ad instance if any placement is enabled
            if (adsConfig.rewardPlacements.Any(p => p.isEnabled))
            {
                // Use the first enabled placement's ad unit ID, or a default one
                string adUnitId = adsConfig.rewardedAdUnitId ?? "DefaultRewarded";
                rewardedAd = new LevelPlayRewardedAd(adUnitId);
                SubscribeRewardedEvents();
                LoadRewardedAd();
                Debug.Log("AdsManager: Rewarded ad instance created");
            }
        }

        #endregion

        #region Interstitial Ad Event Handlers

        private void OnInterstitialAdLoaded(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"AdsManager: Interstitial ad loaded - {adInfo}");
            isInterstitialReady = true;
            OnInterstitialLoaded?.Invoke();
        }

        private void OnInterstitialAdLoadFailed(LevelPlayAdError error)
        {
            Debug.LogError($"AdsManager: Interstitial ad load failed - {error}");
            isInterstitialReady = false;
            OnInterstitialFailed?.Invoke();
        }

        private void OnInterstitialAdDisplayed(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"AdsManager: Interstitial ad displayed - {adInfo}");
        }

        private void OnInterstitialAdClosed(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"AdsManager: Interstitial ad closed - {adInfo}");
            
            // Trigger event
            OnInterstitialClosed?.Invoke(adInfo?.PlacementName ?? "");
            
            // Invoke the callback
            var callback = onInterstitialClosed;
            onInterstitialClosed = null;
            callback?.Invoke();
            
            // Load next interstitial ad
            LoadInterstitial();
        }

        private void OnInterstitialAdClicked(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"AdsManager: Interstitial ad clicked - {adInfo}");
        }

        private void OnInterstitialAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
        {
            Debug.LogError($"AdsManager: Interstitial ad display failed - {error}");
            
            // Trigger failure event
            OnInterstitialFailed?.Invoke();
            
            // Invoke the callback
            var callback = onInterstitialClosed;
            onInterstitialClosed = null;
            callback?.Invoke();
            
            // Try to load another ad
            LoadInterstitial();
        }

        private void OnInterstitialAdInfoChanged(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"AdsManager: Interstitial ad info changed - {adInfo}");
        }

        #endregion

        #region Rewarded Ad Event Handlers

        private void OnRewardedAdLoaded(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"AdsManager: Rewarded ad loaded - {adInfo}");
            isRewardedReady = true;
            OnRewardedLoaded?.Invoke();
        }

        private void OnRewardedAdLoadFailed(LevelPlayAdError error)
        {
            Debug.LogError($"AdsManager: Rewarded ad load failed - {error}");
            isRewardedReady = false;
            OnRewardedFailed?.Invoke();
        }

        private void OnRewardedAdDisplayed(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"AdsManager: Rewarded ad displayed - {adInfo}");
        }

        private void OnRewardedAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
        {
            Debug.LogError($"AdsManager: Rewarded ad display failed - {error}");
            
            // Invoke failure callback
            var failCallback = onRewardedFailed;
            onRewardedCompleted = null;
            onRewardedFailed = null;
            failCallback?.Invoke();
            
            // Trigger failure event
            OnRewardedFailed?.Invoke();
            
            LoadRewardedAd(); // Try to load another ad
        }

        private void OnRewardedAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
        {
            Debug.Log($"AdsManager: Rewarded ad rewarded - {adInfo}, Reward: {reward?.Name} x{reward?.Amount}");
            
            string placementName = adInfo?.PlacementName ?? "default";
            
            // Track cooldown
            if (!placementCooldowns.ContainsKey(placementName))
            {
                placementCooldowns[placementName] = Time.unscaledTime;
            }

            // Track ad show in PlayerData for daily limit
            if (dataProvider != null)
            {
                dataProvider.IncrementAdShowCount(placementName);
            }

            // Trigger events
            OnRewardedCompleted?.Invoke(placementName);
            
            // Invoke the reward callback
            var callback = onRewardedCompleted;
            onRewardedCompleted = null;
            onRewardedFailed = null;
            callback?.Invoke(placementName);
        }

        private void OnRewardedAdClicked(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"AdsManager: Rewarded ad clicked - {adInfo}");
        }

        private void OnRewardedAdClosed(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"AdsManager: Rewarded ad closed - {adInfo}");
            
            // If callbacks haven't been invoked yet, the user closed without watching
            // The reward is only given in OnRewardedAdRewarded
            
            LoadRewardedAd(); // Preload next ad
        }

        private void OnRewardedAdInfoChanged(LevelPlayAdInfo adInfo)
        {
            Debug.Log($"AdsManager: Rewarded ad info changed - {adInfo}");
        }

        #endregion

        /// <summary>
        /// Initialize the ads system
        /// </summary>
        public async UniTask Initialize(AdsConfig config)
        {
            if (config == null)
            {
                Debug.LogError("AdsManager: AdsConfig is null!");
                OnInitializationComplete?.Invoke(false);
                return;
            }

            adsConfig = config;

            // Get IAdDataProvider reference
            dataProvider = ServiceLocator.TryResolve<IAdDataProvider>();
            if (dataProvider == null)
            {
                Debug.LogWarning("AdsManager: IAdDataProvider not found! Level-based ad placement checks will use level 0.");
            }
            else
            {
                // Reset daily ad counters on initialization
                dataProvider.ResetDailyAdCounters();
            }
            
            // Get SignalBus reference and subscribe to ad signals
            signalBus = ServiceLocator.TryResolve<SignalBus>();
            SubscribeToAdSignals();

            if (!adsConfig.adsEnabled)
            {
                Debug.Log("AdsManager: Ads are disabled in configuration");
                OnInitializationComplete?.Invoke(false);
                return;
            }

            if (string.IsNullOrEmpty(adsConfig.GetCurrentAppId()) || adsConfig.GetCurrentAppId() == "YOUR_IRONSOURCE_APP_KEY_HERE")
            {
                Debug.LogError("AdsManager: Invalid app ID in configuration!");
                OnInitializationComplete?.Invoke(false);
                return;
            }

            Debug.Log("AdsManager: Initializing LevelPlay SDK...");

            try
            {
                // Subscribe to SDK initialization events
                SubscribeLevelPlayInitEvents();
                
                // Enable integration debug mode
                LevelPlay.ValidateIntegration();
                Debug.Log("AdsManager: Integration debug mode enabled");
                
                // Initialize LevelPlay SDK using the modern API
                LevelPlay.Init(appKey: adsConfig.GetCurrentAppId(), userId: null);
                Debug.Log("AdsManager: LevelPlay.Init called");
            }
            catch (Exception e)
            {
                Debug.LogError($"AdsManager: Failed to initialize LevelPlay SDK: {e.Message}");
                OnInitializationComplete?.Invoke(false);
            }

            await UniTask.WaitUntil(() => isInitialized);
        }

        #region Signal Handling

        /// <summary>
        /// Subscribe to ad-related signals
        /// </summary>
        private void SubscribeToAdSignals()
        {
            if (signalBus == null)
            {
                Debug.LogWarning("AdsManager: SignalBus not available, cannot subscribe to ad signals");
                return;
            }

            signalBus.Subscribe<ShowInterstitialAdSignal>(OnShowInterstitialAd);
            signalBus.Subscribe<ShowRewardedAdSignal>(OnShowRewardedAd);
            
            Debug.Log("AdsManager: Subscribed to ad signals");
        }

        /// <summary>
        /// Unsubscribe from ad-related signals
        /// </summary>
        private void UnsubscribeFromAdSignals()
        {
            if (signalBus == null) return;

            signalBus.Unsubscribe<ShowInterstitialAdSignal>(OnShowInterstitialAd);
            signalBus.Unsubscribe<ShowRewardedAdSignal>(OnShowRewardedAd);
            
            Debug.Log("AdsManager: Unsubscribed from ad signals");
        }

        private void OnShowInterstitialAd(ShowInterstitialAdSignal signal)
        {
            // Get the placement configuration
            var placement = adsConfig.GetInterstitialPlacement(signal.Placement);
            if (placement == null)
            {
                if (adsConfig.enableDebugLogs)
                    Debug.LogWarning($"AdsManager: No interstitial placement found for '{signal.Placement}'");
                signal.OnClosed?.Invoke();
                return;
            }

            // Get current level from PlayerDataManager if available
            int currentLevel = 0;
            if (dataProvider != null)
            {
                currentLevel = dataProvider.CurrentLevelIndex;
            }

            ShowInterstitial(signal.Placement, signal.OnClosed);
        }

        private void OnShowRewardedAd(ShowRewardedAdSignal signal)
        {
            // Get the placement configuration
            var placement = adsConfig.GetRewardPlacement(signal.Placement);
            if (placement == null)
            {
                if (adsConfig.enableDebugLogs)
                    Debug.LogWarning($"AdsManager: No reward placement found for '{signal.Placement}'");
                signal.OnFailed?.Invoke();
                return;
            }

            // Get current level from PlayerDataManager if available
            int currentLevel = 0;
            if (dataProvider != null)
            {
                currentLevel = dataProvider.CurrentLevelIndex;
            }

            // Use polymorphic condition checking
            if (placement.IsPassedCondition(currentLevel))
            {
                ShowRewarded(signal.Placement, 
                    (placementName) => signal.OnCompleted?.Invoke(),
                    signal.OnFailed);
            }
            else
            {
                if (adsConfig.enableDebugLogs)
                    Debug.Log($"AdsManager: Reward placement '{signal.Placement}' conditions not met for level {currentLevel}");
                signal.OnFailed?.Invoke();
            }
        }

        #endregion

        #region Interstitial Ads

        /// <summary>
        /// Show interstitial ad for specific placement
        /// </summary>
        public async void ShowInterstitial(string placement, Action onClosed = null)
        {
            // Check if player is paid - paid users don't see interstitial ads
            if (dataProvider != null && dataProvider.IsPaid)
            {
                if (adsConfig.enableDebugLogs)
                    Debug.Log($"AdsManager: Interstitial ad blocked for placement '{placement}' - player has made IAP purchase");
                onClosed?.Invoke();
                return;
            }

            if (!CanShowInterstitialInternal(placement))
            {
                Debug.Log($"AdsManager: Cannot show interstitial for placement '{placement}'");
                onClosed?.Invoke();
                return;
            }

            if (interstitialAd == null)
            {
                Debug.LogError("AdsManager: Interstitial ad instance is null!");
                onClosed?.Invoke();
                return;
            }

            onInterstitialClosed = onClosed;

            Debug.Log($"AdsManager: Showing interstitial ad for placement '{placement}'");
            
            try
            {
                // Show interstitial using LevelPlay API
                interstitialAd.ShowAd(placement);
                Debug.Log($"AdsManager: Interstitial ad show initiated for placement '{placement}'");
                
#if UNITY_EDITOR
                // In Unity Editor, simulate the ad callback since SDK doesn't run
                await UniTask.Delay(1000); // Simulate ad watching time
                
                // Manually trigger the closed callback (simulating LevelPlay callback)
                Debug.Log("AdsManager: [UNITY_EDITOR] Simulating OnInterstitialAdClosed callback");
                OnInterstitialAdClosed(null);
#endif
            }
            catch (Exception e)
            {
                Debug.LogError($"AdsManager: Failed to show interstitial: {e.Message}");
                var callback = onInterstitialClosed;
                onInterstitialClosed = null;
                callback?.Invoke();
                return;
            }
            
            // Track cooldown and daily limits immediately when showing
            lastInterstitialShowTime = Time.unscaledTime;
            placementCooldowns[placement] = Time.unscaledTime;
            
            // Track ad show in PlayerData for daily limit
            if (dataProvider != null)
            {
                dataProvider.IncrementAdShowCount(placement);
            }
        }

        /// <summary>
        /// Load interstitial ad
        /// </summary>
        public void LoadInterstitial()
        {
            // Check if any interstitial placements are enabled
            if (!adsConfig.interstitialPlacements.Any(p => p.isEnabled)) return;
            
            if (interstitialAd == null)
            {
                Debug.LogWarning("AdsManager: Interstitial ad instance is null, cannot load");
                return;
            }
            
            Debug.Log("AdsManager: Loading interstitial ad");
            
            #if UNITY_EDITOR
            // In Unity Editor, simulate loading
            isInterstitialReady = true;
            Debug.Log("AdsManager: [UNITY_EDITOR] Interstitial ad marked as ready for testing");
            OnInterstitialLoaded?.Invoke();
            #else
            try
            {
                interstitialAd.LoadAd();
                Debug.Log("AdsManager: Interstitial ad load initiated");
            }
            catch (Exception e)
            {
                Debug.LogError($"AdsManager: Failed to load interstitial: {e.Message}");
                isInterstitialReady = false;
            }
            #endif
        }

        private bool CanShowInterstitialInternal(string placement)
        {
            if (!isInitialized) return false;
            
            if (interstitialAd == null)
            {
                Debug.LogWarning("AdsManager: Interstitial ad instance is null");
                return false;
            }
            
            // Get current level from PlayerDataManager
            int currentLevel = 0;
            if (dataProvider != null)
            {
                currentLevel = dataProvider.CurrentLevelIndex;
            }
            
            if (adsConfig.enableDebugLogs)
            {
                Debug.Log($"AdsManager: Checking interstitial placement '{placement}' for level {currentLevel}");
            }
            
            if (!adsConfig.CanShowInterstitial(placement, currentLevel)) return false;

            // Check if interstitial is ready using LevelPlay API (bypass only in Unity Editor)
            #if !UNITY_EDITOR
            try
            {
                if (!interstitialAd.IsAdReady()) return false;
            }
            catch
            {
                return false;
            }
            #endif

            // Check cooldown based on configuration
            var placementData = adsConfig.GetPlacement(placement);
            
            if (adsConfig.useSharedInterstitialCooldown)
            {
                // Check global interstitial cooldown (skip check for first ad)
                if (lastInterstitialShowTime > 0 && Time.unscaledTime - lastInterstitialShowTime < adsConfig.sharedInterstitialCooldown)
                {
                    if (adsConfig.enableDebugLogs)
                        Debug.Log($"AdsManager: Interstitial '{placement}' blocked by shared cooldown. Time since last: {Time.unscaledTime - lastInterstitialShowTime:F1}s, required: {adsConfig.sharedInterstitialCooldown}s");
                    return false;
                }
            }
            else
            {
                // Check placement-specific cooldown
                if (placementData != null && placementCooldowns.ContainsKey(placement))
                {
                    if (Time.unscaledTime - placementCooldowns[placement] < placementData.cooldown)
                    {
                        if (adsConfig.enableDebugLogs)
                            Debug.Log($"AdsManager: Interstitial '{placement}' blocked by placement cooldown. Time since last: {Time.unscaledTime - placementCooldowns[placement]:F1}s, required: {placementData.cooldown}s");
                        return false;
                    }
                }
            }

            // Check daily limits (tracked in PlayerData)
            if (placementData != null && placementData.maxShowPerDay > 0)
            {
                if (dataProvider != null)
                {
                    int showCountToday = dataProvider.GetAdShowCountToday(placement);
                    if (showCountToday >= placementData.maxShowPerDay)
                        return false;
                }
            }

            return true;
        }

        #endregion

        #region Rewarded Ads

        /// <summary>
        /// Show rewarded ad for specific placement
        /// </summary>
        public async void ShowRewarded(string placement, Action<string> onCompleted = null, Action onFailed = null)
        {
            if (!CanShowRewardedInternal(placement))
            {
                Debug.Log($"AdsManager: Cannot show rewarded ad for placement '{placement}'");
                onFailed?.Invoke();
                return;
            }

            if (rewardedAd == null)
            {
                Debug.LogError("AdsManager: Rewarded ad instance is null!");
                onFailed?.Invoke();
                return;
            }

            onRewardedCompleted = onCompleted;
            onRewardedFailed = onFailed;

            Debug.Log($"AdsManager: Showing rewarded ad for placement '{placement}'");

            // Fire analytics signal for reward video shown
            if (signalBus != null)
            {
                signalBus.Fire(new RewardVideoShownSignal(placement));
            }

            try
            {
                // Show rewarded ad using LevelPlay API
                rewardedAd.ShowAd(placement);
                Debug.Log($"AdsManager: Rewarded ad show initiated for placement '{placement}'");
                
#if UNITY_EDITOR
                // In Unity Editor, simulate the ad callback since SDK doesn't run
                await UniTask.Delay(1000); // Simulate ad watching time
                
                // Manually trigger the reward callback (simulating LevelPlay callback)
                Debug.Log("AdsManager: [UNITY_EDITOR] Simulating OnRewardedAdRewarded callback");
                OnRewardedAdRewarded(null, null);
#endif
            }
            catch (Exception e)
            {
                Debug.LogError($"AdsManager: Failed to show rewarded ad: {e.Message}");
                var failCallback = onRewardedFailed;
                onRewardedCompleted = null;
                onRewardedFailed = null;
                failCallback?.Invoke();
                return;
            }
        }

        /// <summary>
        /// Load rewarded ad
        /// </summary>
        public void LoadRewardedAd()
        {
            // Check if any reward placements are enabled
            if (!adsConfig.rewardPlacements.Any(p => p.isEnabled)) return;
            
            if (rewardedAd == null)
            {
                Debug.LogWarning("AdsManager: Rewarded ad instance is null, cannot load");
                return;
            }
            
            Debug.Log("AdsManager: Loading rewarded ad");
            
            #if UNITY_EDITOR
            // In Unity Editor, simulate loading
            isRewardedReady = true;
            Debug.Log("AdsManager: [UNITY_EDITOR] Rewarded ad marked as ready for testing");
            OnRewardedLoaded?.Invoke();
            #else
            try
            {
                rewardedAd.LoadAd();
                Debug.Log("AdsManager: Rewarded ad load initiated");
            }
            catch (Exception e)
            {
                Debug.LogError($"AdsManager: Failed to load rewarded ad: {e.Message}");
                isRewardedReady = false;
            }
            #endif
        }

        private bool CanShowRewardedInternal(string placement)
        {
            if (!isInitialized) return false;
            
            if (rewardedAd == null)
            {
                Debug.LogWarning("AdsManager: Rewarded ad instance is null");
                return false;
            }
            
            // Get current level from PlayerDataManager
            int currentLevel = 0;
            if (dataProvider != null)
            {
                currentLevel = dataProvider.CurrentLevelIndex;
            }
            
            if (adsConfig.enableDebugLogs)
            {
                Debug.Log($"AdsManager: Checking rewarded placement '{placement}' for level {currentLevel}");
            }
            
            if (!adsConfig.CanShowRewarded(placement, currentLevel)) return false;

            // Check if rewarded ad is ready using LevelPlay API (bypass only in Unity Editor)
            #if !UNITY_EDITOR
            try
            {
                if (!rewardedAd.IsAdReady()) return false;
            }
            catch
            {
                return false;
            }
            #endif

            // Check placement-specific cooldown
            var placementData = adsConfig.GetPlacement(placement);
            if (placementData != null && placementCooldowns.ContainsKey(placement))
            {
                if (Time.unscaledTime - placementCooldowns[placement] < placementData.cooldown)
                    return false;
            }

            // Check daily limits (tracked in PlayerData)
            if (placementData != null && placementData.maxShowPerDay > 0)
            {
                if (dataProvider != null)
                {
                    int showCountToday = dataProvider.GetAdShowCountToday(placement);
                    if (showCountToday >= placementData.maxShowPerDay)
                        return false;
                }
            }

            return true;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Check if interstitial ad can be shown for placement
        /// </summary>
        public bool CanShowInterstitial(string placement)
        {
            return CanShowInterstitialInternal(placement);
        }

        /// <summary>
        /// Check if rewarded ad can be shown for placement
        /// </summary>
        public bool CanShowRewarded(string placement)
        {
            return CanShowRewardedInternal(placement);
        }

        #endregion

        #region Testing & Debug Methods

        /// <summary>
        /// Check if we're running in Unity Editor (testing mode)
        /// </summary>
        public bool IsTestingMode
        {
            get
            {
                #if UNITY_EDITOR
                return true;
                #else
                return false;
                #endif
            }
        }

        /// <summary>
        /// Force show any rewarded ad for testing purposes (Unity Editor only)
        /// </summary>
        public void ForceShowRewardedForTesting(string placement, Action<string> onCompleted = null, Action onFailed = null)
        {
            #if UNITY_EDITOR
            Debug.Log($"AdsManager: [TESTING] Force showing rewarded ad for placement '{placement}'");
            ShowRewarded(placement, onCompleted, onFailed);
            #else
            Debug.LogWarning("AdsManager: ForceShowRewardedForTesting is only available in Unity Editor");
            onFailed?.Invoke();
            #endif
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Clean up resources and unsubscribe from events
        /// </summary>
        public void Dispose()
        {
            UnsubscribeFromAdSignals();
            UnsubscribeLevelPlayInitEvents();
            UnsubscribeInterstitialEvents();
            UnsubscribeRewardedEvents();
            
            // Dispose ad instances
            if (interstitialAd != null)
            {
                interstitialAd.DestroyAd();
                interstitialAd = null;
            }
            
            if (rewardedAd != null)
            {
                rewardedAd.DestroyAd();
                rewardedAd = null;
            }
            
            isInitialized = false;
            Debug.Log("AdsManager: Disposed and cleaned up resources");
        }

        #endregion
    }
}
