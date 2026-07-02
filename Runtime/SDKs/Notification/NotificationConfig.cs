using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject configuration for game notifications
/// </summary>
[CreateAssetMenu(fileName = "NotificationConfig", menuName = "BlockSort/Notification/Notification Config", order = 1)]
public class NotificationConfig : ScriptableObject
{
    [Header("General Settings")]
    [SerializeField] public bool enableNotifications = true;
    [SerializeField] public bool debugLogs = true;
    
    [Header("Notifications")]
    [SerializeField] public List<NotificationData> notifications = new List<NotificationData>();
    
    /// <summary>
    /// Get notification data by ID
    /// </summary>
    public NotificationData GetNotificationById(string id)
    {
        return notifications.Find(n => n.id == id);
    }
    
    /// <summary>
    /// Check if notifications are enabled
    /// </summary>
    public bool IsEnabled()
    {
        return enableNotifications;
    }
}
