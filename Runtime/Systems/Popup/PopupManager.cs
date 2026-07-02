using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;

public class PopupManager : MonoBehaviour
{
    private readonly Stack<BasePopup> popupStack = new Stack<BasePopup>();
    private readonly Dictionary<BasePopup, AsyncOperationHandle<GameObject>> popupHandles = new Dictionary<BasePopup, AsyncOperationHandle<GameObject>>();
    private readonly HashSet<BasePopup> transitioningPopups = new HashSet<BasePopup>();
    private bool isOpeningPopup = false;
    private bool isInitialized = false;

    [SerializeField] private Transform popupRoot;
    [SerializeField] private Transform popupRootPreapare;
    [SerializeField] private PopupConfig popupConfig;

    // Events
    public event Action<BasePopup> OnPopupOpened;
    public event Action<BasePopup> OnPopupClosed;
    public event Action OnAllPopupsClosed;

    // Property to check if initialized
    public bool IsInitialized => isInitialized;

    void Awake()
    {
        // Ensure this persists across scenes
        DontDestroyOnLoad(gameObject);
        
        // Initialize popup root if needed
        if (popupRoot == null)
        {
            var go = new GameObject("PopupRoot");
            popupRoot = go.transform;
            popupRoot.SetParent(transform);
        }
        
        // Register PopupConfig to ServiceLocator if available
        if (popupConfig != null)
        {
            ServiceLocator.Register(popupConfig);
        }
        else
        {
            Debug.LogWarning("PopupManager: PopupConfig not assigned!");
        }
    }

    void Start()
    {
        // Subscribe to popup signals using your SignalBus
        var signalBus = ServiceLocator.TryResolve<SignalBus>();
        if (signalBus != null)
        {
            signalBus.Subscribe<PopupSignal>(OnPopupSignalReceived);
            isInitialized = true;
        }
        else
        {
            Debug.LogWarning("SignalBus not found in ServiceLocator. Popup signals will not work.");
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from signals using your SignalBus
        var signalBus = ServiceLocator.TryResolve<SignalBus>();
        if (signalBus != null)
        {
            signalBus.Unsubscribe<PopupSignal>(OnPopupSignalReceived);
        }
        
        // Clean up any remaining handles
        foreach (var handle in popupHandles.Values)
        {
            if (handle.IsValid())
            {
                Addressables.ReleaseInstance(handle);
            }
        }
        popupHandles.Clear();
        
        // Unregister from ServiceLocator
        ServiceLocator.Unregister<PopupManager>();
        ServiceLocator.Unregister<PopupConfig>();
    }
    
    private async void OnPopupSignalReceived(PopupSignal signal)
    {
        if (popupConfig == null)
        {
            Debug.LogError("PopupConfig is not assigned to PopupManager");
            return;
        }
        
        var assetReference = popupConfig.GetPopupAssetReference(signal.PopupKey);
        if (assetReference == null)
        {
            Debug.LogError($"Popup key '{signal.PopupKey}' not found in PopupConfig");
            return;
        }
        
        // Simply use BasePopup since all popups inherit from it
        await OpenPopup<BasePopup>(assetReference, signal.ScreenData);
    }

    // Method to open popup by key
    public async Task<T> OpenPopupByKey<T>(PopupType popupKey, ScreenData screenData) where T : BasePopup
    {
        if (popupConfig == null)
        {
            Debug.LogError("PopupConfig is not assigned to PopupManager");
            return null;
        }
        
        var assetReference = popupConfig.GetPopupAssetReference(popupKey);
        if (assetReference == null)
        {
            Debug.LogError($"Popup key '{popupKey}' not found in PopupConfig");
            return null;
        }
        
        return await OpenPopup<T>(assetReference, screenData);
    }

    public async Task<T> OpenPopup<T>(AssetReference assetReference, ScreenData data) where T : BasePopup
    {
        if (isOpeningPopup)
        {
            Debug.LogWarning("Popup opening already in progress");
            return null;
        }
        
        isOpeningPopup = true;
        AsyncOperationHandle<GameObject> handle = default;

        handle = Addressables.InstantiateAsync(assetReference, popupRootPreapare);
        await handle.Task;
        
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"Failed to load popup from AssetReference: {handle.OperationException}");
            isOpeningPopup = false;
            return null;
        }

        var popup = handle.Result.GetComponent<T>();
        
        if (popup == null)
        {
            Debug.LogError($"Popup prefab from AssetReference does not have component {typeof(T)}");
            Addressables.ReleaseInstance(handle.Result);
            isOpeningPopup = false;
            return null;
        }

        // Initialize the popup
        popup.Init();
        popup.SetData(data);
        popup.transform.SetParent(popupRoot);
        await popup.TransitionIn();
        popupStack.Push(popup);
        popupHandles[popup] = handle;

        // Invoke the OnPopupOpened event
        OnPopupOpened?.Invoke(popup);

        isOpeningPopup = false;
        return popup;
    }

    public async Task CloseTopPopup(bool immediate = false)
    {
        if (popupStack.Count == 0) return;
        
        var popup = popupStack.Peek();
        
        // Check if this specific popup is already transitioning
        if (transitioningPopups.Contains(popup))
        {
            return;
        }
        
        transitioningPopups.Add(popup);
        popupStack.Pop();
        
        if (!immediate)
        {
            await popup.TransitionOut();
        }
        
        if (popupHandles.TryGetValue(popup, out var handle))
        {
            Addressables.ReleaseInstance(handle);
            popupHandles.Remove(popup);
        }
        else
        {
            Debug.LogWarning($"PopupManager: Popup handle not found for {popup.name}, using fallback release");
            Addressables.ReleaseInstance(popup.gameObject);
        }
        
        transitioningPopups.Remove(popup);
        OnPopupClosed?.Invoke(popup);
    }

    public async Task CloseAllPopups(bool immediate = false)
    {
        int initialCount = popupStack.Count;
        
        while (popupStack.Count > 0)
        {
            await CloseTopPopup(immediate);
        }

        // Invoke the OnAllPopupsClosed event if there were any popups closed
        if (initialCount > 0)
        {
            OnAllPopupsClosed?.Invoke();
        }
    }
    
    public async Task<bool> ClosePopup<T>(bool immediate = false) where T : BasePopup
    {
        var popup = GetPopup<T>();
        if (popup == null) 
        {
            return false;
        }
        
        // Check if this specific popup is already transitioning
        if (transitioningPopups.Contains(popup))
        {
            return false;
        }
        
        // Close all popups above this one first
        while (popupStack.Count > 0 && popupStack.Peek() != popup)
        {
            await CloseTopPopup(immediate);
        }
        
        if (popupStack.Count > 0 && popupStack.Peek() == popup)
        {
            await CloseTopPopup(immediate);
            return true;
        }
        
        return false;
    }
    
    public T GetPopup<T>() where T : BasePopup
    {
        foreach (var popup in popupStack)
        {
            if (popup is T typedPopup)
                return typedPopup;
        }
        return null;
    }
    
    public bool IsPopupOpen<T>() where T : BasePopup
    {
        return GetPopup<T>() != null;
    }
    
    public bool IsPopupOpen()
    {
        return popupStack.Count > 0;
    }
    
    public BasePopup GetTopPopup()
    {
        return popupStack.Count > 0 ? popupStack.Peek() : null;
    }
}
