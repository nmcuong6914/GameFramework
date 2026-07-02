using UnityEngine;
using System.Collections.Generic;
using Analytics;

/// <summary>
/// Simple test component to verify analytics events are working
/// Add this to a GameObject in your scene and use the context menu to test events
/// </summary>
public class AnalyticsTest : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private void Start()
    {
        Debug.Log("[AnalyticsTest] Analytics test component ready. Use context menu to test events.");
    }
    
    [ContextMenu("Test Win Event")]
    public void TestWinEvent()
    {
        Debug.Log("[AnalyticsTest] Testing level win event...");
        
        var winParams = new Dictionary<string, object>
        {
            { "level_index", 1 },
            { "score", 1500 },
            { "level_name", "Test_Level_1" },
            { "time_spent", 45.5f },
            { "rewards", "Coin:100" }
        };
        
        AnalyticsManager.Instance.TrackEvent("level_won", winParams);
        Debug.Log("[AnalyticsTest] Level win event sent!");
    }
    
    [ContextMenu("Test Lose Event")]
    public void TestLoseEvent()
    {
        Debug.Log("[AnalyticsTest] Testing level lose event...");
        
        var loseParams = new Dictionary<string, object>
        {
            { "level_index", 1 },
            { "fail_reason", "TimeExpired" },
            { "level_name", "Test_Level_1" },
            { "time_spent", 120.0f }
        };
        
        AnalyticsManager.Instance.TrackEvent("level_lost", loseParams);
        Debug.Log("[AnalyticsTest] Level lose event sent!");
    }
    
    [ContextMenu("Test Simple Event")]
    public void TestSimpleEvent()
    {
        Debug.Log("[AnalyticsTest] Testing simple event...");
        
        AnalyticsManager.Instance.TrackEvent("test_button_clicked");
        Debug.Log("[AnalyticsTest] Simple event sent!");
    }
    
    [ContextMenu("Show Active Services")]
    public void ShowActiveServices()
    {
        var activeServices = AnalyticsManager.Instance.GetActiveServices();
        Debug.Log($"[AnalyticsTest] Active analytics services: {string.Join(", ", activeServices)}");
    }
}
