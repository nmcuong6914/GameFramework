using System.Collections.Generic;
using UnityEngine;

namespace Analytics
{
    /// <summary>
    /// Service that tracks analytics events based on game signals
    /// </summary>
    public class GameAnalyticsTracker : MonoBehaviour
    {
        private SignalBus signalBus;
        private TimerManager timerManager;

        private void Start()
        {
            // Get SignalBus from ServiceLocator
            signalBus = ServiceLocator.TryResolve<SignalBus>();
            if (signalBus == null)
            {
                Debug.LogWarning("[GameAnalyticsTracker] SignalBus not found in ServiceLocator.");
                return;
            }

            // Get TimerManager for checking level completion time
            timerManager = ServiceLocator.TryResolve<TimerManager>();

            SubscribeToSignals();
            SubscribeToPurchaseEvents();
            Debug.Log("[GameAnalyticsTracker] Analytics tracker initialized and listening for game signals");
        }

        private void SubscribeToSignals()
        {
            signalBus.Subscribe<WinGameSignal>(OnGameWon);
            signalBus.Subscribe<LoseGameSignal>(OnGameLost);
            signalBus.Subscribe<GameInitializationCompleteSignal>(OnGameInitializationComplete);
            signalBus.Subscribe<LevelLoadedSignal>(OnLevelStarted);
            signalBus.Subscribe<RetryLevelSignal>(OnLevelRestarted);
            signalBus.Subscribe<RetryLevelSignal>(OnReplayButtonClicked); // Track replay button separately
            signalBus.Subscribe<Analytics.PopupOpenedSignal>(OnPopupOpened); // Use Analytics.PopupOpenedSignal
            signalBus.Subscribe<RewardVideoShownSignal>(OnRewardVideoShown);
            
            // Shop and purchase events
            signalBus.Subscribe<ShopPurchaseAttemptSignal>(OnShopPurchaseAttempt);
            signalBus.Subscribe<ShopPurchaseCompletedSignal>(OnShopPurchaseCompleted);
            signalBus.Subscribe<ShopPurchaseFailedSignal>(OnShopPurchaseFailed);
            signalBus.Subscribe<BuyCurrencyCompletedSignal>(OnBuyCurrencyCompleted);
            signalBus.Subscribe<WatchAdForLivesCompletedSignal>(OnWatchAdForLivesCompleted);
            
            // Booster events
            signalBus.Subscribe<BoosterButtonClickedSignal>(OnBoosterButtonClicked);
            signalBus.Subscribe<BoosterUsedSignal>(OnBoosterUsed);
            signalBus.Subscribe<BoosterCancelledSignal>(OnBoosterCancelled);
        }

        private void SubscribeToPurchaseEvents()
        {
            Purchase.PurchaseEventManager.OnPurchaseStarted += OnPurchaseStarted;
            Purchase.PurchaseEventManager.OnPurchaseCompleted += OnPurchaseCompleted;
            Purchase.PurchaseEventManager.OnPurchaseFailed += OnPurchaseFailed;
        }

        private void OnDestroy()
        {
            if (signalBus != null)
            {
                signalBus.Unsubscribe<WinGameSignal>(OnGameWon);
                signalBus.Unsubscribe<LoseGameSignal>(OnGameLost);
                signalBus.Unsubscribe<GameInitializationCompleteSignal>(OnGameInitializationComplete);
                signalBus.Unsubscribe<LevelLoadedSignal>(OnLevelStarted);
                signalBus.Unsubscribe<RetryLevelSignal>(OnLevelRestarted);
                signalBus.Unsubscribe<RetryLevelSignal>(OnReplayButtonClicked);
                signalBus.Unsubscribe<Analytics.PopupOpenedSignal>(OnPopupOpened); // Use Analytics.PopupOpenedSignal
                signalBus.Unsubscribe<RewardVideoShownSignal>(OnRewardVideoShown);
                
                // Shop and purchase events
                signalBus.Unsubscribe<ShopPurchaseAttemptSignal>(OnShopPurchaseAttempt);
                signalBus.Unsubscribe<ShopPurchaseCompletedSignal>(OnShopPurchaseCompleted);
                signalBus.Unsubscribe<ShopPurchaseFailedSignal>(OnShopPurchaseFailed);
                signalBus.Unsubscribe<BuyCurrencyCompletedSignal>(OnBuyCurrencyCompleted);
                signalBus.Unsubscribe<WatchAdForLivesCompletedSignal>(OnWatchAdForLivesCompleted);
                
                // Booster events
                signalBus.Unsubscribe<BoosterButtonClickedSignal>(OnBoosterButtonClicked);
                signalBus.Unsubscribe<BoosterUsedSignal>(OnBoosterUsed);
                signalBus.Unsubscribe<BoosterCancelledSignal>(OnBoosterCancelled);
            }
            
            // Unsubscribe from purchase events
            Purchase.PurchaseEventManager.OnPurchaseStarted -= OnPurchaseStarted;
            Purchase.PurchaseEventManager.OnPurchaseCompleted -= OnPurchaseCompleted;
            Purchase.PurchaseEventManager.OnPurchaseFailed -= OnPurchaseFailed;
        }

