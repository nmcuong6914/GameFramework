using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

/// <summary>
/// VFX Controller that uses the new PoolManager system for better performance and reusability
/// Focused specifically on VFX management and effects
/// </summary>
public class VFXController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Default auto-release time for VFX")]
    [SerializeField] private float defaultAutoReleaseTime = 2f;
    
    [Header("Runtime Info")]
    [SerializeField, ReadOnly] private int totalActiveVFX;
    
    // Dependencies
    private PoolManager poolManager;
    
    // Events
    public System.Action<AssetKey, GameObject> OnVFXPlayed;
    public System.Action<AssetKey, GameObject> OnVFXStopped;

    #region Unity Callbacks


    void Start()
    {
        poolManager = ServiceLocator.TryResolve<PoolManager>();
        
        // Subscribe to pool events
        poolManager.OnObjectSpawned += OnPoolObjectSpawned;
        poolManager.OnObjectReturned += OnPoolObjectReturned;
    }
    
    void OnDestroy()
    {
        // Unsubscribe from pool events
        if (poolManager != null)
        {
            poolManager.OnObjectSpawned -= OnPoolObjectSpawned;
            poolManager.OnObjectReturned -= OnPoolObjectReturned;
        }
        
        // Unregister from ServiceLocator
        ServiceLocator.Unregister<VFXController>();
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Get auto-release time - simply returns the default time
    /// </summary>
    private float GetAutoReleaseTime(AssetKey assetKey)
    {
        return defaultAutoReleaseTime;
    }
    
    /// <summary>
    /// Handle pool object spawned event
    /// </summary>
    private void OnPoolObjectSpawned(AssetKey assetKey, GameObject obj)
    {
        if (!assetKey.IsVFX()) return;
        
        totalActiveVFX++;
        OnVFXPlayed?.Invoke(assetKey, obj);
    }
    
    /// <summary>
    /// Handle pool object returned event
    /// </summary>
    private void OnPoolObjectReturned(AssetKey assetKey, GameObject obj)
    {
        if (!assetKey.IsVFX()) return;
        
        totalActiveVFX = Mathf.Max(0, totalActiveVFX - 1);
        OnVFXStopped?.Invoke(assetKey, obj);
    }
    
    #endregion
    
    #region Public API - Async Methods
    
    /// <summary>
    /// Play VFX at specified position with rotation (Async)
    /// </summary>
    /// <param name="assetKey">The VFX asset key</param>
    /// <param name="position">World position to play VFX</param>
    /// <param name="rotation">Rotation for the VFX</param>
    /// <param name="autoRelease">Auto-return to pool after specified time</param>
    /// <param name="customAutoReleaseTime">Custom auto-release time (overrides config)</param>
    /// <returns>The spawned VFX GameObject</returns>
    public async Task<GameObject> PlayVFXAsync(AssetKey assetKey, Vector3 position, Quaternion rotation, bool autoRelease = true, float? customAutoReleaseTime = null)
    {
        if (poolManager == null)
        {
            FDebug.LogError("VFXController: PoolManager not available!");
            return null;
        }
        
        if (!assetKey.IsVFX())
        {
            FDebug.LogError($"VFXController: Asset key {assetKey} is not a VFX asset!");
            return null;
        }
        
        // Get VFX from pool
        var vfxInstance = await poolManager.GetAsync(assetKey);
        if (vfxInstance == null)
        {
            FDebug.LogError($"VFXController: Failed to get VFX instance for {assetKey}");
            return null;
        }
        
        // Setup position and rotation
        vfxInstance.transform.position = position;
        vfxInstance.transform.rotation = rotation;
        
        // Setup auto-release if enabled
        if (autoRelease)
        {
            float releaseTime = customAutoReleaseTime ?? GetAutoReleaseTime(assetKey);
            StartCoroutine(AutoReturnVFX(assetKey, vfxInstance, releaseTime));
        }
        
        return vfxInstance;
    }
    
    /// <summary>
    /// Play VFX at specified position (Async)
    /// </summary>
    public async Task<GameObject> PlayVFXAsync(AssetKey assetKey, Vector3 position, bool autoRelease = true, float? customAutoReleaseTime = null)
    {
        return await PlayVFXAsync(assetKey, position, Quaternion.identity, autoRelease, customAutoReleaseTime);
    }
    
    /// <summary>
    /// Play VFX attached to a transform (Async)
    /// </summary>
    public async Task<GameObject> PlayVFXAsync(AssetKey assetKey, Transform parent, bool autoRelease = true, float? customAutoReleaseTime = null)
    {
        var vfxInstance = await PlayVFXAsync(assetKey, parent.position, parent.rotation, autoRelease, customAutoReleaseTime);
        if (vfxInstance != null)
        {
            vfxInstance.transform.SetParent(parent);
        }
        return vfxInstance;
    }
    
    #endregion
    
    #region Public API - Synchronous Methods
    
    /// <summary>
    /// Play VFX at specified position with rotation (Sync - requires pre-loaded pool)
    /// </summary>
    public GameObject PlayVFX(AssetKey assetKey, Vector3 position, Quaternion rotation, bool autoRelease = true, float? customAutoReleaseTime = null)
    {
        if (poolManager == null)
        {
            FDebug.LogError("VFXController: PoolManager not available!");
            return null;
        }
        
        if (!assetKey.IsVFX())
        {
            FDebug.LogError($"VFXController: Asset key {assetKey} is not a VFX asset!");
            return null;
        }
        
        // Try to get VFX from pool synchronously
        var vfxInstance = poolManager.Get(assetKey);
        if (vfxInstance == null)
        {
            FDebug.LogWarning($"VFXController: Pool for {assetKey} not ready. Consider using PlayVFXAsync or preloading pools.");
            return null;
        }
        
        // Setup position and rotation
        vfxInstance.transform.position = position;
        vfxInstance.transform.rotation = rotation;
        
        // Setup auto-release if enabled
        if (autoRelease)
        {
            float releaseTime = customAutoReleaseTime ?? GetAutoReleaseTime(assetKey);
            StartCoroutine(AutoReturnVFX(assetKey, vfxInstance, releaseTime));
        }
        
        return vfxInstance;
    }
    
    /// <summary>
    /// Play VFX at specified position (Sync)
    /// </summary>
    public GameObject PlayVFX(AssetKey assetKey, Vector3 position, bool autoRelease = true, float? customAutoReleaseTime = null)
    {
        return PlayVFX(assetKey, position, Quaternion.identity, autoRelease, customAutoReleaseTime);
    }
    
    /// <summary>
    /// Play VFX attached to a transform (Sync)
    /// </summary>
    public GameObject PlayVFX(AssetKey assetKey, Transform parent, bool autoRelease = true, float? customAutoReleaseTime = null)
    {
        var vfxInstance = PlayVFX(assetKey, parent.position, parent.rotation, autoRelease, customAutoReleaseTime);
        if (vfxInstance != null)
        {
            vfxInstance.transform.SetParent(parent);
        }
        return vfxInstance;
    }
    
    #endregion
    
    #region Pool Management
    
    /// <summary>
    /// Manually return a VFX instance to the pool
    /// </summary>
    public void StopVFX(AssetKey assetKey, GameObject vfxInstance)
    {
        if (poolManager != null)
        {
            poolManager.Return(assetKey, vfxInstance);
        }
    }
    
    /// <summary>
    /// Stop all active VFX of a specific type
    /// </summary>
    public void StopAllVFX(AssetKey assetKey)
    {
        if (poolManager != null)
        {
            poolManager.ReturnAllActive(assetKey);
        }
    }
    
    /// <summary>
    /// Stop all VFX currently active
    /// </summary>
    public void StopAllVFX()
    {
        // For now, we'll iterate through all VFX keys and return them
        // This is because PoolManager doesn't have a ReturnAllActiveByCategory method
        var vfxKeys = System.Enum.GetValues(typeof(AssetKey));
        foreach (AssetKey key in vfxKeys)
        {
            if (key.IsVFX())
            {
                poolManager?.ReturnAllActive(key);
            }
        }
    }
    
    /// <summary>
    /// Check if a VFX pool is ready for synchronous use
    /// </summary>
    public bool IsVFXReady(AssetKey assetKey)
    {
        return poolManager?.IsPoolReady(assetKey) ?? false;
    }
    
    /// <summary>
    /// Get pool status for a VFX
    /// </summary>
    public (int active, int available) GetVFXPoolStatus(AssetKey assetKey)
    {
        // PoolManager doesn't have GetPoolStatus method, so we'll return a basic status
        if (poolManager?.IsPoolReady(assetKey) == true)
        {
            // We can't get exact numbers without the API, so return basic info
            return (0, 1); // Indicate pool is available
        }
        return (0, 0);
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Auto-return VFX to pool after delay
    /// </summary>
    private IEnumerator AutoReturnVFX(AssetKey assetKey, GameObject vfxInstance, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (vfxInstance != null && vfxInstance.activeInHierarchy)
        {
            StopVFX(assetKey, vfxInstance);
        }
    }
    
    /// <summary>
    /// Get total count of active VFX
    /// </summary>
    public int GetActiveVFXCount()
    {
        return totalActiveVFX;
    }
    
    /// <summary>
    /// Get total count of all VFX pools
    /// </summary>
    public int GetTotalPoolCount()
    {
        // Count VFX pools manually since PoolManager doesn't have GetPoolCount by category
        int count = 0;
        var vfxKeys = System.Enum.GetValues(typeof(AssetKey));
        foreach (AssetKey key in vfxKeys)
        {
            if (key.IsVFX() && poolManager?.IsPoolReady(key) == true)
            {
                count++;
            }
        }
        return count;
    }
    
    #endregion

    #region Editor Utilities
    
    #if UNITY_EDITOR
    
    [ContextMenu("Validate VFX Setup")]
    private void ValidateVFXSetup()
    {
        var vfxKeys = System.Enum.GetValues(typeof(AssetKey));
        int vfxCount = 0;
        
        foreach (AssetKey key in vfxKeys)
        {
            if (key.IsVFX())
            {
                vfxCount++;
                FDebug.Log($"VFX Asset Key: {key}");
            }
        }
        
        FDebug.Log($"Total VFX keys found: {vfxCount}");
    }
    
    #endif
    
    #endregion
}


