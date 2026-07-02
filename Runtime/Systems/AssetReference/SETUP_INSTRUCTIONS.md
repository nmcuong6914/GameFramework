# Setup Instructions - Asset Reference Collection System

## 🚀 Quick Setup Guide

### 1. Create Asset Reference Collection
1. In Unity Project window, right-click
2. Go to `Create → BlockSort → Asset Reference Collection`
3. Name it `GameAssetCollection`
4. Save it in `Assets/Resources/` or any folder you prefer

### 2. Setup AssetManager in Scene
1. Create empty GameObject named "AssetManager"
2. Add `AssetManager` component
3. Assign your `GameAssetCollection` to the `assetCollection` field
4. The AssetManager will automatically register itself with ServiceLocator

### 3. Configure Asset Entries
1. Select your `GameAssetCollection` ScriptableObject
2. Use the enhanced custom editor to add asset entries:
   - Click "Add Entry" to create new entries
   - Select appropriate AssetKey from dropdown
   - Drag your Addressable assets to AssetReference fields
   - Add descriptions for better organization

### 4. Update VFXController
1. Select your VFXController GameObject
2. Configure VFX Pools:
   - Set `assetKey` to appropriate VFX AssetKey (e.g., `AssetKey.VFX_BlockExplosion`)
   - Remove old `prefabReference` assignments
   - Set pool sizes and auto-release times
3. Enable `preloadOnStart` for better performance

## 🔧 Migration from Old System

### VFXController Changes
**Old Way (VFXKey system):**
```csharp
public enum VFXKey { BlockBlast }
vfxController.PlayVFX(VFXKey.BlockBlast, position, rotation);
```

**New Way (AssetKey system):**
```csharp
// Now uses AssetKey enum
vfxController.PlayVFX(AssetKey.VFX_BlockExplosion, position, rotation);

// Or async version
var vfx = await vfxController.PlayVFXAsync(AssetKey.VFX_BlockExplosion, position);
```

### Using DI for AssetManager
**In any MonoBehaviour:**
```csharp
public class MyScript : MonoBehaviour
{
    private AssetManager assetManager;
    
    void Start()
    {
        // Get via DI
        assetManager = ServiceLocator.TryResolve<AssetManager>();
        
        // Or fallback to Instance
        if (assetManager == null)
            assetManager = AssetManager.Instance;
    }
    
    async void LoadSomething()
    {
        var asset = await assetManager.LoadGameObjectAsync(AssetKey.UI_HealthBar);
    }
}
```

## 📋 AssetKey Categories

### VFX Assets (100-199)
```csharp
AssetKey.VFX_BlockExplosion = 100
AssetKey.VFX_LevelComplete = 101
AssetKey.VFX_ComboEffect = 102
// Add more as needed...
```

### UI Assets (200-299)
```csharp
AssetKey.UI_HealthBar = 200
AssetKey.UI_ScoreCounter = 201
AssetKey.UI_PowerUpButton = 202
// Add more as needed...
```

### GameObject Assets (300-399)
```csharp
AssetKey.GameObject_Block = 300
AssetKey.GameObject_Wall = 301
AssetKey.GameObject_Floor = 302
// Add more as needed...
```

## 🛠️ Common Usage Patterns

### Load and Instantiate GameObject
```csharp
var instance = await assetManager.LoadGameObjectAsync(AssetKey.GameObject_Block, parent);
```

### Load Asset Template
```csharp
var material = await assetManager.LoadAssetAsync<Material>(AssetKey.Material_BlockRed);
```

### Preload Assets by Category
```csharp
await assetManager.PreloadAssetsByCategory(AssetCategory.VFX);
```

### Play VFX
```csharp
// Simple usage
vfxController.PlayVFX(AssetKey.VFX_BlockExplosion, transform.position);

// Advanced usage with callback
await vfxController.PlayVFXWithCallback(
    AssetKey.VFX_BlockExplosion, 
    position, 
    rotation, 
    parent, 
    autoReleaseTime: 3f,
    onVFXReady: (vfx) => {
        // Configure the VFX instance
        var particles = vfx.GetComponent<ParticleSystem>();
        particles.startColor = Color.red;
    }
);
```

## 🎯 Benefits of New System

1. **Type Safety**: No more string-based references
2. **Centralized Management**: All assets in one collection
3. **Better Performance**: Object pooling + smart preloading
4. **Dependency Injection**: Clean architecture with DI support
5. **Editor Tools**: Enhanced inspector with validation
6. **Resource Management**: Proper cleanup and memory management

## 🐛 Troubleshooting

### "AssetManager not found"
- Ensure AssetManager GameObject exists in scene
- Check that AssetManager component is properly configured
- Verify ServiceLocator registration is working

### "Asset not found for key"
- Check AssetReferenceCollection has the key configured
- Verify AssetReference points to valid Addressable asset
- Use validation tools in custom editor

### VFX not playing
- Ensure VFX pool is properly configured with correct AssetKey
- Check if preloadOnStart is enabled or manually preload VFX
- Verify AssetReference is valid in collection

### Performance Issues
- Enable preloading for frequently used assets
- Use object pooling for VFX
- Monitor loaded asset count in AssetManager inspector