        private void OnGameWon(WinGameSignal signal)
        {
            // Check if player completed level in less than 10 seconds
            bool closeWin = false;
            if (timerManager != null)
            {
                var levelTimer = timerManager.GetTimer(TimerIDs.LEVEL_TIMER);
                if (levelTimer != null)
                {
                    closeWin = levelTimer.RemainingTime < 10f;
                }
            }

            var parameters = new Dictionary<string, object>
            {
                { GameAnalyticsEvents.PARAM_LEVEL_INDEX, signal.LevelIndex },
                { GameAnalyticsEvents.PARAM_CLOSE_WIN, closeWin }
            };

            AnalyticsManager.Instance.TrackEvent(GameAnalyticsEvents.LEVEL_COMPLETED, parameters);
        }

        private void OnGameLost(LoseGameSignal signal)
        {
            var parameters = new Dictionary<string, object>
            {
                { GameAnalyticsEvents.PARAM_LEVEL_INDEX, signal.LevelIndex }
            };

            AnalyticsManager.Instance.TrackEvent(GameAnalyticsEvents.LEVEL_FAILED, parameters);
        }
        
        private void OnGameInitializationComplete(GameInitializationCompleteSignal signal)
        {
            // Update retention data on game initialization (this will be included as default parameter in all events)
            UpdatePlayerRetention();
            
            // Track game initialization complete event with build number and platform
            var parameters = new Dictionary<string, object>
            {
                { GameAnalyticsEvents.PARAM_BUILD_NUMBER, Application.buildGUID },
                { GameAnalyticsEvents.PARAM_PLATFORM, Application.platform.ToString() }
            };

            AnalyticsManager.Instance.TrackEvent(GameAnalyticsEvents.GAME_INITIALIZATION_COMPLETE, parameters);
        }

        private void OnLevelStarted(LevelLoadedSignal signal)
        {
            var parameters = new Dictionary<string, object>
            {
                { GameAnalyticsEvents.PARAM_LEVEL_INDEX, signal.LevelIndex }
            };

            AnalyticsManager.Instance.TrackEvent(GameAnalyticsEvents.LEVEL_STARTED, parameters);
        }

        private void OnLevelRestarted(RetryLevelSignal signal)
        {
            // For retry signal, we need to get the current level index from PlayerDataManager
            // since RetryLevelSignal doesn't contain level info
            var playerDataManager = ServiceLocator.TryResolve<PlayerDataManager>();
            int levelIndex = playerDataManager?.GetCurrentLevelIndex() ?? -1;
            
            var parameters = new Dictionary<string, object>
            {
                { GameAnalyticsEvents.PARAM_LEVEL_INDEX, levelIndex }
            };

            AnalyticsManager.Instance.TrackEvent(GameAnalyticsEvents.LEVEL_RESTARTED, parameters);
        }

        private void OnReplayButtonClicked(RetryLevelSignal signal)
        {
            // Get current level index from PlayerDataManager
            var playerDataManager = ServiceLocator.TryResolve<PlayerDataManager>();
            int levelIndex = playerDataManager?.GetCurrentLevelIndex() ?? -1;
            
            var parameters = new Dictionary<string, object>
            {
                { GameAnalyticsEvents.PARAM_LEVEL_INDEX, levelIndex }
            };

            AnalyticsManager.Instance.TrackEvent(GameAnalyticsEvents.REPLAY_BUTTON_CLICKED, parameters);
        }

        private void OnPopupOpened(Analytics.PopupOpenedSignal signal)
        {
            // Changed: Use event name format "popup_open_PopupName" instead of parameter
            string eventName = $"{GameAnalyticsEvents.POPUP_OPEN}_{signal.PopupName}";
            AnalyticsManager.Instance.TrackEvent(eventName, new Dictionary<string, object>());
        }

