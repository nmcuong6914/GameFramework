using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

/// <summary>
/// Generic GameObject pooling system that can be used for VFX, projectiles, enemies, etc.
/// Uses AssetKey system for type-safe asset loading and dependency injection.
/// 
/// Features:
/// - Predefined pools with custom settings configured in inspector
/// - Auto pool creation for assets not predefined (configurable)
/// - Async and sync pool operations
/// - Auto-return functionality with timers
/// - Pool cleanup and management
/// - Comprehensive debugging and monitoring
/// 
/// Usage:
/// - For best performance, predefine frequently used pools in the inspector
/// - Use GetAsync() for auto pool creation of new asset types
/// - Use Get() for synchronous access to predefined pools
/// </summary>
public class PoolManager : MonoBehaviour
{
    [System.Serializable]
    public class GameObjectPool
    {
        [Header("Configuration")]
        [Tooltip("The asset key for this object in the AssetReferenceCollection")]
        public AssetKey assetKey = AssetKey.None;
        
        [Tooltip("Initial pool size")]
        public int initialSize = 5;
        
        [Tooltip("Maximum pool size (0 = unlimited)")]
        public int maxSize = 20;
        
        [Tooltip("Auto-expand pool when empty")]
        public bool autoExpand = true;
        
        [Tooltip("Default auto-release time in seconds (0 = manual release)")]
        public float defaultAutoReleaseTime = 0f;
        
        [Header("Runtime Info")]
        [HideInInspector] public ObjectPool pool;
        [HideInInspector] public bool isLoaded = false;
        [HideInInspector] public GameObject prefab;
        [HideInInspector] public int activeCount = 0;
    }
    
    [Header("Configuration")]
    [Tooltip("List of GameObject pools managed by this pool manager")]
    [SerializeField] private List<GameObjectPool> pools = new List<GameObjectPool>();
    
    [Header("Performance Settings")]
    [Tooltip("Preload all pools on start")]
    [SerializeField] private bool preloadOnStart = true;
    
    [Tooltip("Auto-cleanup inactive pools after this time (0 = disabled)")]
    [SerializeField] private float poolCleanupInterval = 60f;
    
    [Tooltip("Cleanup pools with 0 active objects")]
    [SerializeField] private bool cleanupEmptyPools = false;
    
    [Header("Auto Pool Creation Settings")]
    [Tooltip("Allow creating new pools for assets not predefined in the pools list")]
    [SerializeField] private bool allowAutoPoolCreation = true;
    
    [Tooltip("Default initial size for auto-created pools")]
    [SerializeField] private int defaultInitialSize = 3;
    
    [Tooltip("Default maximum size for auto-created pools (0 = unlimited)")]
    [SerializeField] private int defaultMaxSize = 15;
    
    [Tooltip("Default auto-expand setting for auto-created pools")]
    [SerializeField] private bool defaultAutoExpand = true;
    
    [Tooltip("Default auto-release time for auto-created pools")]
    [SerializeField] private float defaultAutoReleaseTime = 0f;
    
    [Header("Runtime Info")]
    [SerializeField, ReadOnly] private int totalActivePools;
    [SerializeField, ReadOnly] private int totalActiveObjects;
    [SerializeField, ReadOnly] private int totalPooledObjects;
    
    // Runtime dependencies and lookup
    private Dictionary<AssetKey, GameObjectPool> poolDict = new Dictionary<AssetKey, GameObjectPool>();
    private AssetManager assetManager;
    private Coroutine cleanupCoroutine;

    // Events for monitoring
    public System.Action<AssetKey, GameObject> OnObjectSpawned;
    public System.Action<AssetKey, GameObject> OnObjectReturned;
    public System.Action<AssetKey> OnPoolInitialized;

    #region Unity Callbacks
    
    void Awake()
    {
        // Get AssetManager via DI
        assetManager = ServiceLocator.TryResolve<AssetManager>();
        
        // Initialize pool dictionaries
        InitializePoolDictionaries();
    }
    
