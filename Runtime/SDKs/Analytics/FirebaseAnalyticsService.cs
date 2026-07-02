using System.Collections.Generic;
using Firebase.Extensions;
using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks;
using System;

namespace Analytics
{
    public class FirebaseAnalyticsService : BaseAnalyticsService
    {
        private FirebaseController firebaseController;
        private bool isFirebaseInitialized = false;
        private Queue<System.Action> pendingEvents = new Queue<System.Action>();
        
        public override string ServiceName => "Firebase";
        
        protected override void InitializeService()
        {
#if FIREBASE_ANALYTICS
            // Get FirebaseController from ServiceLocator
            firebaseController = ServiceLocator.TryResolve<FirebaseController>();
            
            if (firebaseController == null)
            {
                Debug.LogWarning("[FirebaseAnalytics] FirebaseController not found in ServiceLocator. Analytics will be unavailable.");
                return;
            }
            
            // Wait for Firebase initialization
            WaitForFirebaseInitialization().Forget();
#else
            Debug.LogWarning("[Firebase] Firebase Analytics SDK not found. Please import the Firebase Unity package.");
#endif
        }
        
        private async UniTaskVoid WaitForFirebaseInitialization()
        {
            try
            {
                // Wait for Firebase to be ready
                await firebaseController.WaitForInitializationAsync();
                
                if (firebaseController.IsAvailable)
                {
                    // Set default properties
                    Firebase.Analytics.FirebaseAnalytics.SetUserProperty("platform", Application.platform.ToString());
                    Firebase.Analytics.FirebaseAnalytics.SetUserProperty("app_version", Application.version);
                    Firebase.Analytics.FirebaseAnalytics.SetUserProperty("unity_version", Application.unityVersion);
                    
                    // Enable/disable debug mode
                    Firebase.Analytics.FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                    
                    // Mark as initialized and process queued events
                    isFirebaseInitialized = true;
                    ProcessQueuedEvents();
                    
                    Debug.Log("[FirebaseAnalytics] Successfully initialized");
                }
                else
                {
                    Debug.LogWarning($"[FirebaseAnalytics] Firebase not available: {firebaseController.GetStatusMessage()}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseAnalytics] Initialization failed: {e.Message}");
            }
        }

        private void ProcessQueuedEvents()
        {
            int processedCount = pendingEvents.Count;
            while (pendingEvents.Count > 0)
            {
                var action = pendingEvents.Dequeue();
                try
                {
                    action.Invoke();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Firebase] Error processing queued event: {e.Message}");
                }
            }
            
            if (processedCount > 0)
            {
                Debug.Log($"[Firebase] Processed {processedCount} queued events");
            }
        }

        protected override void TrackEventInternal(string eventName, Dictionary<string, object> parameters)
        {
#if FIREBASE_ANALYTICS
            // If Firebase is not initialized yet, queue the event
            if (!isFirebaseInitialized)
            {
                pendingEvents.Enqueue(() => TrackEventInternal(eventName, parameters));
                Debug.Log($"[Firebase] Queued event '{eventName}' - Firebase not yet initialized");
                return;
            }

            try
            {
                if (parameters != null && parameters.Count > 0)
                {
                    var firebaseParameters = new List<Firebase.Analytics.Parameter>();

                    foreach (var param in parameters)
                    {
                        var value = param.Value;

                        // Convert parameters to Firebase parameter format
                        if (value is string stringValue)
                        {
                            firebaseParameters.Add(new Firebase.Analytics.Parameter(param.Key, stringValue));
                        }
                        else if (value is int intValue)
                        {
                            firebaseParameters.Add(new Firebase.Analytics.Parameter(param.Key, intValue));
                        }
                        else if (value is long longValue)
                        {
                            firebaseParameters.Add(new Firebase.Analytics.Parameter(param.Key, longValue));
                        }
                        else if (value is double doubleValue)
                        {
                            firebaseParameters.Add(new Firebase.Analytics.Parameter(param.Key, doubleValue));
                        }
                        else if (value is float floatValue)
                        {
                            firebaseParameters.Add(new Firebase.Analytics.Parameter(param.Key, floatValue));
                        }
                        else if (value is bool boolValue)
                        {
                            firebaseParameters.Add(new Firebase.Analytics.Parameter(param.Key, boolValue ? "true" : "false"));
                        }
                        else
                        {
                            firebaseParameters.Add(new Firebase.Analytics.Parameter(param.Key, value?.ToString() ?? "null"));
                        }
                    }

                    Firebase.Analytics.FirebaseAnalytics.LogEvent(eventName, firebaseParameters.ToArray());
                }
                else
                {
                    Firebase.Analytics.FirebaseAnalytics.LogEvent(eventName);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Firebase] Failed to track event '{eventName}': {e.Message}");
            }
#endif
        }
        
        protected override void SetUserPropertiesInternal(Dictionary<string, object> properties)
        {
#if FIREBASE_ANALYTICS
            // If Firebase is not initialized yet, queue the operation
            if (!isFirebaseInitialized)
            {
                pendingEvents.Enqueue(() => SetUserPropertiesInternal(properties));
                Debug.Log("[Firebase] Queued SetUserProperties - Firebase not yet initialized");
                return;
            }

            try
            {
                foreach (var property in properties)
                {
                    Firebase.Analytics.FirebaseAnalytics.SetUserProperty(property.Key, property.Value?.ToString());
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Firebase] Failed to set user properties: {e.Message}");
            }
#endif
        }
        
        protected override void SetUserPropertyInternal(string key, object value)
        {
#if FIREBASE_ANALYTICS
            // If Firebase is not initialized yet, queue the operation
            if (!isFirebaseInitialized)
            {
                pendingEvents.Enqueue(() => SetUserPropertyInternal(key, value));
                Debug.Log($"[Firebase] Queued SetUserProperty '{key}' - Firebase not yet initialized");
                return;
            }

            try
            {
                Firebase.Analytics.FirebaseAnalytics.SetUserProperty(key, value?.ToString());
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Firebase] Failed to set user property '{key}': {e.Message}");
            }
#endif
        }
        
        protected override void SetUserIdInternal(string userId)
        {
#if FIREBASE_ANALYTICS
            // If Firebase is not initialized yet, queue the operation
            if (!isFirebaseInitialized)
            {
                pendingEvents.Enqueue(() => SetUserIdInternal(userId));
                Debug.Log("[Firebase] Queued SetUserId - Firebase not yet initialized");
                return;
            }

            try
            {
                Firebase.Analytics.FirebaseAnalytics.SetUserId(userId);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Firebase] Failed to set user ID: {e.Message}");
            }
#endif
        }
        
        protected override void FlushEventsInternal()
        {
            // Firebase automatically handles flushing, but we can call this to ensure immediate sending
            // Note: Firebase doesn't have an explicit flush method like Amplitude
#if FIREBASE_ANALYTICS
            // Firebase automatically batches and sends events
            Debug.Log("[Firebase] Events will be automatically flushed by Firebase");
#endif
        }
    }
}
