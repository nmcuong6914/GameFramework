using UnityEngine;
using UnityEngine.SceneManagement;
using BlockSort.Ads;
using Cysharp.Threading.Tasks;

/// <summary>
/// GameInitFlow handles initialization of core/common services that are used throughout the application.
/// This component persists across scenes and automatically transitions to the game scene after initialization.
/// Gameplay-specific services (GameUI, BoosterManager, FreezeTimeManager) are now initialized 
/// in GameplayManager to maintain better separation of concerns.
/// </summary>
public class GameInitFlow : MonoBehaviour
{
    [Header("Scene Management")]
    [SerializeField] private string homeSceneName = "HomeScene";
    [SerializeField] private string gameplaySceneName = "GameplayScene";
    [SerializeField] private int levelThresholdForDirectPlay = 5; // If player level <= this, go directly to gameplay
    
    private static GameInitFlow instance;
    
    public static GameInitFlow Instance => instance;
    [Header("Core Services")]
    [SerializeField] private AssetManager assetManager;
    [SerializeField] private PoolManager poolManager;
    [SerializeField] private VFXController vFXController;
    [SerializeField] private PlayerDataManager playerDataManager;
    [SerializeField] private LivesManager livesManager;
    [SerializeField] private NotificationService notificationService;
    [SerializeField] private GameNotificationManager gameNotificationManager;
    [SerializeField] private PopupManager popupManager;
    [SerializeField] private SoundManager soundManager;
    [SerializeField] private GameAudioBridge gameAudioBridge;
    [SerializeField] private RateAppManager rateAppManager;
    [SerializeField] private FirebaseController firebaseController;
    [SerializeField] private RemoteConfigService remoteConfigService;
    [SerializeField] private TransitionManager transitionManager;
    [SerializeField] private FacebookController facebookController;
    [SerializeField] private Analytics.AnalyticsManager analyticsManager;
    
    [Header("Configuration Data")]
    [SerializeField] private CurrencyConfigData currencyConfigData;
    [SerializeField] private ShopConfig shopConfig;
    [SerializeField] private MiscConfig miscConfig;
    
    [Header("Purchase System")]
    [SerializeField] private Purchase.PurchaseManager purchaseManager;
    
    [Header("Ads Configuration")]
    [SerializeField] private AdsConfig adsConfig;
    public bool IsInitializationComplete { get; private set; } = false;

