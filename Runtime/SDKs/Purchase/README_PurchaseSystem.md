# Unity In-App Purchase System Documentation

## Overview

This Unity IAP (In-App Purchase) system provides a comprehensive, callback-based purchase management solution that integrates seamlessly with your existing shop system. The system handles purchase validation, history tracking, and provides callbacks for success/failure without managing any UI elements.

## Key Features

- ✅ **Unity IAP Integration**: Full Unity In-App Purchasing support
- ✅ **Callback System**: Event-driven architecture for purchase lifecycle
- ✅ **Purchase History**: Complete transaction tracking and analytics
- ✅ **Shop Integration**: Works with existing ShopConfig system and enhanced ShopPackage
- ✅ **Service Locator**: Integrated with game's dependency injection
- ✅ **No UI Coupling**: Pure business logic, no view handling
- ✅ **Cross-Platform**: iOS and Android support
- ✅ **Analytics Ready**: Built-in analytics event tracking
- ✅ **Simplified Configuration**: All settings in ShopPackage, no complex config files

## Architecture

### Core Components

1. **PurchaseManager**: Main service managing all purchase operations
2. **PurchaseEventManager**: Centralized event system for purchase callbacks
3. **PurchaseValidator**: Handles receipt validation and security
4. **PurchaseDataModels**: Data structures for transactions and product info
5. **PurchaseServiceExample**: Integration example for other systems

### Event Flow

```
Purchase Request → Unity IAP → Processing → Completion/Failure
      ↓              ↓            ↓              ↓
  OnStarted      Processing   Fulfillment   OnCompleted/OnFailed
```

## Setup Instructions

### 1. GameInitFlow Integration

The PurchaseManager is already integrated into the GameInitFlow system:

```csharp
[Header("Purchase System")]
[SerializeField] private Purchase.PurchaseManager purchaseManager;
```

### 2. Configure Shop Products

Configure your shop products in the existing ShopConfig system. Enhanced ShopPackage now includes:
- `iapProductType`: Unity IAP ProductType (Consumable, NonConsumable, Subscription)
- `enableDebugLogging`: Per-product debug logging
- `purchaseTimeoutSeconds`: Custom timeout per product

The PurchaseManager will automatically build the product catalog from IAP packages in your shop configuration.

### 3. Unity IAP Setup

1. Install Unity IAP package via Package Manager
2. Configure your products in Unity Cloud Build or use the IAP Catalog
3. Set up store-specific product IDs in your ShopPackage configurations

### 4. Platform Configuration

#### iOS (App Store)
- Configure products in App Store Connect
- Set up your app's bundle identifier
- Test with sandbox accounts

#### Android (Google Play)
- Configure products in Google Play Console
- Set up your app's package name
- Test with test accounts

## Usage Examples

### Basic Purchase Flow

```csharp
public class ShopUI : MonoBehaviour
{
    private PurchaseManager purchaseManager;
    
    private void Start()
    {
        // Get PurchaseManager from ServiceLocator
        purchaseManager = ServiceLocator.Resolve<PurchaseManager>();
        
        // Subscribe to purchase events
        PurchaseEventManager.OnPurchaseCompleted += OnPurchaseSuccess;
        PurchaseEventManager.OnPurchaseFailed += OnPurchaseFailure;
    }
    
    public void OnBuyButtonClicked(ShopPackage package)
    {
        // Initiate purchase - PurchaseManager handles everything
        purchaseManager.PurchasePackage(package);
    }
    
    private void OnPurchaseSuccess(PurchaseEventArgs args)
    {
        // Grant rewards to player
        if (args.ShopPackage != null)
        {
            GrantRewards(args.ShopPackage.lootReward);
        }
        
        // Show success feedback
        ShowPurchaseSuccess();
    }
    
    private void OnPurchaseFailure(PurchaseEventArgs args)
    {
        // Show error message to user
        ShowErrorMessage(args.ErrorMessage);
    }
}
```

### Advanced Integration

