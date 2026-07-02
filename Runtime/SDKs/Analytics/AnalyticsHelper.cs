using System.Collections.Generic;

namespace Analytics
{
    /// <summary>
    /// Static helper class for easy access to AnalyticsManager
    /// Provides backward compatibility with the old singleton pattern
    /// </summary>
    public static class AnalyticsHelper
    {
        private static AnalyticsManager cachedManager;
        
        /// <summary>
        /// Get the AnalyticsManager instance from ServiceLocator
        /// </summary>
        public static AnalyticsManager Instance
        {
            get
            {
                if (cachedManager == null)
                {
                    cachedManager = ServiceLocator.TryResolve<AnalyticsManager>();
                }
                return cachedManager;
            }
        }
        
        /// <summary>
        /// Track an event with just a name
        /// </summary>
        public static void TrackEvent(string eventName)
        {
            Instance?.TrackEvent(eventName);
        }
        
        /// <summary>
        /// Track an event with parameters
        /// </summary>
        public static void TrackEvent(string eventName, Dictionary<string, object> parameters)
        {
            Instance?.TrackEvent(eventName, parameters);
        }
        
        /// <summary>
        /// Set user properties
        /// </summary>
        public static void SetUserProperties(Dictionary<string, object> properties)
        {
            Instance?.SetUserProperties(properties);
        }
        
        /// <summary>
        /// Set a single user property
        /// </summary>
        public static void SetUserProperty(string key, object value)
        {
            Instance?.SetUserProperty(key, value);
        }
        
        /// <summary>
        /// Set user ID
        /// </summary>
        public static void SetUserId(string userId)
        {
            Instance?.SetUserId(userId);
        }
        
        /// <summary>
        /// Flush pending events
        /// </summary>
        public static void FlushEvents()
        {
            Instance?.FlushEvents();
        }
        
        /// <summary>
        /// Log a Facebook purchase event
        /// </summary>
        public static void LogFacebookPurchase(float amount, string currency = "USD", Dictionary<string, object> parameters = null)
        {
            Instance?.LogFacebookPurchase(amount, currency, parameters);
        }
        
        /// <summary>
        /// Get active analytics services
        /// </summary>
        public static List<string> GetActiveServices()
        {
            return Instance?.GetActiveServices() ?? new List<string>();
        }
    }
}
