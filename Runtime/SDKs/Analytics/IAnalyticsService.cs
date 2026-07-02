using System.Collections.Generic;

namespace Analytics
{
    public interface IAnalyticsService
    {
        /// <summary>
        /// Initialize the analytics service
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Track a simple event with just a name
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        void TrackEvent(string eventName);
        
        /// <summary>
        /// Track an event with parameters
        /// </summary>
        /// <param name="eventName">Name of the event</param>
        /// <param name="parameters">Event parameters</param>
        void TrackEvent(string eventName, Dictionary<string, object> parameters);
        
        /// <summary>
        /// Set user properties
        /// </summary>
        /// <param name="properties">User properties to set</param>
        void SetUserProperties(Dictionary<string, object> properties);
        
        /// <summary>
        /// Set a single user property
        /// </summary>
        /// <param name="key">Property key</param>
        /// <param name="value">Property value</param>
        void SetUserProperty(string key, object value);
        
        /// <summary>
        /// Set user ID for tracking
        /// </summary>
        /// <param name="userId">Unique user identifier</param>
        void SetUserId(string userId);
        
        /// <summary>
        /// Flush any pending events
        /// </summary>
        void FlushEvents();
        
        /// <summary>
        /// Get the service name for identification
        /// </summary>
        string ServiceName { get; }
        
        /// <summary>
        /// Check if the service is enabled
        /// </summary>
        bool IsEnabled { get; }
        
        /// <summary>
        /// Enable or disable the service
        /// </summary>
        void SetEnabled(bool enabled);
    }
}