```csharp
public class MonetizationManager : MonoBehaviour, IPurchaseEventHandler
{
    private PurchaseManager purchaseManager;
    
    private void Start()
    {
        purchaseManager = ServiceLocator.Resolve<PurchaseManager>();
        
        // Register as event handler to receive all events
        PurchaseEventManager.RegisterHandler(this);
    }
    
    // Implement IPurchaseEventHandler
    public void OnPurchaseCompleted(PurchaseEventArgs args)
    {
        // Track successful purchase analytics
        TrackPurchaseSuccess(args);
        
        // Update player progression
        UpdatePlayerProgression(args);
        
        // Trigger other game systems
        TriggerPurchaseEffects(args);
    }
    
    public void OnPurchaseFailed(PurchaseEventArgs args)
    {
        // Track failed purchase analytics
        TrackPurchaseFailure(args);
        
        // Handle retry logic if needed
        HandlePurchaseRetry(args);
    }
    
    // Other IPurchaseEventHandler methods...
}
```

### Product Information

```csharp
public class ProductInfoDisplay : MonoBehaviour
{
    public void UpdateProductDisplay(string productId)
    {
        var purchaseManager = ServiceLocator.Resolve<PurchaseManager>();
        var productInfo = purchaseManager.GetProductInfo(productId);
        
        if (productInfo != null)
        {
            priceLabel.text = productInfo.PriceString;
            titleLabel.text = productInfo.Title;
            descriptionLabel.text = productInfo.Description;
        }
    }
}
```

## Event System

### Available Events

```csharp
// Subscribe to specific events
PurchaseEventManager.OnPurchaseSystemInitialized += OnSystemReady;
PurchaseEventManager.OnPurchaseStarted += OnPurchaseStarted;
PurchaseEventManager.OnPurchaseValidating += OnValidating;
PurchaseEventManager.OnPurchaseCompleted += OnSuccess;
PurchaseEventManager.OnPurchaseFailed += OnFailure;
PurchaseEventManager.OnPurchaseRestored += OnRestored;
PurchaseEventManager.OnProductsFetched += OnProductsLoaded;
```

### Event Handler Interface

```csharp
public class MyPurchaseHandler : IPurchaseEventHandler
{
    public void OnPurchaseInitialized(InitializationEventArgs args) { }
    public void OnPurchaseStarted(PurchaseEventArgs args) { }
    public void OnPurchaseValidating(PurchaseEventArgs args) { }
    public void OnPurchaseCompleted(PurchaseEventArgs args) { }
    public void OnPurchaseFailed(PurchaseEventArgs args) { }
    public void OnPurchaseRestored(PurchaseEventArgs args) { }
    public void OnProductsFetched(ProductFetchEventArgs args) { }
}
```

## Configuration

### PurchaseConfig Settings

```csharp
[Header("General Settings")]
public bool enableDebugLogging = true;
public bool enablePurchaseValidation = true;
public float purchaseTimeoutSeconds = 30f;

[Header("Product Info Cache")]
public float productInfoCacheHours = 24f;
public bool autoRefreshProductInfo = true;

[Header("Analytics")]
public bool enablePurchaseAnalytics = true;
public bool enableFunnelTracking = true;
```

## Security & Validation

### Receipt Validation

The system supports multiple validation methods:

1. **Unity IAP Validator**: Uses Unity's built-in CrossPlatformValidator
2. **Server Validator**: Custom server-side validation (implement as needed)
3. **Mock Validator**: For testing purposes
4. **Composite Validator**: Combines multiple validators

### Validation Configuration

```csharp
// Configure validation in PurchaseConfig
config.enablePurchaseValidation = true;

// The system automatically chooses appropriate validator based on platform
```

## Analytics Integration

### Automatic Events

The system automatically tracks these analytics events:

- `iap_purchase_completed`: Successful purchases
- `iap_purchase_failed`: Failed purchases
- Purchase funnel events (if enabled)

### Custom Analytics

```csharp
private void OnPurchaseCompleted(PurchaseEventArgs args)
{
    // System automatically tracks basic events
    // Add custom analytics here
    
    var analyticsManager = ServiceLocator.Resolve<AnalyticsManager>();
    analyticsManager.TrackEvent("custom_purchase_success", new Dictionary<string, object>
    {
        ["product_category"] = GetProductCategory(args.ProductId),
        ["user_level"] = GetPlayerLevel(),
        ["purchase_session"] = GetCurrentSession()
    });
}
```

## Error Handling

### Common Error Scenarios

1. **Purchase System Not Initialized**
   - Check GameInitFlow configuration
   - Verify PurchaseManager is assigned and registered