        private void OnRewardVideoShown(RewardVideoShownSignal signal)
        {
            var parameters = new Dictionary<string, object>
            {
                { GameAnalyticsEvents.PARAM_ADS_NAME, signal.Placement }
            };

            AnalyticsManager.Instance.TrackEvent(GameAnalyticsEvents.REWARD_VIDEO_SHOWN, parameters);
        }

        private void UpdatePlayerRetention()
        {
            var playerDataManager = ServiceLocator.TryResolve<PlayerDataManager>();
            if (playerDataManager == null || playerDataManager.PlayerData == null)
            {
                Debug.LogWarning("[GameAnalyticsTracker] PlayerDataManager or PlayerData not available for retention update");
                return;
            }

            // Update retention data - this will now be included as default parameter in all subsequent events
            int retentionDay = playerDataManager.PlayerData.UpdateRetentionOnLogin();
            
            if (retentionDay >= 0)
            {
                Debug.Log($"[GameAnalyticsTracker] Retention updated to Day {retentionDay} - will be included as default parameter in all events");
            }
            else
            {
                Debug.Log($"[GameAnalyticsTracker] Same day login - retention remains at Day {playerDataManager.PlayerData.GetRetentionDay()}");
            }
        }
        
        // Purchase event handlers - listen to PurchaseEventManager directly
        private void OnPurchaseStarted(Purchase.PurchaseEventArgs args)
        {
            if (args.ShopPackage == null) return;

            var parameters = new Dictionary<string, object>
            {
                { GameAnalyticsEvents.PARAM_PRODUCT_ID, args.ProductId ?? "unknown" },
                { GameAnalyticsEvents.PARAM_PACKAGE_NAME, args.ShopPackage.packageId ?? "unknown" },
                { GameAnalyticsEvents.PARAM_PURCHASE_TYPE, args.ShopPackage.purchaseType.ToString() },
                { GameAnalyticsEvents.PARAM_POPUP_TYPE, "shop" },
                { GameAnalyticsEvents.PARAM_ACTION, "attempt" }
            };
            
            AnalyticsManager.Instance.TrackEvent(GameAnalyticsEvents.SHOP_PURCHASE_ATTEMPT, parameters);
            
            // Also fire signal for other listeners
            if (signalBus != null)
            {
                var signal = new ShopPurchaseAttemptSignal(
                    args.ProductId ?? "unknown",
                    args.ShopPackage.packageId ?? "unknown",
                    args.ShopPackage.purchaseType.ToString()
                );
                signalBus.Fire(signal);
            }
        }
        
        private void OnPurchaseCompleted(Purchase.PurchaseEventArgs args)
        {
            if (args.ShopPackage == null) return;

            var parameters = new Dictionary<string, object>
            {
                { GameAnalyticsEvents.PARAM_PRODUCT_ID, args.ProductId ?? "unknown" },
                { GameAnalyticsEvents.PARAM_PACKAGE_NAME, args.ShopPackage.packageId ?? "unknown" },
                { GameAnalyticsEvents.PARAM_PURCHASE_TYPE, args.ShopPackage.purchaseType.ToString() },
                { GameAnalyticsEvents.PARAM_PRICE, args.Price },
                { "currency_code", args.CurrencyCode ?? "unknown" },
                { GameAnalyticsEvents.PARAM_POPUP_TYPE, "shop" },
                { GameAnalyticsEvents.PARAM_ACTION, "purchase_completed" }
            };
            
            // Add coin cost if it's a coin purchase
            if (args.ShopPackage.coinCost > 0)
            {
                parameters[GameAnalyticsEvents.PARAM_COIN_COST] = args.ShopPackage.coinCost;
            }
            
            AnalyticsManager.Instance.TrackEvent(GameAnalyticsEvents.SHOP_PURCHASE, parameters);
            
            // Also fire signal for other listeners
            if (signalBus != null)
            {
                var signal = new ShopPurchaseCompletedSignal(
                    args.ProductId ?? "unknown",
                    args.ShopPackage.packageId ?? "unknown",
                    args.ShopPackage.purchaseType.ToString(),
                    args.Price,
                    args.CurrencyCode ?? "unknown",
                    args.ShopPackage.coinCost
                );
                signalBus.Fire(signal);
            }
        }
        
