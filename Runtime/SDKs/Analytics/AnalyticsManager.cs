using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Analytics;

namespace Analytics
{
    /// <summary>
    /// Central manager for all analytics services
    /// Supports multiple analytics platforms: Amplitude, Firebase, and Facebook
    /// Initialized by GameInitFlow during app startup
    /// </summary>
    public class AnalyticsManager : MonoBehaviour
    {
        private List<IAnalyticsService> analyticsServices = new List<IAnalyticsService>();
        private bool isInitialized = false;
        private PlayerDataManager playerDataManager;
        
        /// <summary>
        /// Backward compatibility: Get instance from ServiceLocator
        /// Use ServiceLocator.Resolve<AnalyticsManager>() or AnalyticsHelper.Instance for new code
        /// </summary>
        public static AnalyticsManager Instance => ServiceLocator.TryResolve<AnalyticsManager>();
        
        /// <summary>
        /// Initialize analytics manager and all enabled services
        /// Called by GameInitFlow
        /// </summary>
        public async UniTask InitializeAsync()
        {
            if (isInitialized)
            {
                Debug.LogWarning("[AnalyticsManager] Already initialized");
                return;
            }
                
            Debug.Log("[AnalyticsManager] Initializing analytics services...");
            
            // Add Amplitude service if enabled
            if (AnalyticsSettings.Instance.EnableAmplitude)
            {
                AddService(new AmplitudeAnalyticsService());
            }
            
            // Add Firebase service if enabled
            if (AnalyticsSettings.Instance.EnableFirebase)
            {
                AddService(new FirebaseAnalyticsService());
            }
            
            // Add Facebook service if enabled
            if (AnalyticsSettings.Instance.EnableFacebook)
            {
                AddService(new FacebookAnalyticsService());
            }
            
            // Initialize all services
            foreach (var service in analyticsServices)
            {
                service.Initialize();
            }
            
            isInitialized = true;
            Debug.Log($"[AnalyticsManager] Initialized {analyticsServices.Count} analytics services");
            
            // Wait a frame to ensure other services are registered
            await UniTask.Yield();
            
            // Cache PlayerDataManager reference
            playerDataManager = ServiceLocator.TryResolve<PlayerDataManager>();
            
            if (playerDataManager == null)
            {
                Debug.LogWarning("[AnalyticsManager] PlayerDataManager not available at initialization");
            }
        }
        
        public void AddService(IAnalyticsService service)
        {
            if (analyticsServices.Any(s => s.ServiceName == service.ServiceName))
            {
                Debug.LogWarning($"[AnalyticsManager] Service {service.ServiceName} already exists");
                return;
            }
            
            analyticsServices.Add(service);
            Debug.Log($"[AnalyticsManager] Added service: {service.ServiceName}");
        }
        
        public void RemoveService(string serviceName)
        {
            var service = analyticsServices.FirstOrDefault(s => s.ServiceName == serviceName);
            if (service != null)
            {
                analyticsServices.Remove(service);
                Debug.Log($"[AnalyticsManager] Removed service: {serviceName}");
            }
        }
        
        public void TrackEvent(string eventName)
        {
            TrackEvent(eventName, null);
        }
        
        public void TrackEvent(string eventName, Dictionary<string, object> parameters)
        {
            // Add level index as default parameter if not already present
            var enrichedParameters = EnrichParametersWithDefaults(parameters);
            
            foreach (var service in analyticsServices)
            {
                service.TrackEvent(eventName, enrichedParameters);
            }
        }
        
        /// <summary>
        /// Enrich event parameters with default values like level index, retention day, coins, and lives
        /// </summary>
        private Dictionary<string, object> EnrichParametersWithDefaults(Dictionary<string, object> parameters)
        {
            var enrichedParams = parameters != null ? new Dictionary<string, object>(parameters) : new Dictionary<string, object>();
            
            // Try to resolve PlayerDataManager if not cached yet
            if (playerDataManager == null)
            {
                playerDataManager = ServiceLocator.TryResolve<PlayerDataManager>();
            }
            
            if (playerDataManager != null && playerDataManager.PlayerData != null)
            {
                // Add level index if not already present
                if (!enrichedParams.ContainsKey(GameAnalyticsEvents.PARAM_LEVEL_INDEX))
                {
                    enrichedParams[GameAnalyticsEvents.PARAM_LEVEL_INDEX] = playerDataManager.GetCurrentLevelIndex();
                }
                
                // Add retention day if not already present - this is now a default parameter for all events
                if (!enrichedParams.ContainsKey(GameAnalyticsEvents.PARAM_RETENTION_DAY))
                {
                    // Update retention on every login session and get current retention day
                    var retentionDay = playerDataManager.PlayerData.GetRetentionDay();
                    enrichedParams[GameAnalyticsEvents.PARAM_RETENTION_DAY] = retentionDay;
                }
                
                // Add coins if not already present - default parameter for all events
                if (!enrichedParams.ContainsKey(GameAnalyticsEvents.PARAM_COINS))
                {
                    enrichedParams[GameAnalyticsEvents.PARAM_COINS] = playerDataManager.GetCurrencyAmount(CurrencyType.Coin);
                }
                
                // Add lives if not already present - default parameter for all events
                if (!enrichedParams.ContainsKey(GameAnalyticsEvents.PARAM_LIVES))
                {
                    enrichedParams[GameAnalyticsEvents.PARAM_LIVES] = playerDataManager.GetCurrencyAmount(CurrencyType.Lives);
                }
            }
            
            return enrichedParams;
        }
        
        public void SetUserProperties(Dictionary<string, object> properties)
        {
            foreach (var service in analyticsServices)
            {
                service.SetUserProperties(properties);
            }
        }
        
        public void SetUserProperty(string key, object value)
        {
            foreach (var service in analyticsServices)
            {
                service.SetUserProperty(key, value);
            }
        }
        
        public void SetUserId(string userId)
        {
            foreach (var service in analyticsServices)
            {
                service.SetUserId(userId);
            }
        }
        
        public void FlushEvents()
        {
            foreach (var service in analyticsServices)
            {
                service.FlushEvents();
            }
        }
        
        public void SetServiceEnabled(string serviceName, bool enabled)
        {
            var service = analyticsServices.FirstOrDefault(s => s.ServiceName == serviceName);
            if (service != null)
            {
                service.SetEnabled(enabled);
            }
        }
        
        /// <summary>
        /// Get the Facebook Analytics service for specific Facebook operations
        /// </summary>
        public FacebookAnalyticsService GetFacebookService()
        {
            return analyticsServices.OfType<FacebookAnalyticsService>().FirstOrDefault();
        }
        
        /// <summary>
        /// Log a purchase to Facebook Analytics for ads optimization
        /// </summary>
        public void LogFacebookPurchase(float amount, string currency = "USD", Dictionary<string, object> parameters = null)
        {
            var fbService = GetFacebookService();
            if (fbService != null)
            {
                fbService.LogPurchase(amount, currency, parameters);
            }
        }
        
        public List<string> GetActiveServices()
        {
            return analyticsServices.Where(s => s.IsEnabled).Select(s => s.ServiceName).ToList();
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                FlushEvents();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                FlushEvents();
            }
        }
        
        private void OnDestroy()
        {
            FlushEvents();
        }
    }
}
