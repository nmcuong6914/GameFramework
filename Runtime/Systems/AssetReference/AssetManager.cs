using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// Manager class for loading and accessing assets through the AssetReferenceCollection system
/// Provides convenient methods for loading assets by their AssetKey
/// </summary>
public class AssetManager : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The asset reference collection containing all asset mappings")]
    [SerializeField] private AssetReferenceCollection assetCollection;
    
    [Header("Runtime Info")]
    [Tooltip("Shows the number of currently loaded assets")]
    [SerializeField, ReadOnly] private int loadedAssetsCount;
    
    // Singleton instance (for backward compatibility)
    public static AssetManager Instance { get; private set; }
    
    // Improved asset caching - tracks both prefab templates and instances
    private Dictionary<AssetKey, Object> loadedAssetTemplates = new Dictionary<AssetKey, Object>(); // Templates/Prefabs
    private Dictionary<AssetKey, List<GameObject>> instantiatedObjects = new Dictionary<AssetKey, List<GameObject>>(); // Instances
    private Dictionary<AssetKey, AsyncOperationHandle> activeHandles = new Dictionary<AssetKey, AsyncOperationHandle>();
    
    #region Unity Callbacks
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Register with Service Locator for DI
            ServiceLocator.Register<AssetManager>(this);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        ValidateConfiguration();
    }
    
    private void OnDestroy()
    {
        // Clean up any active handles when the manager is destroyed
        CleanupAllHandles();
        
        if (Instance == this)
        {
            // Unregister from Service Locator
            ServiceLocator.Unregister<AssetManager>();
            Instance = null;
        }
    }
    
    #endregion
    
    #region Public API - Sync Access
    
    /// <summary>
    /// Gets an asset reference by its key
    /// </summary>
    /// <param name="key">The asset key to look up</param>
    /// <returns>The asset reference, or null if not found</returns>
    public AssetReference GetAssetReference(AssetKey key)
    {
        if (assetCollection == null)
        {
            Debug.LogError("AssetCollection is not assigned to AssetManager");
            return null;
        }
        
        return assetCollection.GetAssetReference(key);
    }
    
    /// <summary>
    /// Checks if an asset key exists in the collection
    /// </summary>
    public bool HasAsset(AssetKey key)
    {
        return assetCollection != null && assetCollection.ContainsAssetKey(key);
    }
    
    /// <summary>
    /// Gets asset entry information by key
    /// </summary>
    public AssetReferenceCollection.AssetEntry GetAssetEntry(AssetKey key)
    {
        if (assetCollection != null && assetCollection.TryGetAssetEntry(key, out var entry))
        {
            return entry;
        }
        return null;
    }
    
    #endregion
    
    #region Public API - Async Loading
    
    /// <summary>
    /// Loads a GameObject asset asynchronously by its key
    /// </summary>
    /// <param name="key">The asset key to load</param>
    /// <param name="parent">Optional parent transform for instantiated object</param>
    /// <returns>The loaded GameObject, or null if loading failed</returns>
    public async Task<GameObject> LoadGameObjectAsync(AssetKey key, Transform parent = null)
    {
        var assetReference = GetAssetReference(key);
        if (assetReference == null)
        {
            Debug.LogError($"No asset reference found for key: {key}");
            return null;
        }
        
        return await LoadGameObjectAsync(assetReference, parent);
    }
    
    /// <summary>
    /// Loads a GameObject asset asynchronously from an AssetReference
    /// </summary>
    /// <param name="assetReference">The asset reference to load</param>
    /// <param name="parent">Optional parent transform for instantiated object</param>
    /// <returns>The loaded GameObject, or null if loading failed</returns>
    public async Task<GameObject> LoadGameObjectAsync(AssetReference assetReference, Transform parent = null)
    {
        if (assetReference == null || !assetReference.RuntimeKeyIsValid())
        {
            Debug.LogError("Invalid asset reference provided");
            return null;
        }
        
        try
        {
            AsyncOperationHandle<GameObject> handle;
            
            if (parent != null)
            {
                handle = Addressables.InstantiateAsync(assetReference, parent);
            }
            else
            {
                handle = Addressables.InstantiateAsync(assetReference);
            }
            
            var result = await handle.Task;
            
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                loadedAssetsCount++;
                
                // Track instantiated object for proper cleanup
                var assetKey = FindAssetKeyByReference(assetReference);
                if (assetKey != AssetKey.None) // Only track valid asset keys
                {
                    if (!instantiatedObjects.ContainsKey(assetKey))
                    {
                        instantiatedObjects[assetKey] = new List<GameObject>();
                    }
                    instantiatedObjects[assetKey].Add(result);
                }
                
                return result;
            }
            else
            {
                Debug.LogError($"Failed to load GameObject from AssetReference: {handle.OperationException}");
                return null;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Exception loading GameObject from AssetReference: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Loads an asset of type T asynchronously by its key
    /// </summary>
    /// <typeparam name="T">The type of asset to load</typeparam>
    /// <param name="key">The asset key to load</param>
    /// <returns>The loaded asset, or default(T) if loading failed</returns>
    public async Task<T> LoadAssetAsync<T>(AssetKey key) where T : Object
    {
        var assetReference = GetAssetReference(key);
        if (assetReference == null)
        {
            Debug.LogError($"No asset reference found for key: {key}");
            return default(T);
        }
        
        return await LoadAssetAsync<T>(assetReference);
    }
    
    /// <summary>
    /// Loads an asset of type T asynchronously from an AssetReference
    /// </summary>
    /// <typeparam name="T">The type of asset to load</typeparam>
    /// <param name="assetReference">The asset reference to load</param>
    /// <returns>The loaded asset, or default(T) if loading failed</returns>
    public async Task<T> LoadAssetAsync<T>(AssetReference assetReference) where T : Object
    {
        if (assetReference == null || !assetReference.RuntimeKeyIsValid())
        {
            Debug.LogError("Invalid asset reference provided");
            return default(T);
        }
        
        try
        {
            var handle = Addressables.LoadAssetAsync<T>(assetReference);
            var result = await handle.Task;
            
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                loadedAssetsCount++;
                return result;
            }
            else
            {
                Debug.LogError($"Failed to load asset of type {typeof(T)} from AssetReference: {handle.OperationException}");
                return default(T);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Exception loading asset of type {typeof(T)} from AssetReference: {ex.Message}");
            return default(T);
        }
    }
    
    #endregion
    
    #region Public API - Preloading
    
    /// <summary>
    /// Preloads multiple assets by category for faster access
    /// </summary>
    /// <param name="category">The category of assets to preload</param>
    /// <returns>Task that completes when preloading is done</returns>
    public async Task PreloadAssetsByCategory(AssetCategory category)
    {
        if (assetCollection == null)
        {
            Debug.LogError("AssetCollection is not assigned to AssetManager");
            return;
        }
        
        var assetsInCategory = assetCollection.GetAssetsByCategory(category);
        var preloadTasks = new List<Task>();
        
        foreach (var assetEntry in assetsInCategory)
        {
            if (assetEntry.assetReference != null && assetEntry.assetReference.RuntimeKeyIsValid())
            {
                preloadTasks.Add(PreloadAssetReference(assetEntry.key, assetEntry.assetReference));
            }
        }
        
        await Task.WhenAll(preloadTasks);
        Debug.Log($"Preloaded {preloadTasks.Count} assets from category: {category}");
    }
    
    /// <summary>
    /// Preloads a specific asset for faster access
    /// </summary>
    /// <param name="key">The asset key to preload</param>
    /// <param name="assetReference">The asset reference to preload</param>
    private async Task PreloadAssetReference(AssetKey key, AssetReference assetReference)
    {
        try
        {
            var handle = Addressables.LoadAssetAsync<Object>(assetReference);
            activeHandles[key] = handle;
            
            await handle.Task;
            
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"Preloaded asset: {key}");
            }
            else
            {
                Debug.LogWarning($"Failed to preload asset {key}: {handle.OperationException}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Exception preloading asset {key}: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Public API - Resource Management
    
    /// <summary>
    /// Releases a specific loaded asset
    /// </summary>
    /// <param name="key">The key of the asset to release</param>
    public void ReleaseAsset(AssetKey key)
    {
        if (activeHandles.TryGetValue(key, out var handle))
        {
            Addressables.Release(handle);
            activeHandles.Remove(key);
            loadedAssetsCount--;
        }
        
        // Release asset template
        if (loadedAssetTemplates.TryGetValue(key, out var template))
        {
            if (template != null)
            {
                Addressables.Release(template);
            }
            loadedAssetTemplates.Remove(key);
        }
        
        // Release instantiated objects
        if (instantiatedObjects.TryGetValue(key, out var instances))
        {
            foreach (var instance in instances)
            {
                if (instance != null)
                {
                    Addressables.ReleaseInstance(instance);
                }
            }
            instantiatedObjects.Remove(key);
        }
    }
    
    /// <summary>
    /// Releases a specific instantiated GameObject
    /// </summary>
    /// <param name="gameObject">The GameObject instance to release</param>
    public void ReleaseInstance(GameObject gameObject)
    {
        if (gameObject == null) return;
        
        // Find and remove from tracking
        foreach (var kvp in instantiatedObjects)
        {
            if (kvp.Value.Contains(gameObject))
            {
                kvp.Value.Remove(gameObject);
                break;
            }
        }
        
        Addressables.ReleaseInstance(gameObject);
        loadedAssetsCount--;
    }
    
    /// <summary>
    /// Releases all loaded assets
    /// </summary>
    public void ReleaseAllAssets()
    {
        CleanupAllHandles();
        
        // Release all asset templates
        foreach (var template in loadedAssetTemplates.Values)
        {
            if (template != null)
            {
                Addressables.Release(template);
            }
        }
        loadedAssetTemplates.Clear();
        
        // Release all instantiated objects
        foreach (var instances in instantiatedObjects.Values)
        {
            foreach (var instance in instances)
            {
                if (instance != null)
                {
                    Addressables.ReleaseInstance(instance);
                }
            }
        }
        instantiatedObjects.Clear();
        
        loadedAssetsCount = 0;
        
        Debug.Log("Released all loaded assets");
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Gets assets by category for inspection or iteration
    /// </summary>
    public List<AssetReferenceCollection.AssetEntry> GetAssetsByCategory(AssetCategory category)
    {
        return assetCollection?.GetAssetsByCategory(category) ?? new List<AssetReferenceCollection.AssetEntry>();
    }
    
    /// <summary>
    /// Gets all available asset keys
    /// </summary>
    public System.Collections.Generic.IEnumerable<AssetKey> GetAllAssetKeys()
    {
        return assetCollection?.GetAllAssetKeys() ?? new AssetKey[0];
    }
    
    #endregion
    
    #region Private Methods
    
    private void ValidateConfiguration()
    {
        if (assetCollection == null)
        {
            Debug.LogError("AssetManager is missing AssetReferenceCollection! Please assign one in the inspector.");
        }
    }
    
    private void CleanupAllHandles()
    {
        foreach (var handle in activeHandles.Values)
        {
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }
        activeHandles.Clear();
    }
    
    /// <summary>
    /// Helper method to find AssetKey by AssetReference (for tracking)
    /// </summary>
    private AssetKey FindAssetKeyByReference(AssetReference assetReference)
    {
        if (assetCollection == null) return default(AssetKey);
        
        foreach (var key in assetCollection.GetAllAssetKeys())
        {
            var reference = assetCollection.GetAssetReference(key);
            if (reference == assetReference)
            {
                return key;
            }
        }
        
        return default(AssetKey); // Return default if not found
    }
    
    #endregion
    
    #region Debug Methods
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void UpdateLoadedAssetsCount()
    {
        int totalInstances = 0;
        foreach (var instances in instantiatedObjects.Values)
        {
            totalInstances += instances.Count;
        }
        loadedAssetsCount = activeHandles.Count + loadedAssetTemplates.Count + totalInstances;
    }
    
    #endregion
}
