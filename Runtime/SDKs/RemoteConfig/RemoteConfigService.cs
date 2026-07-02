using UnityEngine;
using Firebase;
using Firebase.RemoteConfig;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// Service for managing Firebase Remote Config
/// Handles loading remote configuration from Firebase, local caching, and providing access to config values
/// </summary>
public class RemoteConfigService : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;
    
    [Header("Configuration")]
    [SerializeField] private float fetchTimeoutSeconds = 30f;
    [SerializeField] private string localConfigFileName = "remote_config_cache.json";
    
    // Firebase dependencies
    private FirebaseController firebaseController;
    
    // Configuration state
    private RemoteConfigData configData;
    private bool isInitialized = false;
    private CancellationTokenSource initializationCancellation;
    
    // Local storage path
    private string LocalConfigPath => 
        System.IO.Path.Combine(Application.persistentDataPath, localConfigFileName);
    
    // Properties
    public bool IsInitialized => isInitialized;
    public RemoteConfigData ConfigData => configData;
    
    // Events
    public event System.Action<RemoteConfigData> OnConfigLoaded;
    public event System.Action<string> OnConfigLoadFailed;
    
    /// <summary>
    /// Initialize the Remote Config service
    /// </summary>
    public async UniTask InitializeAsync()
    {
        if (isInitialized) return;
        
        initializationCancellation = new CancellationTokenSource();
        
        if (debugLogs)
        {
            Debug.Log("[RemoteConfigService] Starting initialization...");
        }
        
        try
        {
            // Get FirebaseController from ServiceLocator
            firebaseController = ServiceLocator.TryResolve<FirebaseController>();
            
            if (firebaseController == null)
            {
                Debug.LogWarning("[RemoteConfigService] FirebaseController not found in ServiceLocator. Firebase features will be unavailable.");
            }
            
            // Load cached config first (fallback)
            LoadCachedConfig();
            
            // Wait for Firebase initialization
            if (firebaseController != null)
            {
                await firebaseController.WaitForInitializationAsync();
                
                // Try to fetch remote config if Firebase is available
                if (firebaseController.IsAvailable)
                {
                    await FetchRemoteConfig();
                }
                else
                {
                    if (debugLogs)
                    {
                        Debug.LogWarning($"[RemoteConfigService] Firebase not available ({firebaseController.GetStatusMessage()}), using cached config only");
                    }
                }
            }
            else
            {
                if (debugLogs)
                {
                    Debug.LogWarning("[RemoteConfigService] FirebaseController not registered, using cached config only");
                }
            }
            
            // Ensure we have some config data
            if (configData == null)
            {
                configData = CreateDefaultConfig();
                if (debugLogs)
                {
                    Debug.Log("[RemoteConfigService] Using default config");
                }
            }
            
            isInitialized = true;
            
            if (debugLogs)
            {
                Debug.Log($"[RemoteConfigService] Initialization completed. Version: {configData?.versionCode ?? "Unknown"}");
            }
            
            // Notify listeners
            OnConfigLoaded?.Invoke(configData);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RemoteConfigService] Initialization failed: {ex.Message}");
            
            // Use cached or default config as fallback
            if (configData == null)
            {
                configData = CreateDefaultConfig();
            }
            
            isInitialized = true; // Mark as initialized even on error to prevent blocking game start
            OnConfigLoadFailed?.Invoke(ex.Message);
        }
        finally
        {
            initializationCancellation?.Dispose();
            initializationCancellation = null;
        }
    }
    
    /// <summary>
    /// Fetch remote configuration from Firebase
    /// </summary>
    private async UniTask FetchRemoteConfig()
    {
        if (firebaseController == null || !firebaseController.IsAvailable)
        {
            if (debugLogs)
            {
                Debug.LogWarning("[RemoteConfigService] Firebase not available, cannot fetch remote config");
            }
            return;
        }
        
        try
        {
            if (debugLogs)
            {
                Debug.Log("[RemoteConfigService] Fetching remote config...");
            }
            
            // Set default values
            var defaults = new Dictionary<string, object>
            {
                { "config_json", CreateDefaultConfigJson() }
            };
            
            await FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults);
            
            // Fetch from remote with timeout
            var fetchTask = FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.FromSeconds(fetchTimeoutSeconds));
            
            // Convert Task to UniTask with cancellation
            await fetchTask.AsUniTask().AttachExternalCancellation(initializationCancellation?.Token ?? CancellationToken.None);
            
            // Check fetch status
            var info = FirebaseRemoteConfig.DefaultInstance.Info;
            if (info.LastFetchStatus == LastFetchStatus.Success)
            {
                // Activate fetched config
                await FirebaseRemoteConfig.DefaultInstance.ActivateAsync();
                
                // Parse the fetched config
                string configJson = FirebaseRemoteConfig.DefaultInstance.GetValue("config_json").StringValue;
                ParseAndSaveConfig(configJson);
                
                if (debugLogs)
                {
                    Debug.Log($"[RemoteConfigService] Remote config fetched successfully. Version: {configData?.versionCode}");
                }
            }
            else
            {
                if (debugLogs)
                {
                    Debug.LogWarning($"[RemoteConfigService] Remote config fetch failed: {info.LastFetchStatus}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            if (debugLogs)
            {
                Debug.Log("[RemoteConfigService] Remote config fetch cancelled");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RemoteConfigService] Error fetching remote config: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Parse config JSON and save to cache
    /// </summary>
    private void ParseAndSaveConfig(string configJson)
    {
        try
        {
            var newConfigData = JsonUtility.FromJson<RemoteConfigData>(configJson);
            
            if (newConfigData != null)
            {
                configData = newConfigData;
                SaveConfigToCache(configJson);
                
                if (debugLogs)
                {
                    Debug.Log($"[RemoteConfigService] Config parsed and cached. Version: {configData.versionCode}");
                }
            }
            else
            {
                Debug.LogError("[RemoteConfigService] Failed to parse config JSON");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RemoteConfigService] Error parsing config JSON: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Load cached configuration from local storage
    /// </summary>
    private void LoadCachedConfig()
    {
        try
        {
            if (System.IO.File.Exists(LocalConfigPath))
            {
                string cachedJson = System.IO.File.ReadAllText(LocalConfigPath);
                configData = JsonUtility.FromJson<RemoteConfigData>(cachedJson);
                
                if (debugLogs)
                {
                    Debug.Log($"[RemoteConfigService] Loaded cached config. Version: {configData?.versionCode}");
                }
            }
            else
            {
                if (debugLogs)
                {
                    Debug.Log("[RemoteConfigService] No cached config found");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RemoteConfigService] Error loading cached config: {ex.Message}");
            configData = null;
        }
    }
    
    /// <summary>
    /// Save configuration to local cache
    /// </summary>
    private void SaveConfigToCache(string configJson)
    {
        try
        {
            System.IO.File.WriteAllText(LocalConfigPath, configJson);
            
            if (debugLogs)
            {
                Debug.Log("[RemoteConfigService] Config saved to cache");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RemoteConfigService] Error saving config to cache: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Create default configuration
    /// </summary>
    private RemoteConfigData CreateDefaultConfig()
    {
        return new RemoteConfigData
        {
            versionCode = Application.version,
            forceUpdateMessage = "Please update to the latest version to continue playing!"
        };
    }
    
    /// <summary>
    /// Create default configuration JSON
    /// </summary>
    private string CreateDefaultConfigJson()
    {
        var defaultConfig = CreateDefaultConfig();
        return JsonUtility.ToJson(defaultConfig, true);
    }
    
    /// <summary>
    /// Get string value from config
    /// </summary>
    public string GetString(string key, string defaultValue = "")
    {
        if (!isInitialized || configData == null)
        {
            return defaultValue;
        }
        
        // Use reflection to get the value dynamically
        var field = typeof(RemoteConfigData).GetField(key);
        if (field != null && field.FieldType == typeof(string))
        {
            return (string)field.GetValue(configData) ?? defaultValue;
        }
        
        return defaultValue;
    }
    
    /// <summary>
    /// Get int value from config
    /// </summary>
    public int GetInt(string key, int defaultValue = 0)
    {
        if (!isInitialized || configData == null)
        {
            return defaultValue;
        }
        
        // Use reflection to get the value dynamically
        var field = typeof(RemoteConfigData).GetField(key);
        if (field != null && field.FieldType == typeof(int))
        {
            return (int)field.GetValue(configData);
        }
        
        return defaultValue;
    }
    
    /// <summary>
    /// Get bool value from config
    /// </summary>
    public bool GetBool(string key, bool defaultValue = false)
    {
        if (!isInitialized || configData == null)
        {
            return defaultValue;
        }
        
        // Use reflection to get the value dynamically
        var field = typeof(RemoteConfigData).GetField(key);
        if (field != null && field.FieldType == typeof(bool))
        {
            return (bool)field.GetValue(configData);
        }
        
        return defaultValue;
    }
    
    /// <summary>
    /// Convert version string to comparable integer 
    /// Supports up to 4 version parts with each part up to 999
    /// Examples: "1.2.1.5" -> 1002001005, "1.2.2" -> 1002002000
    /// </summary>
    private long ConvertVersionToInt(string version)
    {
        try
        {
            if (string.IsNullOrEmpty(version))
                return 0;
            
            // Split version into parts (e.g., "1.2.1.5" -> ["1", "2", "1", "5"])
            string[] parts = version.Split('.');
            
            long versionInt = 0;
            long multiplier = 1000000000; // 10^9 for major version
            
            // Support up to 4 parts: major.minor.patch.build
            // Each part can be up to 999 (3 digits)
            for (int i = 0; i < Math.Min(parts.Length, 4); i++)
            {
                if (int.TryParse(parts[i], out int part))
                {
                    // Clamp each part to 999 to prevent overflow
                    part = Math.Min(part, 999);
                    versionInt += (long)part * multiplier;
                    multiplier /= 1000; // Move to next section (1000000000 -> 1000000 -> 1000 -> 1)
                }
                else
                {
                    if (debugLogs)
                    {
                        Debug.LogWarning($"[RemoteConfigService] Invalid version part: {parts[i]} in version {version}");
                    }
                }
            }
            
            return versionInt;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RemoteConfigService] Error converting version '{version}' to int: {ex.Message}");
            return 0;
        }
    }
    
    /// <summary>
    /// Check if player's version is outdated
    /// </summary>
    public bool IsVersionOutdated()
    {
        if (!isInitialized || configData == null)
        {
            return false;
        }
        
        try
        {
            // Compare current version with remote version using integer comparison
            string currentVersionString = Application.version;
            string remoteVersionString = configData.versionCode;
            
            long currentVersionInt = ConvertVersionToInt(currentVersionString);
            long remoteVersionInt = ConvertVersionToInt(remoteVersionString);
            
            if (debugLogs)
            {
                Debug.Log($"[RemoteConfigService] Version comparison - Current: {currentVersionString} ({currentVersionInt}) vs Remote: {remoteVersionString} ({remoteVersionInt})");
            }
            
            // Player's version is outdated if remote version is higher
            return remoteVersionInt > currentVersionInt;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RemoteConfigService] Error checking version: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Force refresh config from remote (useful for testing)
    /// </summary>
    [ContextMenu("Force Refresh Config")]
    public async UniTask ForceRefreshConfig()
    {
        if (firebaseController == null || !firebaseController.IsAvailable)
        {
            Debug.LogWarning("[RemoteConfigService] Firebase not available, cannot refresh config");
            return;
        }
        
        try
        {
            await FetchRemoteConfig();
            OnConfigLoaded?.Invoke(configData);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RemoteConfigService] Force refresh failed: {ex.Message}");
            OnConfigLoadFailed?.Invoke(ex.Message);
        }
    }
    
    private void OnDestroy()
    {
        initializationCancellation?.Cancel();
        initializationCancellation?.Dispose();
    }
}