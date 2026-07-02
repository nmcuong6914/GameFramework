using System.Collections.Generic;
using UnityEngine;
using Facebook.Unity;

namespace Analytics
{
    /// <summary>
    /// Facebook Analytics service implementation
    /// Tracks events to Facebook Analytics for ads optimization and user behavior analysis
    /// </summary>
    public class FacebookAnalyticsService : BaseAnalyticsService
    {
        public override string ServiceName => "Facebook";

        protected override void InitializeService()
        {
            // Facebook SDK should already be initialized by FacebookController in GameInitFlow
            if (!FB.IsInitialized)
            {
                Debug.LogWarning("[FacebookAnalytics] Facebook SDK not initialized. Events will be queued.");
            }
            else
            {
                Debug.Log("[FacebookAnalytics] Service initialized successfully");
            }
        }

        protected override void TrackEventInternal(string eventName, Dictionary<string, object> parameters)
        {
            if (!FB.IsInitialized)
            {
                Debug.LogWarning($"[FacebookAnalytics] Cannot track event '{eventName}' - Facebook SDK not initialized");
                return;
            }

            try
            {
                if (parameters == null || parameters.Count == 0)
                {
                    // Log event without parameters
                    FB.LogAppEvent(eventName);
                }
                else
                {
                    // Convert parameters to Facebook format
                    var fbParameters = ConvertParametersToFacebookFormat(parameters);
                    FB.LogAppEvent(eventName, null, fbParameters);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FacebookAnalytics] Error tracking event '{eventName}': {ex.Message}");
            }
        }

        protected override void SetUserPropertiesInternal(Dictionary<string, object> properties)
        {
            // Facebook doesn't have a direct user properties API like Firebase
            // We can log this as a custom event instead
            if (properties != null && properties.Count > 0)
            {
                Debug.Log($"[FacebookAnalytics] User properties set (logged as event): {string.Join(", ", properties)}");
                FB.LogAppEvent("user_properties_updated", null, ConvertParametersToFacebookFormat(properties));
            }
        }

        protected override void SetUserPropertyInternal(string key, object value)
        {
            // Log as a custom event
            var parameters = new Dictionary<string, object> { { key, value } };
            Debug.Log($"[FacebookAnalytics] User property set: {key} = {value}");
            FB.LogAppEvent("user_property_updated", null, ConvertParametersToFacebookFormat(parameters));
        }

        protected override void SetUserIdInternal(string userId)
        {
            // Facebook uses its own user ID system
            // We can log this for tracking purposes
            Debug.Log($"[FacebookAnalytics] User ID set: {userId}");
            var parameters = new Dictionary<string, object> { { "user_id", userId } };
            FB.LogAppEvent("user_id_set", null, ConvertParametersToFacebookFormat(parameters));
        }

        protected override void FlushEventsInternal()
        {
            // Facebook SDK handles event flushing automatically
            Debug.Log("[FacebookAnalytics] Events flushed (automatic by SDK)");
        }

        /// <summary>
        /// Convert parameters dictionary to Facebook's parameter format
        /// Facebook expects Dictionary<string, object> where values are converted to appropriate types
        /// </summary>
        private Dictionary<string, object> ConvertParametersToFacebookFormat(Dictionary<string, object> parameters)
        {
            var fbParameters = new Dictionary<string, object>();
            
            foreach (var param in parameters)
            {
                var value = param.Value;
                
                // Facebook accepts: string, int, long, double, float, bool
                if (value == null)
                {
                    fbParameters[param.Key] = "null";
                }
                else if (value is string || value is int || value is long || 
                         value is double || value is float || value is bool)
                {
                    fbParameters[param.Key] = value;
                }
                else
                {
                    // Convert other types to string
                    fbParameters[param.Key] = value.ToString();
                }
            }
            
            return fbParameters;
        }

        /// <summary>
        /// Log a purchase event to Facebook for ads optimization
        /// </summary>
        public void LogPurchase(float amount, string currency = "USD", Dictionary<string, object> parameters = null)
        {
            if (!FB.IsInitialized || !IsEnabled || !isInitialized)
            {
                Debug.LogWarning($"[FacebookAnalytics] Cannot log purchase - Service not ready");
                return;
            }

            try
            {
                if (parameters != null && parameters.Count > 0)
                {
                    var fbParameters = ConvertParametersToFacebookFormat(parameters);
                    FB.LogPurchase((decimal)amount, currency, fbParameters);
                }
                else
                {
                    FB.LogPurchase((decimal)amount, currency);
                }
                
                Debug.Log($"[FacebookAnalytics] Purchase logged: {amount} {currency}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FacebookAnalytics] Error logging purchase: {ex.Message}");
            }
        }
    }
}
