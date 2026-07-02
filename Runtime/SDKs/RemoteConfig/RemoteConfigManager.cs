using UnityEngine;
using System;

/// <summary>
/// Utility class to easily access Remote Config values throughout the game
/// Simplified version focusing on version management
/// </summary>
public static class RemoteConfigManager
{
    /// <summary>
    /// Get the RemoteConfigService from ServiceLocator
    /// </summary>
    private static RemoteConfigService GetService()
    {
        return ServiceLocator.TryResolve<RemoteConfigService>();
    }
    
    /// <summary>
    /// Check if Remote Config is available and initialized
    /// </summary>
    public static bool IsAvailable()
    {
        var service = GetService();
        return service != null && service.IsInitialized;
    }
    
    /// <summary>
    /// Get the current remote config data
    /// </summary>
    public static RemoteConfigData GetConfigData()
    {
        var service = GetService();
        return service?.ConfigData;
    }
    
    // ====== VERSION MANAGEMENT ======
    
    /// <summary>
    /// Check if the current app version is outdated
    /// </summary>
    public static bool IsVersionOutdated()
    {
        var service = GetService();
        return service?.IsVersionOutdated() ?? false;
    }
    
    /// <summary>
    /// Get the required version code from remote config
    /// </summary>
    public static string GetRequiredVersion(string fallback = "1.0.0")
    {
        var configData = GetConfigData();
        return !string.IsNullOrEmpty(configData?.versionCode) ? configData.versionCode : fallback;
    }
    
    /// <summary>
    /// Get the force update message
    /// </summary>
    public static string GetUpdateMessage(string fallback = "Please update to the latest version!")
    {
        var configData = GetConfigData();
        return !string.IsNullOrEmpty(configData?.forceUpdateMessage) ? configData.forceUpdateMessage : fallback;
    }
    
    // ====== UTILITY METHODS ======
    
    /// <summary>
    /// Log current configuration for debugging
    /// </summary>
    public static void LogConfiguration()
    {
        var configData = GetConfigData();
        if (configData != null)
        {
            configData.LogConfiguration();
        }
        else
        {
            Debug.LogWarning("[RemoteConfigManager] No configuration data available");
        }
    }
    
    /// <summary>
    /// Force refresh config from Firebase (useful for testing)
    /// </summary>
    public static void ForceRefresh()
    {
        var service = GetService();
        if (service != null && service.IsInitialized)
        {
            service.ForceRefreshConfig();
        }
        else
        {
            Debug.LogWarning("[RemoteConfigManager] RemoteConfigService not available for refresh");
        }
    }
}