    void OnDestroy()
    {
        if (cleanupCoroutine != null)
        {
            StopCoroutine(cleanupCoroutine);
        }
        
        // Cleanup all pools
        CleanupAllPools();
        
        // Unregister from ServiceLocator
        ServiceLocator.Unregister<PoolManager>();
    }
    
    #endregion
    
    #region Initialization
    
    private void InitializePoolDictionaries()
    {
        poolDict.Clear();
        
        foreach (var pool in pools)
        {
            poolDict[pool.assetKey] = pool;
        }
    }
    
    private async Task InitializeAllPools()
    {
        var initTasks = new List<Task>();
        foreach (var pool in pools)
        {
            initTasks.Add(InitializePool(pool));
        }
        
        await Task.WhenAll(initTasks);
        Debug.Log($"PoolManager: Initialized {pools.Count} pools");
    }
    
    private async Task InitializePool(GameObjectPool gameObjectPool)
    {
        try
        {
            // Load the prefab template
            gameObjectPool.prefab = await assetManager.LoadAssetAsync<GameObject>(gameObjectPool.assetKey);
            
            if (gameObjectPool.prefab != null)
            {
                // Initialize object pool
                gameObjectPool.pool = new ObjectPool();
                gameObjectPool.pool.Init(gameObjectPool.prefab, gameObjectPool.initialSize, this.transform);
                gameObjectPool.isLoaded = true;
                gameObjectPool.activeCount = 0;
                
                OnPoolInitialized?.Invoke(gameObjectPool.assetKey);
                Debug.Log($"PoolManager: Loaded pool for {gameObjectPool.assetKey}");
            }
            else
            {
                Debug.LogError($"PoolManager: Failed to load prefab for {gameObjectPool.assetKey}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"PoolManager: Exception initializing pool for {gameObjectPool.assetKey}: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Creates a pool for an asset key that wasn't predefined
    /// </summary>
    private async Task<GameObjectPool> CreateAutoPool(AssetKey assetKey)
    {
        try
        {
            // Try to load the prefab first to see if it exists
            var prefab = await assetManager.LoadAssetAsync<GameObject>(assetKey);
            if (prefab == null)
            {
                Debug.LogError($"PoolManager: Cannot create pool - asset not found for {assetKey}");
                return null;
            }
            
            // Create new pool with default settings
            var newPool = new GameObjectPool
            {
                assetKey = assetKey,
                initialSize = defaultInitialSize,
                maxSize = defaultMaxSize,
                autoExpand = defaultAutoExpand,
                defaultAutoReleaseTime = defaultAutoReleaseTime,
                prefab = prefab,
                isLoaded = false,
                activeCount = 0
            };
            
            // Add to pools list and dictionary
            pools.Add(newPool);
            poolDict[assetKey] = newPool;
            
            Debug.Log($"PoolManager: Created pool for {assetKey} with initial size {defaultInitialSize}");
            return newPool;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"PoolManager: Exception creating pool for {assetKey}: {ex.Message}");
            return null;
        }
    }
    
    #endregion
    
    #region Public API - Basic Operations
    
    /// <summary>
    /// Gets an object from the pool
    /// </summary>
    public async Task<GameObject> GetAsync(AssetKey assetKey, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
    {
        var pooledObject = await GetFromPool(assetKey);
        if (pooledObject != null)
        {
            SetupGameObject(pooledObject, position, rotation, parent);
            return pooledObject;
        }
        
        // Fallback to direct instantiation
        return await GetDirect(assetKey, position, rotation, parent);
    }
    
    /// <summary>
    /// Gets an object from the pool (synchronous)
    /// Note: For assets not predefined in pools, use GetAsync for auto pool creation
    /// </summary>
    public GameObject Get(AssetKey assetKey, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
    {
        if (!poolDict.TryGetValue(assetKey, out var pool))
        {
            if (allowAutoPoolCreation)
            {
                Debug.LogWarning($"PoolManager: Pool not found for {assetKey}. Use GetAsync for auto pool creation or predefine the pool.");
            }
            else
            {
                Debug.LogWarning($"PoolManager: Pool not found for {assetKey} and auto pool creation is disabled.");
            }
            return null;
        }
        
        if (!pool.isLoaded)
        {
            Debug.LogWarning($"PoolManager: Pool not ready for {assetKey}. Use GetAsync or preload the pool.");
            return null;
        }
        
        var pooledObject = pool.pool.Get();
        if (pooledObject != null)
        {
            SetupGameObject(pooledObject, position, rotation, parent);
            pool.activeCount++;
            OnObjectSpawned?.Invoke(assetKey, pooledObject);
            return pooledObject;
        }
        
        return null;
    }
    
    /// <summary>
    /// Returns an object to its pool
    /// </summary>
    public void Return(AssetKey assetKey, GameObject obj)
    {
        Debug.Log($"PoolManager: Returning object to pool {assetKey}");
        if (obj == null) return;
        
        if (poolDict.TryGetValue(assetKey, out var pool) && pool.isLoaded)
        {
            // Let the ObjectPool handle deactivation and reparenting
            pool.pool.Release(obj);
            pool.activeCount = Mathf.Max(0, pool.activeCount - 1);
            OnObjectReturned?.Invoke(assetKey, obj);
        }
        else
        {
            // Pool not available, destroy the object
            Destroy(obj);
        }
    }
    
    /// <summary>
    /// Gets an object with auto-return after specified time
    /// </summary>
    public async Task<GameObject> GetWithAutoReturn(AssetKey assetKey, Vector3 position = default, Quaternion rotation = default, Transform parent = null, float autoReturnTime = -1f)
    {
        var obj = await GetAsync(assetKey, position, rotation, parent);
        if (obj != null)
        {
            var pool = poolDict[assetKey];
            float returnTime = autoReturnTime > 0 ? autoReturnTime : pool.defaultAutoReleaseTime;
            
            if (returnTime > 0)
            {
                StartCoroutine(AutoReturn(assetKey, obj, returnTime));
            }
        }
        return obj;
    }
    
    #endregion
    
    #region Public API - Pool Management
    
    /// <summary>
    /// Preloads a specific pool, creates it automatically if it doesn't exist
    /// </summary>
    public async Task PreloadPool(AssetKey assetKey)
    {
        if (!poolDict.TryGetValue(assetKey, out var pool))
        {
            // Try to create auto pool if enabled
            if (allowAutoPoolCreation)
            {
                pool = await CreateAutoPool(assetKey);
                if (pool == null) return;
            }
            else
            {
                Debug.LogWarning($"PoolManager: Cannot preload pool for {assetKey} - pool not found and auto pool creation is disabled");
                return;
            }
        }
        
        if (!pool.isLoaded)
        {
            await InitializePool(pool);
        }
    }
    
    /// <summary>
    /// Manually creates a pool with custom settings
    /// </summary>
    public async Task<bool> CreatePoolWithSettings(AssetKey assetKey, int initialSize = -1, int maxSize = -1, bool? autoExpand = null, float? autoReleaseTime = null)
    {
        if (poolDict.ContainsKey(assetKey))
        {
            Debug.LogWarning($"PoolManager: Pool already exists for {assetKey}");
            return false;
        }
        
        if (!allowAutoPoolCreation)
        {
            Debug.LogWarning($"PoolManager: Auto pool creation is disabled");
            return false;
        }
        
        try
        {
            // Try to load the prefab first
            var prefab = await assetManager.LoadAssetAsync<GameObject>(assetKey);
            if (prefab == null)
            {
                Debug.LogError($"PoolManager: Cannot create pool - asset not found for {assetKey}");
                return false;
            }
            
            // Create new pool with specified or default settings
            var newPool = new GameObjectPool
            {
                assetKey = assetKey,
                initialSize = initialSize >= 0 ? initialSize : defaultInitialSize,
                maxSize = maxSize >= 0 ? maxSize : defaultMaxSize,
                autoExpand = autoExpand ?? defaultAutoExpand,
                defaultAutoReleaseTime = autoReleaseTime ?? defaultAutoReleaseTime,
                prefab = prefab,
                isLoaded = false,
                activeCount = 0
            };
            
            // Add to pools list and dictionary
            pools.Add(newPool);
            poolDict[assetKey] = newPool;
            
            Debug.Log($"PoolManager: Created pool for {assetKey} with custom settings");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"PoolManager: Exception creating pool for {assetKey}: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Returns all active objects of a specific type to their pool
    /// </summary>
    public void ReturnAllActive(AssetKey assetKey)
    {
        if (poolDict.TryGetValue(assetKey, out var pool) && pool.isLoaded)
        {
            int activeCount = pool.pool.ActiveCount;
            pool.pool.ReturnAllActive();
            pool.activeCount = 0; // Reset the active count since all objects are returned
            
            Debug.Log($"PoolManager: Returned {activeCount} active objects for {assetKey}");
        }
        else
        {
            Debug.LogWarning($"PoolManager: Cannot return active objects for {assetKey} - pool not found or not loaded");
        }
    }
    
    /// <summary>
    /// Clears a specific pool
    /// </summary>
    public void ClearPool(AssetKey assetKey)
    {
        if (poolDict.TryGetValue(assetKey, out var pool))
        {
            pool.pool = null;
            pool.isLoaded = false;
            pool.prefab = null;
            pool.activeCount = 0;
        }
    }
    
    #endregion
    
    #region Public API - Information
    
    /// <summary>
    /// Checks if a pool is available and loaded
    /// </summary>
    public bool IsPoolReady(AssetKey assetKey)
    {
        return poolDict.TryGetValue(assetKey, out var pool) && pool.isLoaded;
    }
    
    /// <summary>
    /// Gets pool information for debugging
    /// </summary>
    public PoolInfo GetPoolInfo(AssetKey assetKey)
    {
        if (poolDict.TryGetValue(assetKey, out var pool))
        {
            return new PoolInfo
            {
                assetKey = assetKey,
                isLoaded = pool.isLoaded,
                activeCount = pool.activeCount,
                poolCount = pool.pool?.Count ?? 0,
                maxSize = pool.maxSize
            };
        }
        return null;
    }
    
    /// <summary>
    /// Checks if auto pool creation is enabled
    /// </summary>
    public bool IsAutoPoolCreationEnabled()
    {
        return allowAutoPoolCreation;
    }
    
    /// <summary>
    /// Gets the total number of pools (including dynamically created ones)
    /// </summary>
    public int GetTotalPoolCount()
    {
        return poolDict.Count;
    }
    
    /// <summary>
    /// Gets the number of auto-created pools
    /// </summary>
    public int GetAutoCreatedPoolCount()
    {
        return poolDict.Count - pools.Count;
    }
    
    #endregion
    
    #region Private Methods
    
    private async Task<GameObject> GetFromPool(AssetKey assetKey)
    {
        if (!poolDict.TryGetValue(assetKey, out var pool))
        {
            // Try to create a pool if enabled
            if (allowAutoPoolCreation)
            {
                pool = await CreateAutoPool(assetKey);
                if (pool == null)
                {
                    Debug.LogWarning($"PoolManager: Failed to create pool for {assetKey}");
                    return null;
                }
            }
            else
            {
                Debug.LogWarning($"PoolManager: Pool not found for {assetKey} and auto pool creation is disabled");
                return null;
            }
        }
        
        // Initialize pool if not loaded
        if (!pool.isLoaded)
        {
            await InitializePool(pool);
            if (!pool.isLoaded)
            {
                return null;
            }
        }
        
        var obj = pool.pool.Get();
        if (obj != null)
        {
            pool.activeCount++;
            OnObjectSpawned?.Invoke(assetKey, obj);
        }
        
        return obj;
    }
    
    private async Task<GameObject> GetDirect(AssetKey assetKey, Vector3 position, Quaternion rotation, Transform parent)
    {
        var instance = await assetManager.LoadGameObjectAsync(assetKey, parent);
        if (instance != null)
        {
            SetupGameObject(instance, position, rotation, parent);
        }
        return instance;
    }
    
    private void SetupGameObject(GameObject obj, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (parent != null)
        {
            obj.transform.SetParent(parent, false);
        }
        
        obj.transform.position = position;
        if (rotation != default(Quaternion))
        {
            obj.transform.rotation = rotation;
        }
        
        obj.SetActive(true);
    }
    
    private IEnumerator AutoReturn(AssetKey assetKey, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (obj != null)
        {
            Return(assetKey, obj);
        }
    }
    
    private void CleanupAllPools()
    {
        foreach (var pool in pools)
        {
            pool.pool = null;
            pool.isLoaded = false;
            pool.prefab = null;
            pool.activeCount = 0;
        }
        
        totalActiveObjects = 0;
        totalPooledObjects = 0;
        totalActivePools = 0;
    }
    
    private IEnumerator PeriodicCleanup()
    {
        while (true)
        {
            yield return new WaitForSeconds(poolCleanupInterval);
            
            UpdateDebugInfo();
            
            if (cleanupEmptyPools)
            {
                PerformEmptyPoolCleanup();
            }
            
            yield return null;
        }
    }
    
    private void PerformEmptyPoolCleanup()
    {
        foreach (var pool in pools)
        {
            if (pool.isLoaded && pool.activeCount == 0)
            {
                // Pool is empty, could be cleaned up
                // Implementation depends on your needs
            }
        }
    }
    
    private void UpdateDebugInfo()
    {
        totalActivePools = 0;
        totalActiveObjects = 0;
        totalPooledObjects = 0;
        
        foreach (var pool in pools)
        {
            if (pool.isLoaded)
            {
                totalActivePools++;
                totalActiveObjects += pool.activeCount;
                totalPooledObjects += pool.pool?.Count ?? 0;
            }
        }
    }
    
    #endregion
    
    #region Utility Classes
    
    [System.Serializable]
    public class PoolInfo
    {
        public AssetKey assetKey;
        public bool isLoaded;
        public int activeCount;
        public int poolCount;
        public int maxSize;
    }
    
    #endregion
    
    #region Debug Methods
    
    /// <summary>
    /// Logs status of all pools
    /// </summary>
    public void LogAllPoolStatus()
    {
        Debug.Log("=== Pool Manager Status ===");
        Debug.Log($"Auto Pool Creation Enabled: {allowAutoPoolCreation}");
        Debug.Log($"Total Pools: {poolDict.Count} (Predefined: {pools.Count}, Auto-Created: {GetAutoCreatedPoolCount()})");
        Debug.Log($"Total Active Objects: {totalActiveObjects}, Total Pooled Objects: {totalPooledObjects}");
        Debug.Log("--- Pool Details ---");
        
        foreach (var pool in pools)
        {
            string status = pool.isLoaded ? "Loaded" : "Not Loaded";
            int poolCount = pool.pool?.Count ?? 0;
            string poolType = "Predefined";
            
            Debug.Log($"{pool.assetKey} | Type: {poolType} | Status: {status} | Active: {pool.activeCount} | Pool: {poolCount}");
        }
        
        // Show any additional auto-created pools not in the predefined list
        foreach (var kvp in poolDict)
        {
            if (!pools.Exists(p => p.assetKey == kvp.Key))
            {
                var pool = kvp.Value;
                string status = pool.isLoaded ? "Loaded" : "Not Loaded";
                int poolCount = pool.pool?.Count ?? 0;
                string poolType = "Auto-Created";
                
                Debug.Log($"{pool.assetKey} | Type: {poolType} | Status: {status} | Active: {pool.activeCount} | Pool: {poolCount}");
            }
        }
    }
    
    #endregion
    
    #region Editor Methods
    
#if UNITY_EDITOR
    [ContextMenu("Log All Pool Status")]
    private void EditorLogAllPoolStatus()
    {
        LogAllPoolStatus();
    }
    
    [ContextMenu("Test First Pool")]
    private void EditorTestFirstPool()
    {
        if (Application.isPlaying && pools.Count > 0)
        {
            _ = GetWithAutoReturn(pools[0].assetKey, Vector3.zero, Quaternion.identity, null, 3f);
        }
    }
    
    [ContextMenu("Toggle Auto Pool Creation")]
    private void EditorToggleAutoPoolCreation()
    {
        allowAutoPoolCreation = !allowAutoPoolCreation;
        Debug.Log($"Auto Pool Creation: {(allowAutoPoolCreation ? "Enabled" : "Disabled")}");
    }
#endif
    
    #endregion
}
