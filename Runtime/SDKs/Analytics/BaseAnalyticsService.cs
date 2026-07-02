using System.Collections.Generic;
using UnityEngine;

namespace Analytics
{
    public abstract class BaseAnalyticsService : IAnalyticsService
    {
        protected bool isInitialized = false;
        protected bool isEnabled = true;
        
        public abstract string ServiceName { get; }
        public virtual bool IsEnabled => isEnabled && (!Application.isEditor || AnalyticsSettings.Instance.EnableInEditor);
        
        public virtual void Initialize()
        {
            if (isInitialized)
            {
                Debug.LogWarning($"[{ServiceName}] Service already initialized");
                return;
            }
            
            if (!IsEnabled)
            {
                Debug.Log($"[{ServiceName}] Service is disabled");
                return;
            }
            
            Debug.Log($"[{ServiceName}] Initializing analytics service");
            InitializeService();
            isInitialized = true;
        }
        
        public virtual void TrackEvent(string eventName)
        {
            TrackEvent(eventName, null);
        }
        
        public virtual void TrackEvent(string eventName, Dictionary<string, object> parameters)
        {
            if (!IsEnabled || !isInitialized)
            {
                LogEventDisabled(eventName, parameters);
                return;
            }
            
            LogEvent(eventName, parameters);
            TrackEventInternal(eventName, parameters);
        }
        
        public virtual void SetUserProperties(Dictionary<string, object> properties)
        {
            if (!IsEnabled || !isInitialized)
            {
                Debug.Log($"[{ServiceName}] SetUserProperties disabled: {string.Join(", ", properties)}");
                return;
            }
            
            SetUserPropertiesInternal(properties);
        }
        
        public virtual void SetUserProperty(string key, object value)
        {
            if (!IsEnabled || !isInitialized)
            {
                Debug.Log($"[{ServiceName}] SetUserProperty disabled: {key} = {value}");
                return;
            }
            
            SetUserPropertyInternal(key, value);
        }
        
        public virtual void SetUserId(string userId)
        {
            if (!IsEnabled || !isInitialized)
            {
                Debug.Log($"[{ServiceName}] SetUserId disabled: {userId}");
                return;
            }
            
            SetUserIdInternal(userId);
        }
        
        public virtual void FlushEvents()
        {
            if (!IsEnabled || !isInitialized)
                return;
                
            FlushEventsInternal();
        }
        
        public virtual void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
            Debug.Log($"[{ServiceName}] Service {(enabled ? "enabled" : "disabled")}");
        }
        
        // Abstract methods to be implemented by concrete services
        protected abstract void InitializeService();
        protected abstract void TrackEventInternal(string eventName, Dictionary<string, object> parameters);
        protected abstract void SetUserPropertiesInternal(Dictionary<string, object> properties);
        protected abstract void SetUserPropertyInternal(string key, object value);
        protected abstract void SetUserIdInternal(string userId);
        protected abstract void FlushEventsInternal();
        
        private void LogEvent(string eventName, Dictionary<string, object> parameters)
        {
            if (AnalyticsSettings.Instance.EnableDebugLogs)
            {
                var paramString = parameters != null ? string.Join(", ", parameters) : "no parameters";
                Debug.Log($"[{ServiceName}] Event: {eventName} | {paramString}");
            }
        }
        
        private void LogEventDisabled(string eventName, Dictionary<string, object> parameters)
        {
            if (AnalyticsSettings.Instance.EnableDebugLogs)
            {
                var paramString = parameters != null ? string.Join(", ", parameters) : "no parameters";
                Debug.Log($"[{ServiceName}] Event DISABLED: {eventName} | {paramString}");
            }
        }
    }
}
