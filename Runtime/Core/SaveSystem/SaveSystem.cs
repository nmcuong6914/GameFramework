using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// Cross-platform save system for handling local persistence of player data
/// Supports Android, iOS, and all other Unity platforms
/// </summary>
public class SaveSystem
{
    private const string SAVE_FILE_NAME = "playerdata.json";
    private const string BACKUP_FILE_NAME = "playerdata_backup.json";
    private const string SAVE_FOLDER_NAME = "SaveData";
    private const int MAX_RETRY_ATTEMPTS = 5;
    private const int RETRY_DELAY_MS = 100;
    
    private string saveFolderPath;
    private string saveFilePath;
    private string backupFilePath;
    
    // Semaphore to prevent concurrent save operations
    private readonly SemaphoreSlim saveLock = new SemaphoreSlim(1, 1);
    
    // Events
    public event Action<PlayerData> DataSaved;
    public event Action<PlayerData> DataLoaded;
    public event Action<string> SaveFailed;
    public event Action<string> LoadFailed;
    
    public SaveSystem()
    {
        InitializePaths();
    }
    
    private void InitializePaths()
    {
        // Use Application.persistentDataPath for cross-platform compatibility
        // This path is writable on all platforms including Android and iOS
        string basePath = Application.persistentDataPath;
        
        // Platform-specific adjustments
        #if UNITY_ANDROID && !UNITY_EDITOR
            // On Android, ensure we're using the correct persistent data path
            // Application.persistentDataPath points to /storage/emulated/0/Android/data/<packagename>/files
            FDebug.Log($"Android persistent data path: {basePath}");
        #elif UNITY_IOS && !UNITY_EDITOR
            // On iOS, Application.persistentDataPath points to Documents directory
            // which is backed up to iTunes/iCloud by default
            FDebug.Log($"iOS persistent data path: {basePath}");
        #endif
        
        saveFolderPath = Path.Combine(basePath, SAVE_FOLDER_NAME);
        saveFilePath = Path.Combine(saveFolderPath, SAVE_FILE_NAME);
        backupFilePath = Path.Combine(saveFolderPath, BACKUP_FILE_NAME);
        
        // Ensure save directory exists with proper error handling
        try
        {
            if (!Directory.Exists(saveFolderPath))
            {
                Directory.CreateDirectory(saveFolderPath);
                FDebug.Log($"Created save directory: {saveFolderPath}");
            }
        }
        catch (Exception ex)
        {
            FDebug.LogError($"Failed to create save directory: {ex.Message}");
            // Fallback to root persistent data path if subfolder creation fails
            saveFolderPath = basePath;
            saveFilePath = Path.Combine(saveFolderPath, SAVE_FILE_NAME);
            backupFilePath = Path.Combine(saveFolderPath, BACKUP_FILE_NAME);
        }
    }
    