        private void OnPurchaseFailed(Purchase.PurchaseEventArgs args)
        {
            if (args.ShopPackage == null) return;

            var parameters = new Dictionary<string, object>
            {
                { GameAnalyticsEvents.PARAM_PRODUCT_ID, args.ProductId ?? "unknown" },
                { GameAnalyticsEvents.PARAM_PACKAGE_NAME, args.ShopPackage.packageId ?? "unknown" },
                { GameAnalyticsEvents.PARAM_PURCHASE_TYPE, args.ShopPackage.purchaseType.ToString() },
                { GameAnalyticsEvents.PARAM_FAIL_STATUS, args.FailureReason?.ToString() ?? "unknown" },
                { GameAnalyticsEvents.PARAM_POPUP_TYPE, "shop" },
                { GameAnalyticsEvents.PARAM_ACTION, "failed" }
            };
            
            AnalyticsManager.Instance.TrackEvent(GameAnalyticsEvents.SHOP_PURCHASE_FAILED, parameters);
            
            // Also fire signal for other listeners
            if (signalBus != null)
            {
                var signal = new ShopPurchaseFailedSignal(
                    args.ProductId ?? "unknown",
                    args.ShopPackage.packageId ?? "unknown",
                    args.ShopPackage.purchaseType.ToString(),
                    args.FailureReason?.ToString() ?? "unknown"
                );
                signalBus.Fire(signal);
            }
        }
        
        private void OnShopPurchaseAttempt(ShopPurchaseAttemptSignal signal)
        {
            // This is now handled by OnPurchaseStarted - kept for backwards compatibility
        }
        
        private void OnShopPurchaseCompleted(ShopPurchaseCompletedSignal signal)
        {
            // This is now handled by OnPurchaseCompleted - kept for backwards compatibility
        }
        
        private void OnShopPurchaseFailed(ShopPurchaseFailedSignal signal)
        {
            // This is now handled by OnPurchaseFailed - kept for backwards compatibility
        }
        
        private void OnBuyCurrencyCompleted(BuyCurrencyCompletedSignal signal)
        {
            var parameters = new Dictionary<string, object>
            {
                { GameAnalyticsEvents.PARAM_CURRENCY_TYPE, signal.CurrencyType },
                { GameAnalyticsEvents.PARAM_CURRENCY_AMOUNT, signal.Amount },
                { GameAnalyticsEvents.PARAM_POPUP_TYPE, "buy_currency" },
                { GameAnalyticsEvents.PARAM_ACTION, "purchase_completed" }
            };
            
            AnalyticsManager.Instance.TrackEvent(GameAnalyticsEvents.PLAYER_BUY_CURRENCY, parameters);
        }
        
        private void OnWatchAdForLivesCompleted(WatchAdForLivesCompletedSignal signal)
        {
            var parameters = new Dictionary<string, object>
            {
                { GameAnalyticsEvents.PARAM_AD_PLACEMENT, signal.AdPlacement },
                { GameAnalyticsEvents.PARAM_POPUP_TYPE, "watch_ad_for_lives" },
                { GameAnalyticsEvents.PARAM_ACTION, "watch_completed" }
            };
            
            AnalyticsManager.Instance.TrackEvent(GameAnalyticsEvents.PLAYER_WATCH_AD_FOR_LIVES, parameters);
        }
        
        private void OnBoosterButtonClicked(BoosterButtonClickedSignal signal)
        {
            var parameters = new Dictionary<string, object>
            {
                { "booster_type", signal.BoosterType },
                { "has_booster", signal.HasBooster },
                { GameAnalyticsEvents.PARAM_ACTION, "click" }
            };
            
            AnalyticsManager.Instance.TrackEvent("booster_button_clicked", parameters);
        }
        
        private void OnBoosterUsed(BoosterUsedSignal signal)
        {
            var parameters = new Dictionary<string, object>
            {
                { "booster_type", signal.BoosterType },
                { GameAnalyticsEvents.PARAM_ACTION, "use" }
            };
            
            AnalyticsManager.Instance.TrackEvent("booster_used", parameters);
        }
        
        private void OnBoosterCancelled(BoosterCancelledSignal signal)
        {
            var parameters = new Dictionary<string, object>
            {
                { "booster_type", signal.BoosterType },
                { GameAnalyticsEvents.PARAM_ACTION, "cancel" }
            };
            
            AnalyticsManager.Instance.TrackEvent("booster_cancelled", parameters);
        }
    }
}
