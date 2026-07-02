using UnityEngine;
using Facebook.Unity;
using Cysharp.Threading.Tasks;

public class FacebookController : MonoBehaviour
{
    private bool isInitialized = false;

    /// <summary>
    /// Initialize Facebook SDK
    /// Called by GameInitFlow during app initialization
    /// </summary>
    public async UniTask InitializeAsync()
    {
        if (isInitialized)
        {
            Debug.Log("[FacebookController] Already initialized");
            return;
        }

        if (!FB.IsInitialized)
        {
            // Initialize the Facebook SDK
            FB.Init(InitCallback, OnHideUnity);
            
            // Wait for initialization to complete
            await UniTask.WaitUntil(() => isInitialized);
        }
        else
        {
            // Already initialized, signal an app activation App Event
            FB.ActivateApp();
            isInitialized = true;
        }
        
        Debug.Log("[FacebookController] Initialization complete");
    }

    private void InitCallback()
    {
        if (FB.IsInitialized)
        {
            // Signal an app activation App Event
            FB.ActivateApp();
            isInitialized = true;
            Debug.Log("[FacebookController] Facebook SDK initialized successfully");
        }
        else
        {
            Debug.LogError("[FacebookController] Failed to Initialize the Facebook SDK");
            isInitialized = true; // Set to true anyway to not block initialization flow
        }
    }

    private void OnHideUnity(bool isGameShown)
    {
        if (!isGameShown)
        {
            // Pause the game - we will need to hide
            Time.timeScale = 0;
        }
        else
        {
            // Resume the game - we're getting focus again
            Time.timeScale = 1;
        }
    }

}