    /// <summary>
    /// Save player data to local storage with cross-platform compatibility
    /// </summary>
    public async UniTask<bool> SaveAsync(PlayerData playerData)
    {
        if (playerData == null)
        {
            FDebug.LogError("Cannot save null PlayerData");
            SaveFailed?.Invoke("PlayerData is null");
            return false;
        }

        try
        {
            string json = playerData.ToJson();
            
            // Create backup of existing save file
            if (File.Exists(saveFilePath))
            {
                try
                {
                    File.Copy(saveFilePath, backupFilePath, overwrite: true);
                }
                catch (Exception backupEx)
                {
                    FDebug.LogWarning($"Failed to create backup: {backupEx.Message}");
                    // Continue with save even if backup fails
                }
            }
            
            // Use atomic save approach with platform-specific handling
            string tempFilePath = saveFilePath + ".tmp";
            
            #if UNITY_ANDROID || UNITY_IOS
                // On mobile platforms, use direct file writing with proper exception handling
                await WriteFileAsync(tempFilePath, json);
                
                // Atomic move operation
                if (File.Exists(saveFilePath))
                {
                    File.Delete(saveFilePath);
                }
                File.Move(tempFilePath, saveFilePath);
            #else
                // On other platforms, use standard approach
                await File.WriteAllTextAsync(tempFilePath, json);
                
                if (File.Exists(saveFilePath))
                {
                    File.Delete(saveFilePath);
                }
                File.Move(tempFilePath, saveFilePath);
            #endif
            
            playerData.NotifyDataSaved();
            DataSaved?.Invoke(playerData);
            
            FDebug.Log($"PlayerData saved successfully to: {saveFilePath}");
            return true;
        }
        catch (Exception ex)
        {
            FDebug.LogError($"Failed to save PlayerData: {ex.Message}");
            SaveFailed?.Invoke(ex.Message);
            
            // Clean up temp file if it exists
            try
            {
                string tempFilePath = saveFilePath + ".tmp";
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
            catch (Exception cleanupEx)
            {
                FDebug.LogWarning($"Failed to cleanup temp file: {cleanupEx.Message}");
            }
            
            return false;
        }
    }    /// <summary>
    /// Load player data from local storage with cross-platform compatibility
    /// </summary>
    public async UniTask<PlayerData> LoadAsync()
    {
        try
        {
            string filePath = saveFilePath;
            
            // If main save file doesn't exist, try backup
            if (!File.Exists(filePath))
            {
                if (File.Exists(backupFilePath))
                {
                    filePath = backupFilePath;
                    FDebug.LogWarning("Main save file not found, loading from backup");
                }
                else
                {
                    FDebug.Log("No save file found, creating new PlayerData");
                    return null;
                }
            }
            
            string json;
            
            #if UNITY_ANDROID || UNITY_IOS
                // On mobile platforms, use custom read method for better compatibility
                json = await ReadFileAsync(filePath);
            #else
                // On other platforms, use standard method
                json = await File.ReadAllTextAsync(filePath);
            #endif
            
            if (string.IsNullOrEmpty(json))
            {
                FDebug.LogWarning("Save file is empty");
                return null;
            }
            
            PlayerData playerData = PlayerData.FromJson(json);
            
            if (playerData != null)
            {
                playerData.NotifyDataLoaded();
                DataLoaded?.Invoke(playerData);
                FDebug.Log($"PlayerData loaded successfully from: {filePath}");
            }
            else
            {
                FDebug.LogError("Failed to deserialize PlayerData from JSON");
                LoadFailed?.Invoke("Failed to deserialize JSON");
            }
            
            return playerData;
        }
        catch (Exception ex)
        {
            FDebug.LogError($"Failed to load PlayerData: {ex.Message}");
            LoadFailed?.Invoke(ex.Message);
            
            // Try to load from backup if main file failed
            if (File.Exists(backupFilePath) && saveFilePath != backupFilePath)
            {
                FDebug.Log("Attempting to load from backup file");
                try
                {
                    string json;
                    #if UNITY_ANDROID || UNITY_IOS
                        json = await ReadFileAsync(backupFilePath);
                    #else
                        json = await File.ReadAllTextAsync(backupFilePath);
                    #endif
                    
                    PlayerData playerData = PlayerData.FromJson(json);
                    
                    if (playerData != null)
                    {
                        playerData.NotifyDataLoaded();
                        DataLoaded?.Invoke(playerData);
                        FDebug.Log("PlayerData loaded successfully from backup");
                        return playerData;
                    }
                }
                catch (Exception backupEx)
                {
                    FDebug.LogError($"Failed to load from backup: {backupEx.Message}");
                }
            }
            
            return null;
        }
    }
    
    #if UNITY_ANDROID || UNITY_IOS
    /// <summary>
    /// Custom file write method for mobile platforms to ensure compatibility
    /// </summary>
    private async UniTask WriteFileAsync(string filePath, string content)
    {
        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
        {
            using (var writer = new StreamWriter(fileStream))
            {
                await writer.WriteAsync(content);
                await writer.FlushAsync();
            }
        }
    }
    
    /// <summary>
    /// Custom file read method for mobile platforms to ensure compatibility
    /// </summary>
    private async UniTask<string> ReadFileAsync(string filePath)
    {
        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
        {
            using (var reader = new StreamReader(fileStream))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }
    #endif
    
    /// <summary>
    /// Check if save file exists
    /// </summary>
    public bool SaveFileExists()
    {
        return File.Exists(saveFilePath) || File.Exists(backupFilePath);
    }
    
    /// <summary>
    /// Delete save file (use with caution)
    /// </summary>
    public bool DeleteSaveFile()
    {
        try
        {
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
            }
            
            if (File.Exists(backupFilePath))
            {
                File.Delete(backupFilePath);
            }
            
            FDebug.Log("Save files deleted successfully");
            return true;
        }
        catch (Exception ex)
        {
            FDebug.LogError($"Failed to delete save files: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Get save file size in bytes
    /// </summary>
    public long GetSaveFileSize()
    {
        if (File.Exists(saveFilePath))
        {
            return new FileInfo(saveFilePath).Length;
        }
        return 0;
    }
    
    /// <summary>
    /// Get last save time
    /// </summary>
    public DateTime? GetLastSaveTime()
    {
        if (File.Exists(saveFilePath))
        {
            return File.GetLastWriteTime(saveFilePath);
        }
        return null;
    }
    
    /// <summary>
    /// Create manual backup with cross-platform compatibility
    /// </summary>
    public async UniTask<bool> CreateBackupAsync(PlayerData playerData, string backupSuffix = null)
    {
        if (playerData == null) return false;
        
        try
        {
            string suffix = backupSuffix ?? DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupFileName = $"playerdata_backup_{suffix}.json";
            string backupPath = Path.Combine(saveFolderPath, backupFileName);
            
            string json = playerData.ToJson();
            
            #if UNITY_ANDROID || UNITY_IOS
                await WriteFileAsync(backupPath, json);
            #else
                await File.WriteAllTextAsync(backupPath, json);
            #endif
            
            FDebug.Log($"Manual backup created: {backupPath}");
            return true;
        }
        catch (Exception ex)
        {
            FDebug.LogError($"Failed to create manual backup: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Get platform information for debugging
    /// </summary>
    public string GetPlatformInfo()
    {
        return $"Platform: {Application.platform}, " +
               $"Persistent Data Path: {Application.persistentDataPath}, " +
               $"Save Folder: {saveFolderPath}";
    }
    
    /// <summary>
    /// Get the save folder path (for editor tools)
    /// </summary>
    public string GetSaveFolderPath()
    {
        return saveFolderPath;
    }
    
    /// <summary>
    /// Get the main save file path (for editor tools)
    /// </summary>
    public string GetSaveFilePath()
    {
        return saveFilePath;
    }
}
