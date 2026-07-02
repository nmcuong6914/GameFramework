using System.Collections.Generic;
using UnityEngine;
using AmplitudeNS;

namespace Analytics
{
    public class AmplitudeAnalyticsService : BaseAnalyticsService
    {
        public override string ServiceName => "Amplitude";
        
        protected override void InitializeService()
        {
            var apiKey = AnalyticsSettings.Instance.AmplitudeApiKey;
            
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("[Amplitude] API key is not set in AnalyticsSettings");
                return;
            }
            
#if AMPLITUDE_UNITY
            try
            {
                Amplitude amplitude = Amplitude.getInstance();
                amplitude.logging = AnalyticsSettings.Instance.EnableDebugLogs;
                amplitude.init(apiKey);
                
                // Set some default properties
                amplitude.setUserProperty("platform", Application.platform.ToString());
                amplitude.setUserProperty("app_version", Application.version);
                amplitude.setUserProperty("unity_version", Application.unityVersion);
                
                Debug.Log("[Amplitude] Successfully initialized");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Amplitude] Failed to initialize: {e.Message}");
            }
#else
            Debug.LogWarning("[Amplitude] Amplitude Unity SDK not found. Please import the Amplitude Unity package.");
#endif
        }
        
        protected override void TrackEventInternal(string eventName, Dictionary<string, object> parameters)
        {
#if AMPLITUDE_UNITY
            try
            {
                Amplitude amplitude = Amplitude.getInstance();
                
                if (parameters != null && parameters.Count > 0)
                {
                    amplitude.logEvent(eventName, parameters);
                }
                else
                {
                    amplitude.logEvent(eventName);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Amplitude] Failed to track event '{eventName}': {e.Message}");
            }
#endif
        }
        
        protected override void SetUserPropertiesInternal(Dictionary<string, object> properties)
        {
#if AMPLITUDE_UNITY
            try
            {
                Amplitude amplitude = Amplitude.getInstance();
                foreach (var property in properties)
                {
                    SetUserPropertyValue(amplitude, property.Key, property.Value);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Amplitude] Failed to set user properties: {e.Message}");
            }
#endif
        }
        
        protected override void SetUserPropertyInternal(string key, object value)
        {
#if AMPLITUDE_UNITY
            try
            {
                Amplitude amplitude = Amplitude.getInstance();
                SetUserPropertyValue(amplitude, key, value);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Amplitude] Failed to set user property '{key}': {e.Message}");
            }
#endif
        }
        
        protected override void SetUserIdInternal(string userId)
        {
#if AMPLITUDE_UNITY
            try
            {
                Amplitude amplitude = Amplitude.getInstance();
                amplitude.setUserId(userId);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Amplitude] Failed to set user ID: {e.Message}");
            }
#endif
        }
        
        protected override void FlushEventsInternal()
        {
#if AMPLITUDE_UNITY
            try
            {
                Amplitude amplitude = Amplitude.getInstance();
                amplitude.uploadEvents();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Amplitude] Failed to flush events: {e.Message}");
            }
#endif
        }
        
#if AMPLITUDE_UNITY
        private void SetUserPropertyValue(Amplitude amplitude, string key, object value)
        {
            // Convert object to appropriate type for Amplitude
            if (value is string stringValue)
            {
                amplitude.setUserProperty(key, stringValue);
            }
            else if (value is int intValue)
            {
                amplitude.setUserProperty(key, intValue);
            }
            else if (value is long longValue)
            {
                amplitude.setUserProperty(key, longValue);
            }
            else if (value is double doubleValue)
            {
                amplitude.setUserProperty(key, doubleValue);
            }
            else if (value is float floatValue)
            {
                amplitude.setUserProperty(key, (double)floatValue);
            }
            else if (value is bool boolValue)
            {
                amplitude.setUserProperty(key, boolValue);
            }
            else
            {
                // Convert to string as fallback
                amplitude.setUserProperty(key, value?.ToString() ?? "null");
            }
        }
#endif
    }
}
