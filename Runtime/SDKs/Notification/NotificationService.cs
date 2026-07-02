using UnityEngine;
using System;
using System.Collections.Generic;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

/// <summary>
/// Data class for notification configuration
/// </summary>
[System.Serializable]
public class NotificationData
{
    public string id;
    public string title;
    public string body;
    public string smallIcon = "app_icon"; // Uses Unity's default app icon
    public string largeIcon = "app_icon"; // Uses Unity's default app icon
    
    // iOS specific
    public string subtitle = "";
    public string categoryIdentifier = "";
    public string threadIdentifier = "";
    
    public NotificationData(string id, string title, string body)
    {
        this.id = id;
        this.title = title;
        this.body = body;
    }
}

/// <summary>
/// Service for managing mobile push notifications
/// Generic service that can be used by any system (Lives, Events, etc.)
/// </summary>
public class NotificationService : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private bool enableNotifications = true;
    [SerializeField] private bool debugLogs = true;
    
    [Header("Android Settings")]
    [SerializeField] private string androidChannelId = "game_notifications";
    [SerializeField] private string androidChannelName = "Game Notifications";
    [SerializeField] private string androidChannelDescription = "Important game notifications";
    
    [Header("Predefined Notifications")]
    [SerializeField] private List<NotificationData> predefinedNotifications = new List<NotificationData>();
    
    // Notification tracking - maps notification ID to platform-specific ID
    private Dictionary<string, int> scheduledAndroidNotifications = new Dictionary<string, int>();
#if UNITY_IOS
    private Dictionary<string, string> scheduledIOSNotifications = new Dictionary<string, string>();
