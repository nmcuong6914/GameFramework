using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

/// <summary>
/// Central manager for all game notifications
/// Coordinates with NotificationService and game systems (Lives, Events, etc.)
/// </summary>
public class GameNotificationManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private NotificationConfig notificationConfig;
    
    [Header("Notification IDs")]
    [SerializeField] private string livesFullNotificationId = "lives_full";
    [SerializeField] private string comebackNotificationId = "comeback_reminder";
    
    // Services
    private NotificationService notificationService;
    private LivesManager livesManager;
    private PlayerDataManager playerDataManager;
    
    private bool isInitialized = false;
    
    // Comeback notification settings
    private const float COMEBACK_NOTIFICATION_DELAY_HOURS = 24f; // 1 day
    private const string LAST_APP_PAUSE_TIME_KEY = "LastAppPauseTime";
    
    public bool IsInitialized => isInitialized;
    
    /// <summary>
    /// Initialize the notification manager
    /// </summary>
    public async UniTask InitializeAsync()
    {
        if (notificationConfig == null)
        {
            FDebug.LogWarning("GameNotificationManager: NotificationConfig is not assigned");
            return;
        }
        
        if (!notificationConfig.IsEnabled())
        {
            if (notificationConfig.debugLogs)
                FDebug.Log("GameNotificationManager: Notifications are disabled in config");
            return;
        }
        
        // Get services
        notificationService = ServiceLocator.TryResolve<NotificationService>();
        livesManager = ServiceLocator.TryResolve<LivesManager>();
        playerDataManager = ServiceLocator.TryResolve<PlayerDataManager>();
        
        if (notificationService == null)
        {
            FDebug.LogWarning("GameNotificationManager: NotificationService not found");
            return;
        }
        
        if (livesManager == null)
        {
            FDebug.LogWarning("GameNotificationManager: LivesManager not found");
        }
        
        if (playerDataManager == null)
        {
            FDebug.LogWarning("GameNotificationManager: PlayerDataManager not found");
        }
        
        // Wait for NotificationService to initialize
        while (!notificationService.IsInitialized)
        {
            await UniTask.NextFrame();
        }
        
        // Load notification data into NotificationService
        LoadNotificationsIntoService();
        
        isInitialized = true;
        
        if (notificationConfig.debugLogs)
            FDebug.Log("GameNotificationManager: Initialized successfully");
    }
    
    /// <summary>
    /// Load notification configurations into the NotificationService
    /// </summary>
    private void LoadNotificationsIntoService()
    {
        if (notificationService == null || notificationConfig == null)
            return;
        
        foreach (var notification in notificationConfig.notifications)
        {
            // Notifications are already defined in config, just log for verification
            if (notificationConfig.debugLogs)
                FDebug.Log($"GameNotificationManager: Loaded notification '{notification.id}': {notification.title}");
        }
    }
    
    #region Lives Notifications
    
    /// <summary>
    /// Schedule notification for when lives will be full
    /// </summary>
    public void ScheduleLivesFullNotification()
    {
        if (!isInitialized || notificationService == null || livesManager == null || playerDataManager == null)
        {
            if (notificationConfig?.debugLogs == true)
                FDebug.LogWarning("GameNotificationManager: Cannot schedule lives notification - not initialized");
            return;
        }
        
        int currentLives = livesManager.GetCurrentLives();
        int maxLives = livesManager.GetMaxLives();
        
        // Only schedule if lives are not full
        if (currentLives >= maxLives)
        {
            if (notificationConfig.debugLogs)
                FDebug.Log("GameNotificationManager: Lives are full, no notification needed");
            return;
        }
        
        // Calculate time until lives are full
        float timeToNextLife = livesManager.GetRegenerationTimeRemaining();
        var livesConfig = playerDataManager.GetCurrencyConfig(CurrencyType.Lives);
        
        if (livesConfig == null || livesConfig.regenerationInterval <= 0)
        {
            if (notificationConfig.debugLogs)
                FDebug.LogWarning("GameNotificationManager: Invalid lives config");
            return;
        }
        
        // Check if lives regeneration is actually enabled
        if (livesConfig.regenerationType != RegenerationType.OverTime)
        {
            if (notificationConfig.debugLogs)
                FDebug.Log("GameNotificationManager: Lives regeneration is disabled, no notification needed");
            return;
        }
        
        int livesNeeded = maxLives - currentLives;
        float totalTimeSeconds = timeToNextLife + ((livesNeeded - 1) * livesConfig.regenerationInterval);
        
        // Ensure minimum delay of one regeneration interval
        totalTimeSeconds = Mathf.Max(totalTimeSeconds, livesConfig.regenerationInterval);
        
        if (totalTimeSeconds <= 0)
        {
            if (notificationConfig.debugLogs)
                FDebug.Log("GameNotificationManager: Time until full is invalid, skipping notification");
            return;
        }
        
        // Get notification data from config
        var notificationData = notificationConfig.GetNotificationById(livesFullNotificationId);
        if (notificationData == null)
        {
            FDebug.LogWarning($"GameNotificationManager: Notification '{livesFullNotificationId}' not found in config");
            return;
        }
        
        // Schedule the notification
        notificationService.ScheduleNotification(livesFullNotificationId, notificationData, totalTimeSeconds);
        
        if (notificationConfig.debugLogs)
        {
            FDebug.Log($"GameNotificationManager: Scheduled lives notification in {totalTimeSeconds}s ({totalTimeSeconds / 60f:F1} min)");
            FDebug.Log($"GameNotificationManager: Current lives: {currentLives}/{maxLives}, Lives needed: {livesNeeded}");
        }
    }
    
    /// <summary>
    /// Cancel lives notification
    /// </summary>
    public void CancelLivesFullNotification()
    {
        if (!isInitialized || notificationService == null)
            return;
        
        notificationService.CancelNotification(livesFullNotificationId);
        
        if (notificationConfig?.debugLogs == true)
            FDebug.Log("GameNotificationManager: Canceled lives notification");
    }
    
    #endregion
    
    #region Comeback Notifications
    
    /// <summary>
    /// Schedule comeback notification for 1 day after user goes offline
    /// </summary>
    public void ScheduleComebackNotification()
    {
        if (!isInitialized || notificationService == null)
        {
            if (notificationConfig?.debugLogs == true)
                FDebug.LogWarning("GameNotificationManager: Cannot schedule comeback notification - not initialized");
            return;
        }
        
        // Get notification data from config
        var notificationData = notificationConfig.GetNotificationById(comebackNotificationId);
        if (notificationData == null)
        {
            FDebug.LogWarning($"GameNotificationManager: Comeback notification '{comebackNotificationId}' not found in config");
            return;
        }
        
        // Calculate delay in seconds (24 hours)
        float delayInSeconds = COMEBACK_NOTIFICATION_DELAY_HOURS * 3600f; // 24 hours * 3600 seconds/hour
        
        // Schedule the notification
        notificationService.ScheduleNotification(comebackNotificationId, notificationData, delayInSeconds);
        
        // Store the time when we scheduled the notification
        PlayerPrefs.SetString(LAST_APP_PAUSE_TIME_KEY, DateTime.Now.ToBinary().ToString());
        
        if (notificationConfig.debugLogs)
        {
            FDebug.Log($"GameNotificationManager: Scheduled comeback notification in {delayInSeconds}s ({COMEBACK_NOTIFICATION_DELAY_HOURS} hours)");
            FDebug.Log($"GameNotificationManager: Notification will remind user to return to the game");
        }
    }
    
    /// <summary>
    /// Cancel comeback notification
    /// </summary>
    public void CancelComebackNotification()
    {
        if (!isInitialized || notificationService == null)
            return;
        
        notificationService.CancelNotification(comebackNotificationId);
        
        if (notificationConfig?.debugLogs == true)
            FDebug.Log("GameNotificationManager: Canceled comeback notification");
    }
    
    /// <summary>
    /// Check if user has been offline for more than the comeback notification delay
    /// </summary>
    /// <returns>True if user has been offline long enough</returns>
    private bool HasBeenOfflineLongEnough()
    {
        if (!PlayerPrefs.HasKey(LAST_APP_PAUSE_TIME_KEY))
            return false;
        
        try
        {
            string pauseTimeString = PlayerPrefs.GetString(LAST_APP_PAUSE_TIME_KEY);
            long pauseTimeBinary = Convert.ToInt64(pauseTimeString);
            DateTime pauseTime = DateTime.FromBinary(pauseTimeBinary);
            
            TimeSpan offlineTime = DateTime.Now - pauseTime;
            bool longEnough = offlineTime.TotalHours >= COMEBACK_NOTIFICATION_DELAY_HOURS;
            
            if (notificationConfig?.debugLogs == true)
            {
                FDebug.Log($"GameNotificationManager: User was offline for {offlineTime.TotalHours:F1} hours (need {COMEBACK_NOTIFICATION_DELAY_HOURS}h for comeback notification)");
            }
            
            return longEnough;
        }
        catch (System.Exception ex)
        {
            FDebug.LogWarning($"GameNotificationManager: Error checking offline time: {ex.Message}");
            return false;
        }
    }
    
    #endregion
    
    #region Application Lifecycle
    
    /// <summary>
    /// Called when app is paused (goes to background)
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        if (!isInitialized)
            return;
        
        if (pauseStatus)
        {
            // App going to background - schedule notifications
            if (notificationConfig?.debugLogs == true)
                FDebug.Log("GameNotificationManager: App paused, scheduling notifications");
            
            ScheduleAllNotifications();
        }
        else
        {
            // App coming to foreground - cancel notifications and check offline time
            if (notificationConfig?.debugLogs == true)
                FDebug.Log("GameNotificationManager: App resumed, canceling notifications and checking offline time");
            
            CancelAllNotifications();
            
            // Check if user was offline long enough and handle accordingly
            if (HasBeenOfflineLongEnough())
            {
                OnUserReturnedAfterLongAbsence();
            }
        }
    }
    
    /// <summary>
    /// Called when app gains/loses focus
    /// </summary>
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!isInitialized)
            return;
        
        if (!hasFocus)
        {
            // App lost focus - schedule notifications
            if (notificationConfig?.debugLogs == true)
                FDebug.Log("GameNotificationManager: App lost focus, scheduling notifications");
            
            ScheduleAllNotifications();
        }
        else
        {
            // App gained focus - cancel notifications
            if (notificationConfig?.debugLogs == true)
                FDebug.Log("GameNotificationManager: App gained focus, canceling notifications");
            
            CancelAllNotifications();
        }
    }
    
    /// <summary>
    /// Called when app is about to quit
    /// </summary>
    private void OnApplicationQuit()
    {
        if (!isInitialized)
            return;
        
        if (notificationConfig?.debugLogs == true)
            FDebug.Log("GameNotificationManager: App quitting, scheduling notifications");
        
        ScheduleAllNotifications();
    }
    
    /// <summary>
    /// Schedule all relevant notifications
    /// </summary>
    private void ScheduleAllNotifications()
    {
        // Schedule lives notification
        ScheduleLivesFullNotification();
        
        // Schedule comeback notification
        ScheduleComebackNotification();
        
        // TODO: Add other notification types here (events, tournaments, etc.)
    }
    
    /// <summary>
    /// Cancel all scheduled notifications
    /// </summary>
    private void CancelAllNotifications()
    {
        if (notificationService != null)
        {
            notificationService.CancelAllNotifications();
        }
    }
    
    /// <summary>
    /// Called when user returns to the game after being offline for a long time
    /// This can be used to trigger special welcome back events or rewards
    /// </summary>
    private void OnUserReturnedAfterLongAbsence()
    {
        if (notificationConfig?.debugLogs == true)
        {
            FDebug.Log("GameNotificationManager: User returned after long absence - welcome back!");
        }
        
        // Clear the stored pause time since user is back
        PlayerPrefs.DeleteKey(LAST_APP_PAUSE_TIME_KEY);
        
        // TODO: Add welcome back logic here:
        // - Show welcome back popup
        // - Grant bonus lives or coins
        // - Display special offers
        // - Fire analytics events
        
        // Example analytics event (uncomment if AnalyticsManager is available)
        // var analyticsManager = ServiceLocator.TryResolve<AnalyticsManager>();
        // if (analyticsManager != null)
        // {
        //     var parameters = new Dictionary<string, object>
        //     {
        //         { "offline_hours", GetOfflineHours() },
        //         { "welcome_back", true }
        //     };
        //     analyticsManager.TrackEvent("player_returned_after_long_absence", parameters);
        // }
    }
    
    #endregion
}
