using System;
using System.IO;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// Cross-platform save system for handling local persistence of serializable data.
/// Supports Android, iOS, and all other Unity platforms.
/// </summary>
public class SaveSystem<T> where T : class
{
    private const string SAVE_FILE_NAME = "savedata.json";
    private const string BACKUP_FILE_NAME = "savedata_backup.json";
    private const string SAVE_FOLDER_NAME = "SaveData";
    
    private readonly string saveFolderPath;
    private readonly string saveFilePath;
    private readonly string backupFilePath;
    
    // Semaphore to prevent concurrent save operations
    private readonly SemaphoreSlim saveLock = new SemaphoreSlim(1, 1);
    
    // Events
    public event Action<T> DataSaved;
    public event Action<T> DataLoaded;
    public event Action<string> SaveFailed;
    public event Action<string> LoadFailed;
    
    public SaveSystem(string filename = SAVE_FILE_NAME, string backupFilename = BACKUP_FILE_NAME)
    {
        string basePath = Application.persistentDataPath;
        
        #if UNITY_ANDROID && !UNITY_EDITOR
            FDebug.Log($"Android persistent data path: {basePath}");
        #elif UNITY_IOS && !UNITY_EDITOR
            FDebug.Log($"iOS persistent data path: {basePath}");
        #endif
        
        saveFolderPath = Path.Combine(basePath, SAVE_FOLDER_NAME);
        saveFilePath = Path.Combine(saveFolderPath, filename);
        backupFilePath = Path.Combine(saveFolderPath, backupFilename);
        
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
            saveFolderPath = basePath;
            saveFilePath = Path.Combine(saveFolderPath, filename);
            backupFilePath = Path.Combine(saveFolderPath, backupFilename);
        }
    }
    
    /// <summary>
    /// Save data to local storage with cross-platform compatibility
    /// </summary>
    public async UniTask<bool> SaveAsync(T data)
    {
        if (data == null)
        {
            FDebug.LogError("Cannot save null data");
            SaveFailed?.Invoke("Data is null");
            return false;
        }

        await saveLock.WaitAsync();
        try
        {
            string json = JsonUtility.ToJson(data);
            
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
                }
            }
            
            string tempFilePath = saveFilePath + ".tmp";
            
            #if UNITY_ANDROID || UNITY_IOS
                await WriteFileAsync(tempFilePath, json);
            #else
                await File.WriteAllTextAsync(tempFilePath, json);
            #endif
            
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
            }
            File.Move(tempFilePath, saveFilePath);
            
            DataSaved?.Invoke(data);
            FDebug.Log($"Data saved successfully to: {saveFilePath}");
            return true;
        }
        catch (Exception ex)
        {
            FDebug.LogError($"Failed to save data: {ex.Message}");
            SaveFailed?.Invoke(ex.Message);
            
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
        finally
        {
            saveLock.Release();
        }
    }
    
    /// <summary>
    /// Load data from local storage with cross-platform compatibility
    /// </summary>
    public async UniTask<T> LoadAsync()
    {
        try
        {
            string filePath = saveFilePath;
            
            if (!File.Exists(filePath))
            {
                if (File.Exists(backupFilePath))
                {
                    filePath = backupFilePath;
                    FDebug.LogWarning("Main save file not found, loading from backup");
                }
                else
                {
                    FDebug.Log("No save file found");
                    return null;
                }
            }
            
            string json;
            
            #if UNITY_ANDROID || UNITY_IOS
                json = await ReadFileAsync(filePath);
            #else
                json = await File.ReadAllTextAsync(filePath);
            #endif
            
            if (string.IsNullOrEmpty(json))
            {
                FDebug.LogWarning("Save file is empty");
                return null;
            }
            
            T data = JsonUtility.FromJson<T>(json);
            
            if (data != null)
            {
                DataLoaded?.Invoke(data);
                FDebug.Log($"Data loaded successfully from: {filePath}");
            }
            else
            {
                FDebug.LogError("Failed to deserialize data from JSON");
                LoadFailed?.Invoke("Failed to deserialize JSON");
            }
            
            return data;
        }
        catch (Exception ex)
        {
            FDebug.LogError($"Failed to load data: {ex.Message}");
            LoadFailed?.Invoke(ex.Message);
            
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
                    
                    T data = JsonUtility.FromJson<T>(json);
                    
                    if (data != null)
                    {
                        DataLoaded?.Invoke(data);
                        FDebug.Log("Data loaded successfully from backup");
                        return data;
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
    
    public bool SaveFileExists()
    {
        return File.Exists(saveFilePath) || File.Exists(backupFilePath);
    }
    
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
    
    public long GetSaveFileSize()
    {
        if (File.Exists(saveFilePath))
        {
            return new FileInfo(saveFilePath).Length;
        }
        return 0;
    }
    
    public DateTime? GetLastSaveTime()
    {
        if (File.Exists(saveFilePath))
        {
            return File.GetLastWriteTime(saveFilePath);
        }
        return null;
    }
    
    public async UniTask<bool> CreateBackupAsync(T data, string backupSuffix = null)
    {
        if (data == null) return false;
        
        try
        {
            string suffix = backupSuffix ?? DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupFileName = $"savedata_backup_{suffix}.json";
            string backupPath = Path.Combine(saveFolderPath, backupFileName);
            
            string json = JsonUtility.ToJson(data);
            
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
    
    public string GetPlatformInfo()
    {
        return $"Platform: {Application.platform}, " +
               $"Persistent Data Path: {Application.persistentDataPath}, " +
               $"Save Folder: {saveFolderPath}";
    }
    
    public string GetSaveFolderPath()
    {
        return saveFolderPath;
    }
    
    public string GetSaveFilePath()
    {
        return saveFilePath;
    }
}