2. **Product Not Found**
   - Verify product IDs in ShopConfig
   - Check Unity IAP catalog configuration

3. **Validation Failure**
   - Check receipt validation settings
   - Verify store configuration

4. **Network Issues**
   - Handle timeout scenarios
   - Implement retry logic

### Error Recovery

```csharp
private void OnPurchaseFailed(PurchaseEventArgs args)
{
    switch (args.FailureReason)
    {
        case PurchaseFailureReason.UserCancelled:
            // User cancelled - no action needed
            break;
            
        case PurchaseFailureReason.BillingUnavailable:
            // Show store unavailable message
            ShowStoreUnavailableMessage();
            break;
            
        case PurchaseFailureReason.ProductUnavailable:
            // Refresh product catalog
            RefreshProducts();
            break;
            
        default:
            // Generic error handling
            ShowGenericErrorMessage();
            break;
    }
}
```

## Testing

### Development Testing

1. Use mock validator for development:
```csharp
// In development builds
#if DEVELOPMENT_BUILD
purchaseValidator = new MockValidator(alwaysValid: true);
#endif
```

2. Enable debug logging:
```csharp
config.enableDebugLogging = true;
```

### Store Testing

#### iOS Testing
- Use sandbox environment
- Create test accounts in App Store Connect
- Test with various scenarios (success, failure, restoration)

#### Android Testing
- Use test tracks in Google Play Console
- Test with test accounts
- Verify receipt validation

## Best Practices

### 1. Always Check Initialization
```csharp
if (!purchaseManager.IsInitialized)
{
    // Handle not ready state
    return;
}
```

### 2. Handle All Event Types
```csharp
// Don't just handle success - handle all states
PurchaseEventManager.OnPurchaseStarted += ShowPurchaseInProgress;
PurchaseEventManager.OnPurchaseCompleted += OnSuccess;
PurchaseEventManager.OnPurchaseFailed += OnFailure;
```

### 3. Graceful Error Handling
```csharp
private void OnPurchaseFailed(PurchaseEventArgs args)
{
    // Always provide user-friendly error messages
    string userMessage = GetUserFriendlyMessage(args.FailureReason);
    ShowErrorDialog(userMessage);
}
```

### 4. Analytics Tracking
```csharp
// Track purchase funnel for business insights
private void TrackPurchaseFunnel(string step, string productId)
{
    analyticsManager.TrackEvent("purchase_funnel", new Dictionary<string, object>
    {
        ["step"] = step,
        ["product_id"] = productId,
        ["timestamp"] = DateTime.UtcNow
    });
}
```

## Troubleshooting

### Common Issues

1. **PurchaseManager not found in ServiceLocator**
   - Check GameInitFlow has PurchaseManager assigned
   - Verify initialization order

2. **Products not loading**
   - Check ShopConfig has IAP packages configured
   - Verify Unity IAP setup

3. **Validation always failing**
   - Check platform-specific validator setup
   - Verify store certificates

4. **Events not firing**
   - Check event subscription timing
   - Verify PurchaseEventManager usage

### Debug Information

Enable debug logging to see:
```
[PurchaseManager] Initialized successfully
[PurchaseManager] IAP initialized with 5 products
[PurchaseManager] Initiating purchase for com.example.coins_100
[PurchaseManager] Processing purchase for com.example.coins_100
[PurchaseManager] Purchase completed for com.example.coins_100
```

## Migration Guide

### From Other IAP Systems

1. Replace direct Unity IAP calls with PurchaseManager calls
2. Convert success/failure callbacks to event subscriptions
3. Update product configuration to use ShopConfig system
4. Remove manual receipt validation code

### Integration Checklist

- [ ] PurchaseManager added to GameInitFlow
- [ ] Products configured in ShopConfig
- [ ] Event handlers implemented
- [ ] Error handling added
- [ ] Analytics integration verified
- [ ] Testing completed on target platforms

## Future Enhancements

- [ ] Subscription support
- [ ] Purchase restore improvements
- [ ] Advanced analytics features
- [ ] A/B testing integration
- [ ] Dynamic pricing support
- [ ] Fraud detection improvements

---

**Note**: This system focuses purely on purchase business logic and does not handle any UI elements. All UI interactions should be implemented in your UI layer using the provided callbacks and events.