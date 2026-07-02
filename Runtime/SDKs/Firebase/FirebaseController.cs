using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

/// <summary>
/// Central controller for Firebase initialization.
/// Manages Firebase app initialization and provides status for dependent services
/// (RemoteConfig, Analytics, etc.)
/// </summary>
public class FirebaseController : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;
    
    // Firebase state
    private FirebaseApp firebaseApp;
    private bool isInitialized = false;
    private DependencyStatus dependencyStatus = DependencyStatus.UnavailableOther;
    private CancellationTokenSource initializationCancellation;
    
    // Initialization promise for dependent services to await
    private UniTaskCompletionSource initializationCompletionSource;
    
    // Properties
    public bool IsInitialized => isInitialized;
    public bool IsAvailable => isInitialized && dependencyStatus == DependencyStatus.Available;
    public DependencyStatus DependencyStatus => dependencyStatus;
    public FirebaseApp App => firebaseApp;
    
    // Events
    public event Action<bool> OnInitializationComplete;
    public event Action<string> OnInitializationFailed;
    
    private void Awake()
    {
        initializationCompletionSource = new UniTaskCompletionSource();
    }
    
    /// <summary>
    /// Initialize Firebase app and check dependencies
    /// </summary>
    public async UniTask InitializeAsync()
    {
        if (isInitialized)
        {
            if (debugLogs)
            {
                Debug.Log("[FirebaseController] Already initialized");
            }
            return;
        }
        
        initializationCancellation = new CancellationTokenSource();
        
        if (debugLogs)
        {
            Debug.Log("[FirebaseController] Starting Firebase initialization...");
        }
        
        try
        {
            // Check and fix Firebase dependencies
            var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();
            dependencyStatus = await dependencyTask.AsUniTask()
                .AttachExternalCancellation(initializationCancellation?.Token ?? CancellationToken.None);
            
            if (dependencyStatus == DependencyStatus.Available)
            {
                // Get or create Firebase app instance
                firebaseApp = FirebaseApp.DefaultInstance;
                isInitialized = true;
                
                if (debugLogs)
                {
                    Debug.Log("[FirebaseController] Firebase initialized successfully");
                }
                
                // Complete the initialization task for awaiting services
                initializationCompletionSource.TrySetResult();
                
                // Notify listeners
                OnInitializationComplete?.Invoke(true);
            }
            else
            {
                string errorMessage = $"Firebase dependencies unavailable: {dependencyStatus}";
                Debug.LogError($"[FirebaseController] {errorMessage}");
                
                isInitialized = false;
                
                // Complete with failure but don't throw to allow graceful degradation
                initializationCompletionSource.TrySetResult();
                
                // Notify listeners
                OnInitializationComplete?.Invoke(false);
                OnInitializationFailed?.Invoke(errorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            if (debugLogs)
            {
                Debug.Log("[FirebaseController] Firebase initialization cancelled");
            }
            
            isInitialized = false;
            initializationCompletionSource.TrySetCanceled();
        }
        catch (Exception ex)
        {
            string errorMessage = $"Firebase initialization failed: {ex.Message}";
            Debug.LogError($"[FirebaseController] {errorMessage}");
            Debug.LogException(ex);
            
            isInitialized = false;
            dependencyStatus = DependencyStatus.UnavailableOther;
            
            // Complete with failure but don't throw
            initializationCompletionSource.TrySetResult();
            
            // Notify listeners
            OnInitializationComplete?.Invoke(false);
            OnInitializationFailed?.Invoke(errorMessage);
        }
        finally
        {
            initializationCancellation?.Dispose();
            initializationCancellation = null;
        }
    }
    
    /// <summary>
    /// Wait for Firebase initialization to complete.
    /// This is used by dependent services (RemoteConfig, Analytics) to ensure
    /// Firebase is ready before they attempt to use Firebase APIs.
    /// </summary>
    public async UniTask WaitForInitializationAsync()
    {
        if (isInitialized)
        {
            return;
        }
        
        if (debugLogs)
        {
            Debug.Log("[FirebaseController] Service waiting for Firebase initialization...");
        }
        
        await initializationCompletionSource.Task;
        
        if (debugLogs)
        {
            Debug.Log($"[FirebaseController] Firebase initialization wait complete. Available: {IsAvailable}");
        }
    }
    
    /// <summary>
    /// Check if Firebase is ready to use
    /// </summary>
    public bool IsReady()
    {
        return isInitialized && dependencyStatus == DependencyStatus.Available && firebaseApp != null;
    }
    
    /// <summary>
    /// Get a detailed status message
    /// </summary>
    public string GetStatusMessage()
    {
        if (!isInitialized)
        {
            return "Firebase not initialized";
        }
        
        switch (dependencyStatus)
        {
            case DependencyStatus.Available:
                return "Firebase ready";
            case DependencyStatus.UnavailableDisabled:
                return "Firebase disabled";
            case DependencyStatus.UnavailablePermission:
                return "Firebase permission required";
            case DependencyStatus.UnavailableUpdaterequired:
                return "Firebase update required";
            case DependencyStatus.UnavailableUpdating:
                return "Firebase updating";
            default:
                return $"Firebase unavailable: {dependencyStatus}";
        }
    }
    
    private void OnDestroy()
    {
        initializationCancellation?.Cancel();
        initializationCancellation?.Dispose();
        
        // Ensure completion source is completed to prevent memory leaks
        if (initializationCompletionSource != null && initializationCompletionSource.Task.Status == UniTaskStatus.Pending)
        {
            initializationCompletionSource.TrySetCanceled();
        }
    }
    
    #region Debug Methods
    
    [ContextMenu("Force Reinitialize")]
    private void ForceReinitialize()
    {
        isInitialized = false;
        initializationCompletionSource = new UniTaskCompletionSource();
        InitializeAsync().Forget();
    }
    
    [ContextMenu("Log Status")]
    private void LogStatus()
    {
        Debug.Log($"[FirebaseController] Status:");
        Debug.Log($"  - Initialized: {isInitialized}");
        Debug.Log($"  - Available: {IsAvailable}");
        Debug.Log($"  - Ready: {IsReady()}");
        Debug.Log($"  - Dependency Status: {dependencyStatus}");
        Debug.Log($"  - Status Message: {GetStatusMessage()}");
    }
    
    #endregion
}