#endif
    
    private bool isInitialized = false;
    
    public bool IsInitialized => isInitialized;
    
    /// <summary>
    /// Initialize the notification service
    /// </summary>
    public void Initialize()
    {
        if (!enableNotifications)
        {
            if (debugLogs)
                FDebug.Log("NotificationService: Notifications are disabled in settings");
            return;
        }
        
#if UNITY_IOS || UNITY_ANDROID
        // Initialize platform-specific notification system
        InitializePlatformNotifications();
        
        isInitialized = true;
        
        if (debugLogs)
            FDebug.Log("NotificationService: Initialized successfully");
#else
        if (debugLogs)
            FDebug.Log("NotificationService: Not supported on this platform");
#endif
    }
    
    /// <summary>
    /// Initialize platform-specific notification channels
    /// </summary>
    private void InitializePlatformNotifications()
    {
#if UNITY_ANDROID
        // Create notification channel for Android
        var channel = new AndroidNotificationChannel()
        {
            Id = androidChannelId,
            Name = androidChannelName,
            Importance = Importance.Default,
            Description = androidChannelDescription,
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
        
        if (debugLogs)
            FDebug.Log($"NotificationService: Android channel '{androidChannelId}' registered");
#elif UNITY_IOS
        // Request authorization for iOS
        var authorizationOption = AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound;
        using (var req = new AuthorizationRequest(authorizationOption, true))
        {
            while (!req.IsFinished)
            {
                // Wait for request to finish
            }
            
            if (debugLogs)
            {
                FDebug.Log($"NotificationService: iOS authorization granted: {req.Granted}");
            }
        }
#endif
    }
    
    /// <summary>
    /// Get predefined notification by ID
    /// </summary>
    public NotificationData GetNotificationById(string notificationId)
    {
        return predefinedNotifications.Find(n => n.id == notificationId);
    }
    
    /// <summary>
    /// Schedule a notification to be shown after a delay
    /// </summary>
    /// <param name="notificationId">ID of the notification (for tracking and cancellation)</param>
    /// <param name="notificationData">Notification configuration</param>
    /// <param name="delayInSeconds">Delay in seconds before showing</param>
    public void ScheduleNotification(string notificationId, NotificationData notificationData, float delayInSeconds)
    {
        if (!enableNotifications || !isInitialized)
        {
            if (debugLogs)
                FDebug.Log("NotificationService: Cannot schedule - service not initialized or disabled");
            return;
        }
        
#if UNITY_IOS || UNITY_ANDROID
        if (notificationData == null)
        {
            FDebug.LogWarning($"NotificationService: Cannot schedule notification '{notificationId}' - data is null");
            return;
        }
        
        if (delayInSeconds <= 0)
        {
            if (debugLogs)
                FDebug.Log($"NotificationService: Invalid delay {delayInSeconds}s for notification '{notificationId}'");
            return;
        }
        
        // Cancel existing notification with same ID first
        CancelNotification(notificationId);
        
        // Schedule platform-specific notification
        SchedulePlatformNotification(notificationId, notificationData, delayInSeconds);
        
        if (debugLogs)
        {
            FDebug.Log($"NotificationService: Scheduled '{notificationId}' in {delayInSeconds}s ({delayInSeconds / 60f:F1} min)");
            FDebug.Log($"NotificationService: Title: '{notificationData.title}', Body: '{notificationData.body}'");
        }
#endif
    }
    
    /// <summary>
    /// Schedule notification using predefined notification ID
    /// </summary>
    public void ScheduleNotification(string notificationId, float delayInSeconds)
    {
        var notificationData = GetNotificationById(notificationId);
        if (notificationData == null)
        {
            FDebug.LogWarning($"NotificationService: Predefined notification '{notificationId}' not found");
            return;
        }
        
        ScheduleNotification(notificationId, notificationData, delayInSeconds);
    }
    
    /// <summary>
    /// Schedule a notification with platform-specific implementation
    /// </summary>
    private void SchedulePlatformNotification(string notificationId, NotificationData data, float delayInSeconds)
    {
#if UNITY_ANDROID
        var notification = new AndroidNotification
        {
            Title = data.title,
            Text = data.body,
            FireTime = DateTime.Now.AddSeconds(delayInSeconds),
            SmallIcon = data.smallIcon,
            LargeIcon = data.largeIcon,
        };
        
        int platformId = AndroidNotificationCenter.SendNotification(notification, androidChannelId);
        scheduledAndroidNotifications[notificationId] = platformId;
        
        if (debugLogs)
            FDebug.Log($"NotificationService: Android notification '{notificationId}' scheduled with platform ID: {platformId}");
            
#elif UNITY_IOS
        var timeTrigger = new iOSNotificationTimeIntervalTrigger()
        {
            TimeInterval = new TimeSpan(0, 0, (int)delayInSeconds),
            Repeats = false
        };
        
        var notification = new iOSNotification()
        {
            Title = data.title,
            Body = data.body,
            Subtitle = data.subtitle,
            ShowInForeground = false,
            ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
            CategoryIdentifier = data.categoryIdentifier,
            ThreadIdentifier = data.threadIdentifier,
            Trigger = timeTrigger,
        };
        
        string platformId = iOSNotificationCenter.ScheduleNotification(notification);
        scheduledIOSNotifications[notificationId] = platformId;
        
        if (debugLogs)
            FDebug.Log($"NotificationService: iOS notification '{notificationId}' scheduled with platform ID: {platformId}");
#endif
    }
    
    /// <summary>
    /// Cancel a specific notification by ID
    /// </summary>
    public void CancelNotification(string notificationId)
    {
        if (!isInitialized)
            return;
        
#if UNITY_ANDROID
        if (scheduledAndroidNotifications.TryGetValue(notificationId, out int platformId))
        {
            AndroidNotificationCenter.CancelNotification(platformId);
            scheduledAndroidNotifications.Remove(notificationId);
            
            if (debugLogs)
                FDebug.Log($"NotificationService: Canceled Android notification '{notificationId}' (platform ID: {platformId})");
        }
        
#elif UNITY_IOS
        if (scheduledIOSNotifications.TryGetValue(notificationId, out string platformId))
        {
            iOSNotificationCenter.RemoveScheduledNotification(platformId);
            scheduledIOSNotifications.Remove(notificationId);
            
            if (debugLogs)
                FDebug.Log($"NotificationService: Canceled iOS notification '{notificationId}' (platform ID: {platformId})");
        }
#endif
    }
    
    /// <summary>
    /// Cancel all scheduled notifications
    /// </summary>
    public void CancelAllNotifications()
    {
        if (!isInitialized)
            return;
        
#if UNITY_ANDROID
        foreach (var kvp in scheduledAndroidNotifications)
        {
            AndroidNotificationCenter.CancelNotification(kvp.Value);
            
            if (debugLogs)
                FDebug.Log($"NotificationService: Canceled Android notification '{kvp.Key}' (platform ID: {kvp.Value})");
        }
        scheduledAndroidNotifications.Clear();
        
        // Also cancel all as safety measure
        AndroidNotificationCenter.CancelAllScheduledNotifications();
        
#elif UNITY_IOS
        foreach (var kvp in scheduledIOSNotifications)
        {
            iOSNotificationCenter.RemoveScheduledNotification(kvp.Value);
            
            if (debugLogs)
                FDebug.Log($"NotificationService: Canceled iOS notification '{kvp.Key}' (platform ID: {kvp.Value})");
        }
        scheduledIOSNotifications.Clear();
        
        // Also remove all as safety measure
        iOSNotificationCenter.RemoveAllScheduledNotifications();
#endif
        
        if (debugLogs)
            FDebug.Log("NotificationService: All notifications canceled");
    }
    
    /// <summary>
    /// Get all scheduled notification IDs
    /// </summary>
    public List<string> GetScheduledNotificationIds()
    {
#if UNITY_ANDROID
        return new List<string>(scheduledAndroidNotifications.Keys);
#elif UNITY_IOS
        return new List<string>(scheduledIOSNotifications.Keys);
#else
        return new List<string>();
#endif
    }
}
