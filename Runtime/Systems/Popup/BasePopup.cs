using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;
using Analytics;
using System.Collections.Generic;

public enum PopupAnimationType
{
    Fade,
    SwipeLeftToRight
}

public abstract class BasePopup : MonoBehaviour
{
    [Header("Base Popup Settings")]
    [SerializeField] protected Animator popupAnimator;
    [SerializeField] protected string transitionInAnimationName = "TransitionIn";
    [SerializeField] protected string transitionOutAnimationName = "TransitionOut";
    [SerializeField] protected CanvasGroup canvasGroup;
    
    [Header("Transition Animation Settings")]
    [SerializeField] protected bool useCustomTransitionAnimation = true;
    [SerializeField] protected PopupAnimationType animationType = PopupAnimationType.Fade;
    [SerializeField] protected RectTransform[] animatedElements;
    
    [Header("Background Tap Settings")]
    [SerializeField] protected bool enableBackgroundTapToClose = true;
    [SerializeField] protected Image dimmedBackground; // The dimmed background image that can be tapped to close
    
    [Header("Animation Duration Override (0 = use PopupConfig)")]
    [SerializeField] protected float overrideFadeInDuration = 0f;
    [SerializeField] protected float overrideFadeOutDuration = 0f;
    
    protected bool isInitialized = false;
    protected bool isTransitioning = false;
    private bool inputLocked = false;
    
    // Animation settings from PopupConfig
    private PopupConfig popupConfig;
    private float fadeInDuration;
    private float fadeOutDuration;
    private float elementAnimationDelay;
    private float elementAnimationDuration;
    private float elementStartScale;
    
    public bool IsOpen { get; protected set; }
    public bool IsTransitioning => isTransitioning;
    public bool IsInputLocked => inputLocked;
    
    public event Action<BasePopup> OnPopupTransitionInComplete;
    public event Action<BasePopup> OnPopupTransitionOutComplete;
    
    // Background tap handling
    private GraphicRaycaster graphicRaycaster;
    private EventSystem eventSystem;
    
    public abstract void SetData(ScreenData data);
    
    /// <summary>
    /// Initialize the popup - called by PopupManager after instantiation
    /// </summary>
    public virtual void Init()
    {
        // Ensure canvas group exists
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        // Load animation settings from PopupConfig
        LoadAnimationSettings();
        
        // Initialize background tap handling
        InitializeBackgroundTapHandling();
    }
    
    private void LoadAnimationSettings()
    {
        popupConfig = ServiceLocator.TryResolve<PopupConfig>();
        if (popupConfig != null)
        {
            fadeInDuration = overrideFadeInDuration > 0 ? overrideFadeInDuration : popupConfig.FadeInDuration;
            fadeOutDuration = overrideFadeOutDuration > 0 ? overrideFadeOutDuration : popupConfig.FadeOutDuration;
            elementAnimationDelay = popupConfig.ElementAnimationDelay;
            elementAnimationDuration = popupConfig.ElementAnimationDuration;
            elementStartScale = popupConfig.ElementStartScale;
        }
        else
        {
            // Fallback to default values if config not found
            fadeInDuration = overrideFadeInDuration > 0 ? overrideFadeInDuration : 0.25f;
            fadeOutDuration = overrideFadeOutDuration > 0 ? overrideFadeOutDuration : 0.2f;
            elementAnimationDelay = 0.1f;
            elementAnimationDuration = 0.3f;
            elementStartScale = 0.5f;
            Debug.LogWarning("BasePopup: PopupConfig not found in ServiceLocator, using default animation settings");
        }
    }
    
    private void InitializeBackgroundTapHandling()
    {
        if (!enableBackgroundTapToClose) return;
        
        // Get GraphicRaycaster from Canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
        }
        