    private void Awake()
    {
        // Implement singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SetTargetFPSBasedOnDevice();
            InitializeGameAsync().Forget();
        }
        else if (instance != this)
        {
            FDebug.Log("[GameInitFlow] Duplicate instance detected, destroying...");
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Set target FPS based on device RAM/strength
    /// Low-end devices (< 2GB RAM): 30 FPS
    /// Mid-range devices (2-4GB RAM): 60 FPS
    /// High-end devices (> 4GB RAM): 60 FPS (can be increased to 120 if needed)
    /// </summary>
    private void SetTargetFPSBasedOnDevice()
    {
        int targetFPS = 60; // Default
        
        // Get device system memory in MB
        int systemMemoryMB = SystemInfo.systemMemorySize;
        
        // Categorize device and set FPS accordingly
        if (systemMemoryMB < 2048) // Less than 2GB RAM - Low-end device
        {
            targetFPS = 30;
            FDebug.Log($"[GameInitFlow] Low-end device detected ({systemMemoryMB}MB RAM) - Setting FPS to {targetFPS}");
        }
        else if (systemMemoryMB < 4096) // 2-4GB RAM - Mid-range device
        {
            targetFPS = 45;
            FDebug.Log($"[GameInitFlow] Mid-range device detected ({systemMemoryMB}MB RAM) - Setting FPS to {targetFPS}");
        }
        else // 4GB+ RAM - High-end device
        {
            targetFPS = 60; // Can set to 120 for very high-end devices if needed
            FDebug.Log($"[GameInitFlow] High-end device detected ({systemMemoryMB}MB RAM) - Setting FPS to {targetFPS}");
        }
        
        Application.targetFrameRate = targetFPS;
        FDebug.Log($"[GameInitFlow] Target FPS set to {targetFPS} for device with {systemMemoryMB}MB RAM");
    }

    private async UniTask InitializeGameAsync()
    {
        RegisterCoreServices();
        
        // Start with dark screen immediately (no fade in)
        var transitionManager = ServiceLocator.TryResolve<TransitionManager>();
        if (transitionManager != null)
        {
            await transitionManager.FadeOutAsync(0f); // Start dark immediately
        }
        
        await InitializeAsyncServices();
        ServiceLocator.Resolve<SignalBus>().Fire(new GameInitializationCompleteSignal());
        IsInitializationComplete = true;
        gameAudioBridge.InitializeAsync().Forget();
        FDebug.Log("[GameInitFlow] Initialization complete");
        
        // Transition to appropriate scene based on player level
        await TransitionToGame();
    }

    private void RegisterCoreServices()
    {
        // Register GameInitFlow first
        ServiceLocator.Register(this);
        
        // Register basic services
        ServiceLocator.Register(new SignalBus());
        ServiceLocator.Register(new TimerManager(debugTimers: true));
        ServiceLocator.Register(new ActionViewManager());
        ServiceLocator.Register(new PreBoosterController());

        // Register MonoBehaviour services
        if (assetManager != null) ServiceLocator.Register(assetManager);
        if (poolManager != null) ServiceLocator.Register(poolManager);
        if (vFXController != null) ServiceLocator.Register(vFXController);
        if (popupManager != null) ServiceLocator.Register(popupManager);
        if (soundManager != null) ServiceLocator.Register(soundManager);
        if (rateAppManager != null) ServiceLocator.Register(rateAppManager);
        if (firebaseController != null) ServiceLocator.Register(firebaseController);
        if (remoteConfigService != null) ServiceLocator.Register(remoteConfigService);
        if (transitionManager != null) ServiceLocator.Register(transitionManager);
        if (purchaseManager != null) ServiceLocator.Register(purchaseManager);
        if (facebookController != null) ServiceLocator.Register(facebookController);
        if (analyticsManager != null) ServiceLocator.Register(analyticsManager);
        
        // Register configuration data
        if (currencyConfigData != null) ServiceLocator.Register(currencyConfigData);
        if (shopConfig != null) ServiceLocator.Register(shopConfig);
        if (miscConfig != null) ServiceLocator.Register(miscConfig);
        
        // if (gameAudioBridge != null) ServiceLocator.Register(gameAudioBridge); // TODO: Uncomment after Unity compiles GameAudioBridge
    }

    private async UniTask InitializeAsyncServices()
    {
        // Initialize Firebase first - all Firebase services depend on this
        if (firebaseController != null)
        {
            await firebaseController.InitializeAsync();
            FDebug.Log($"[GameInitFlow] Firebase initialization complete: {firebaseController.GetStatusMessage()}");
        }
        
        // Initialize Facebook SDK (non-blocking, early initialization)
        if (facebookController != null)
        {
            facebookController.InitializeAsync().Forget();
        }

        // Initialize Analytics Manager (will wait for Firebase internally)
        if (analyticsManager != null)
        {
            await analyticsManager.InitializeAsync();
        }

        // Initialize RemoteConfigService (will wait for Firebase internally)
        if (remoteConfigService != null)
        {
            await remoteConfigService.InitializeAsync();

            // Check for version updates after remote config is loaded
            CheckForVersionUpdate();
        }

        // Initialize PlayerDataManager 
        if (playerDataManager != null)
        {
            ServiceLocator.Register(playerDataManager);
            await playerDataManager.InitializeAsync();
        }

        // Initialize dependent services
        if (livesManager != null)
        {
            ServiceLocator.Register(livesManager);
            await livesManager.InitializeLivesSystem();
        }
        
        // Initialize notification service (depends on LivesManager and PlayerDataManager)
        if (notificationService != null)
        {
            ServiceLocator.Register(notificationService);
            notificationService.Initialize();
        }
        
        // Initialize game notification manager (depends on NotificationService, LivesManager, PlayerDataManager)
        if (gameNotificationManager != null)
        {
            ServiceLocator.Register(gameNotificationManager);
            await gameNotificationManager.InitializeAsync();
        }

        // Initialize sound manager
        if (soundManager != null)
        {
            await soundManager.InitializeAsync();
        }

        // Initialize RateAppManager (depends on PlayerDataManager and SignalBus)
        // Note: RateAppManager doesn't need explicit initialization as it auto-initializes in Start()
        // but we ensure it's registered here for service discovery

        // Initialize game audio bridge (after sound manager)
        // TODO: Uncomment after Unity compiles GameAudioBridge
        // if (gameAudioBridge != null)
        // {
        //     await gameAudioBridge.InitializeAsync();
        // }

        // Initialize ads (non-blocking)
        if (adsConfig != null)
        {
            var adsManager = new AdsManager();
            ServiceLocator.Register(adsManager);
            adsManager.Initialize(adsConfig).Forget();
        }

        // Initialize Purchase Manager (depends on remote config for shop configuration)
        if (purchaseManager != null)
        {
            purchaseManager.InitializeAsync().Forget();
            FDebug.Log("[GameInitFlow] Purchase Manager initialization started");
        }

    }
    
    /// <summary>
    /// Check if the current app version meets the minimum required version from remote config
    /// Shows update popup if version is outdated
    /// </summary>
    private void CheckForVersionUpdate()
    {
        if (remoteConfigService == null || !remoteConfigService.IsInitialized)
        {
            FDebug.LogWarning("[GameInitFlow] RemoteConfigService not available for version check");
            return;
        }
        
        try
        {
            if (remoteConfigService.IsVersionOutdated())
            {
                var configData = remoteConfigService.ConfigData;
                if (configData != null)
                {
                    string currentVersion = Application.version;
                    string requiredVersion = configData.versionCode;
                    string updateMessage = configData.forceUpdateMessage;
                    
                    FDebug.Log($"[GameInitFlow] Version update required - Current: {currentVersion}, Required: {requiredVersion}");
                    
                    // Show version update popup using PopupUtility
                    PopupUtility.ShowVersionUpdatePopup(
                        currentVersion: currentVersion,
                        requiredVersion: requiredVersion,
                        updateMessage: updateMessage,
                        onUpdateClicked: () =>
                        {
                            Application.OpenURL("https://play.google.com/store/apps/details?id=com.PuzzleGameIndie.BlockJamColor");
                            // The popup will handle opening the app store
                        },
                        onForceClose: null // null means this is a mandatory update
                    );
                }
            }
            else
            {
                FDebug.Log($"[GameInitFlow] App version is up to date");
            }
        }
        catch (System.Exception ex)
        {
            FDebug.LogError($"[GameInitFlow] Error checking for version update: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Transition from startup scene to the appropriate scene based on player level
    /// If player level <= levelThresholdForDirectPlay, go to gameplay scene
    /// Otherwise, go to home scene
    /// Uses immediate fade in then loads the target scene
    /// </summary>
    private async UniTask TransitionToGame()
    {
        // Determine target scene based on player level
        string targetSceneName;
        var playerDataManager = ServiceLocator.TryResolve<PlayerDataManager>();
        
        if (playerDataManager != null && playerDataManager.IsInitialized)
        {
            int currentLevel = playerDataManager.GetCurrentLevelIndex();
            
            if (currentLevel <= levelThresholdForDirectPlay)
            {
                targetSceneName = gameplaySceneName;
                FDebug.Log($"[GameInitFlow] Player level {currentLevel} <= {levelThresholdForDirectPlay}, going directly to gameplay");
            }
            else
            {
                targetSceneName = homeSceneName;
                FDebug.Log($"[GameInitFlow] Player level {currentLevel} > {levelThresholdForDirectPlay}, going to home scene");
            }
        }
        else
        {
            // Default to home scene if PlayerDataManager is not available
            targetSceneName = homeSceneName;
            FDebug.LogWarning("[GameInitFlow] PlayerDataManager not available, defaulting to home scene");
        }
        
        if (string.IsNullOrEmpty(targetSceneName))
        {
            FDebug.LogWarning("[GameInitFlow] Target scene name not specified, skipping scene transition");
            return;
        }
        
        try
        {
            FDebug.Log($"[GameInitFlow] Transitioning to scene: {targetSceneName}");
            
            // Get TransitionManager from ServiceLocator
            var transitionManager = ServiceLocator.TryResolve<TransitionManager>();
            if (transitionManager != null)
            {
                // Start with immediate fade in (reveal scene immediately)
                await transitionManager.FadeInAsync(0f); // Immediate fade in
                
                // Load the scene
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
                
                // Wait for scene to load
                while (!asyncLoad.isDone)
                {
                    await UniTask.NextFrame();
                }
                
                FDebug.Log($"[GameInitFlow] Scene loaded: {targetSceneName}");
                
                FDebug.Log($"[GameInitFlow] Successfully transitioned to scene: {targetSceneName}");
            }
            else
            {
                FDebug.LogWarning("[GameInitFlow] TransitionManager not available, using direct scene loading");
                
                // Fallback to direct scene loading
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
                
                while (!asyncLoad.isDone)
                {
                    await UniTask.NextFrame();
                }
                
                FDebug.Log($"[GameInitFlow] Successfully loaded scene: {targetSceneName}");
            }
        }
        catch (System.Exception ex)
        {
            FDebug.LogError($"[GameInitFlow] Failed to load scene '{targetSceneName}': {ex.Message}");
        }
    }
    

    
    /// <summary>
    /// Check if initialization is complete and services are ready
    /// </summary>
    /// <returns>True if all services are initialized and ready</returns>
    public bool AreServicesReady()
    {
        return IsInitializationComplete;
    }
    
    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
