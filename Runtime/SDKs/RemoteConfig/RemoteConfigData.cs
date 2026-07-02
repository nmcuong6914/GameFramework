using System;
using UnityEngine;

/// <summary>
/// Data model for Firebase Remote Config
/// Simple configuration focused on version management
/// </summary>
[Serializable]
public class RemoteConfigData
{
    [Header("App Version")]
    [Tooltip("Minimum required version code. Players with lower versions will be prompted to update")]
    public string versionCode = "1.0.0";
    
    [Tooltip("Message shown to users when they need to update")]
    public string forceUpdateMessage = "Please update to the latest version to continue playing!";
    
    /// <summary>
    /// Check if this config data is valid
    /// </summary>
    public bool IsValid()
    {
        try
        {
            // Basic validation
            return !string.IsNullOrEmpty(versionCode) &&
                   !string.IsNullOrEmpty(forceUpdateMessage);
        }
        catch (Exception ex)
        {
            Debug.LogError($"RemoteConfigData validation error: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Get a formatted version string
    /// </summary>
    public string GetFormattedVersion()
    {
        return $"v{versionCode}";
    }
    
    /// <summary>
    /// Create a copy of this config data
    /// </summary>
    public RemoteConfigData Clone()
    {
        return JsonUtility.FromJson<RemoteConfigData>(JsonUtility.ToJson(this));
    }
    
    /// <summary>
    /// Convert to JSON string
    /// </summary>
    public string ToJson(bool prettyPrint = false)
    {
        return JsonUtility.ToJson(this, prettyPrint);
    }
    
    /// <summary>
    /// Create from JSON string
    /// </summary>
    public static RemoteConfigData FromJson(string json)
    {
        try
        {
            return JsonUtility.FromJson<RemoteConfigData>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to parse RemoteConfigData from JSON: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Log current configuration for debugging
    /// </summary>
    public void LogConfiguration()
    {
        Debug.Log($"[RemoteConfigData] Configuration:\n" +
                  $"Version: {versionCode}\n" +
                  $"Update Message: {forceUpdateMessage}");
    }
}