        // Get EventSystem
        eventSystem = EventSystem.current;
    }
    
    private void Update()
    {
        if (enableBackgroundTapToClose && IsOpen && !inputLocked && Input.GetMouseButtonDown(0))
        {
            CheckForBackgroundTap();
        }
    }
    
    private void CheckForBackgroundTap()
    {
        if (graphicRaycaster == null || eventSystem == null) return;
        
        PointerEventData pointerEventData = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };
        
        List<RaycastResult> results = new List<RaycastResult>();
        graphicRaycaster.Raycast(pointerEventData, results);
        
        // Check if the tap hit the dimmed background
        if (results.Count > 0)
        {
            RaycastResult topHit = results[0];
            if (topHit.gameObject == dimmedBackground.gameObject)
            {
                OnBackgroundTapped();
            }
        }
    }
    
    protected virtual void OnBackgroundTapped()
    {
        // Override this method in derived classes if needed
        // Default behavior: close the popup
        OnBackgroundTapClose();
    }
    
    protected virtual async void OnBackgroundTapClose()
    {
        var popupManager = ServiceLocator.TryResolve<PopupManager>();
        if (popupManager != null)
        {
            await popupManager.CloseTopPopup();
        }
    }
    
    public virtual async UniTask TransitionIn()
    {
        if (isTransitioning) return;
        
        isTransitioning = true;
        inputLocked = true; // Lock input during transition
        
        // Track popup opened analytics event automatically
        TrackPopupOpenedEvent();
        
        // Fire signal for popup opened (for pausing game systems)
        FirePopupOpenedSignal();
        
        await DoTransitionIn();
        IsOpen = true;
        
        inputLocked = false; // Unlock input after transition complete
        OnPopupTransitionInComplete?.Invoke(this);
        isTransitioning = false;
    }
    
    public virtual async UniTask TransitionOut()
    {
        if (isTransitioning) return;
        
        isTransitioning = true;
        await DoTransitionOut();
        IsOpen = false;
        OnPopupTransitionOutComplete?.Invoke(this);
        isTransitioning = false;
        
        // Fire signal for popup closed (for resuming game systems)
        FirePopupClosedSignal();
    }

    protected virtual async UniTask DoTransitionIn()
    {
        // Set alpha to 0 at the very start to prevent flash
        canvasGroup.alpha = 0f;
        await UniTask.WaitForEndOfFrame();
        // Now activate for transition
        gameObject.SetActive(true);
        
        if (useCustomTransitionAnimation)
        {
            // Use custom fade-in and element animation system
            await DoCustomTransitionIn();
        }
        else
        {
            // Use animator if available
            if (popupAnimator != null && !string.IsNullOrEmpty(transitionInAnimationName))
            {
                popupAnimator.Play(transitionInAnimationName);
                await WaitForAnimationToComplete(transitionInAnimationName);
            }
            
            // Ensure final alpha is 1
            canvasGroup.alpha = 1f;
        }
    }
    
    /// <summary>
    /// Custom transition in animation with fade-in and sequential element animations
    /// </summary>
    protected virtual async UniTask DoCustomTransitionIn()
    {
        switch (animationType)
        {
            case PopupAnimationType.Fade:
                await DoFadeTransitionIn();
                break;
            case PopupAnimationType.SwipeLeftToRight:
                await DoSwipeTransitionIn();
                break;
            default:
                await DoFadeTransitionIn();
                break;
        }
    }

    /// <summary>
    /// Fade in animation with sequential element animations
    /// </summary>
    protected virtual async UniTask DoFadeTransitionIn()
    {
        // Step 1: Fade in the popup background
        await FadeInPopup();
        
        // Step 2: Animate elements sequentially
        if (animatedElements != null && animatedElements.Length > 0)
        {
            await AnimateElementsSequentially();
        }
    }

    /// <summary>
    /// Swipe left to right transition in animation
    /// </summary>
    protected virtual async UniTask DoSwipeTransitionIn()
    {
        // Get the RectTransform of the popup
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            // Fallback to fade if no RectTransform
            await DoFadeTransitionIn();
            return;
        }

        // Store original position
        Vector2 originalPosition = rectTransform.anchoredPosition;
        
        // Calculate start position (off-screen left)
        float screenWidth = rectTransform.rect.width;
        Vector2 startPosition = originalPosition - new Vector2(screenWidth + 100f, 0f);
        
        // Set initial state
        rectTransform.anchoredPosition = startPosition;
        canvasGroup.alpha = 1f;
        
        // Animate swipe in
        float elapsedTime = 0f;
        float swipeDuration = fadeInDuration; // Use same duration as fade
        
        while (elapsedTime < swipeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = elapsedTime / swipeDuration;
            
            // Ease out curve for smoother animation
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, originalPosition, easedT);
            await UniTask.Yield();
        }
        
        // Ensure final position is exact
        rectTransform.anchoredPosition = originalPosition;
        
        // Animate elements sequentially if any
        if (animatedElements != null && animatedElements.Length > 0)
        {
            await AnimateElementsSequentially();
        }
    }
    
    /// <summary>
    /// Fade in the popup canvas group
    /// </summary>
    protected virtual async UniTask FadeInPopup()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = elapsedTime / fadeInDuration;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            await UniTask.Yield();
        }
        
        canvasGroup.alpha = 1f;
    }
    
    /// <summary>
    /// Animate elements sequentially with scale and fade
    /// </summary>
    protected virtual async UniTask AnimateElementsSequentially()
    {
        // Dictionary to track temporary canvas groups we create
        Dictionary<RectTransform, CanvasGroup> temporaryCanvasGroups = new Dictionary<RectTransform, CanvasGroup>();
        
        // Initialize all elements (hide and scale down)
        foreach (var element in animatedElements)
        {
            if (element == null) continue;
            
            // Get or add canvas group
            CanvasGroup elementCanvasGroup = element.GetComponent<CanvasGroup>();
            if (elementCanvasGroup == null)
            {
                elementCanvasGroup = element.gameObject.AddComponent<CanvasGroup>();
                temporaryCanvasGroups[element] = elementCanvasGroup;
            }
            
            elementCanvasGroup.alpha = 0f;
            element.localScale = Vector3.one * elementStartScale;
        }
        
        // Animate each element sequentially
        for (int i = 0; i < animatedElements.Length; i++)
        {
            var element = animatedElements[i];
            if (element == null) continue;
            
            // Animate this element
            await AnimateSingleElement(element);
            
            // Wait before animating next element
            if (i < animatedElements.Length - 1)
            {
                await UniTask.Delay((int)(elementAnimationDelay * 1000), ignoreTimeScale: true);
            }
        }
        
        // Remove temporary canvas groups after animation completes
        foreach (var kvp in temporaryCanvasGroups)
        {
            if (kvp.Key != null && kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
    }
    
    /// <summary>
    /// Animate a single element with scale and fade
    /// </summary>
    protected virtual async UniTask AnimateSingleElement(RectTransform element)
    {
        CanvasGroup elementCanvasGroup = element.GetComponent<CanvasGroup>();
        if (elementCanvasGroup == null) return;
        
        float elapsedTime = 0f;
        Vector3 startScale = Vector3.one * elementStartScale;
        Vector3 endScale = Vector3.one;
        
        while (elapsedTime < elementAnimationDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = elapsedTime / elementAnimationDuration;
            
            // Ease out curve for smoother animation
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            
            elementCanvasGroup.alpha = Mathf.Lerp(0f, 1f, easedT);
            element.localScale = Vector3.Lerp(startScale, endScale, easedT);
            
            await UniTask.Yield();
        }
        
        elementCanvasGroup.alpha = 1f;
        element.localScale = endScale;
    }
    
    protected virtual async UniTask DoTransitionOut()
    {
        // Play animation if animator is available
        if (popupAnimator != null && !string.IsNullOrEmpty(transitionOutAnimationName))
        {
            popupAnimator.Play(transitionOutAnimationName);
            await WaitForAnimationToComplete(transitionOutAnimationName);
        }
        else if (useCustomTransitionAnimation)
        {
            // Use custom animation based on type
            switch (animationType)
            {
                case PopupAnimationType.Fade:
                    await DoFallbackFadeOut();
                    break;
                case PopupAnimationType.SwipeLeftToRight:
                    await DoSwipeTransitionOut();
                    break;
                default:
                    await DoFallbackFadeOut();
                    break;
            }
        }
        else
        {
            // Fallback to simple fade out animation
            await DoFallbackFadeOut();
        }
        
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Swipe right to left transition out animation (reverse of swipe in)
    /// </summary>
    protected virtual async UniTask DoSwipeTransitionOut()
    {
        // Get the RectTransform of the popup
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            // Fallback to fade if no RectTransform
            await DoFallbackFadeOut();
            return;
        }

        // Store original position
        Vector2 originalPosition = rectTransform.anchoredPosition;
        
        // Calculate end position (off-screen right)
        float screenWidth = rectTransform.rect.width;
        Vector2 endPosition = originalPosition + new Vector2(screenWidth + 100f, 0f);
        
        // Animate swipe out
        float elapsedTime = 0f;
        float swipeDuration = fadeOutDuration; // Use same duration as fade out
        
        while (elapsedTime < swipeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = elapsedTime / swipeDuration;
            
            // Ease in curve for smoother animation
            float easedT = Mathf.Pow(t, 2f);
            
            rectTransform.anchoredPosition = Vector2.Lerp(originalPosition, endPosition, easedT);
            await UniTask.Yield();
        }
        
        // Ensure final position is exact
        rectTransform.anchoredPosition = endPosition;
    }
    
    
    /// <summary>
    /// Fallback fade out animation that other popups can reuse
    /// </summary>
    protected virtual async UniTask DoFallbackFadeOut()
    {
        if (canvasGroup != null)
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
                await UniTask.Yield();
            }
            
            canvasGroup.alpha = 0f;
        }
        else
        {
            await UniTask.Yield();
        }
    }
    
    protected virtual async UniTask WaitForAnimationToComplete(string animationName)
    {
        if (popupAnimator == null) return;
        
        // Wait for animation to start
        while (!popupAnimator.GetCurrentAnimatorStateInfo(0).IsName(animationName))
        {
            await UniTask.Yield();
        }
        
        if (popupAnimator == null) return;
        
        // Wait for animation to complete
        while (popupAnimator != null && popupAnimator.GetCurrentAnimatorStateInfo(0).IsName(animationName) &&
               popupAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
        {
            await UniTask.Yield();
        }
    }
    
    /// <summary>
    /// Track popup opened analytics event automatically using signals
    /// </summary>
    private void TrackPopupOpenedEvent()
    {
        var signalBus = ServiceLocator.TryResolve<SignalBus>();
        if (signalBus != null)
        {
            // Fire analytics signal with popup name for tracking
            string popupName = GetType().Name;
            signalBus.Fire(new Analytics.PopupOpenedSignal(popupName));
        }
    }
    
    /// <summary>
    /// Fire signal when popup opens (for pausing game systems like timer bombs)
    /// </summary>
    private void FirePopupOpenedSignal()
    {
        var signalBus = ServiceLocator.TryResolve<SignalBus>();
        if (signalBus != null)
        {
            signalBus.Fire(new PopupOpenedSignal());
        }
    }
    
    /// <summary>
    /// Fire signal when popup closes (for resuming game systems like timer bombs)
    /// </summary>
    private void FirePopupClosedSignal()
    {
        var signalBus = ServiceLocator.TryResolve<SignalBus>();
        if (signalBus != null)
        {
            signalBus.Fire(new PopupClosedSignal());
        }
    }
    
    protected virtual void OnDestroy()
    {
        OnPopupTransitionInComplete = null;
        OnPopupTransitionOutComplete = null;
        
        // Ensure popup closed signal is fired if popup is destroyed unexpectedly
        FirePopupClosedSignal();
    }
}