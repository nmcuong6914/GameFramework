# Asset Reference Collection System

## Overview

The Asset Reference Collection system provides a type-safe, centralized way to manage asset references for VFX, UI, GameObjects, Audio, and Materials in your Unity project. It uses enum-based keys for type safety and integrates seamlessly with Unity's Addressables system.

## Key Components

### 1. **AssetKey Enum**
- Type-safe enum that defines all available asset keys
- Organized by categories (VFX, UI, GameObject, Audio, Material)
- Each category has a unique number range for easy identification

### 2. **AssetReferenceCollection ScriptableObject**
- Centralized configuration for mapping AssetKeys to AssetReferences
- Provides validation and organization tools
- Supports categorized viewing and management

### 3. **AssetManager**
- Runtime manager for loading and accessing assets
- Provides async loading methods
- Handles resource management and cleanup

### 4. **Custom Editor**
- Enhanced inspector interface for managing the collection
- Category-based organization with search and filtering
- Validation tools and batch operations

## Setup Instructions

### Step 1: Create Asset Reference Collection
1. In Unity, right-click in Project window
2. Go to `Create → BlockSort → Asset Reference Collection`
3. Name it `GameAssetCollection` or similar
4. Assign it to the AssetManager component

### Step 2: Add AssetManager to Scene
1. Create an empty GameObject named "AssetManager"
2. Add the AssetManager component
3. Assign your AssetReferenceCollection to the `assetCollection` field

### Step 3: Configure Asset Entries
1. Select your AssetReferenceCollection
2. Use the custom editor to add asset entries
3. Assign AssetKeys and corresponding AssetReferences
4. Use the validation tools to check for issues

## Usage Examples

### Basic Asset Loading

```csharp
public class ExampleUsage : MonoBehaviour
{
    async void Start()
    {
        // Load a VFX GameObject
        var explosionVFX = await AssetManager.Instance.LoadGameObjectAsync(
            AssetKey.VFX_BlockExplosion, 
            transform
        );
        
        // Load a UI prefab
        var healthBar = await AssetManager.Instance.LoadGameObjectAsync(
            AssetKey.UI_HealthBar, 
            uiParent
        );
        
        // Load a material asset
        var redMaterial = await AssetManager.Instance.LoadAssetAsync<Material>(
            AssetKey.Material_BlockRed
        );
    }
}
```

### Preloading Assets by Category

```csharp
public class GameInitializer : MonoBehaviour
{
    async void Start()
    {
        // Preload all VFX assets for better performance
        await AssetManager.Instance.PreloadAssetsByCategory(AssetCategory.VFX);
        
        // Preload all UI assets
        await AssetManager.Instance.PreloadAssetsByCategory(AssetCategory.UI);
        
        Debug.Log("Asset preloading complete!");
    }
}
```

### Integration with Existing Systems

#### VFX System Integration
```csharp
public class VFXController : MonoBehaviour
{
    public async Task PlayVFX(AssetKey vfxKey, Vector3 position)
    {
        var vfxPrefab = await AssetManager.Instance.LoadGameObjectAsync(vfxKey);
        if (vfxPrefab != null)
        {
            vfxPrefab.transform.position = position;
            // Additional VFX setup...
        }
    }
}
```

#### UI System Integration
```csharp
public class UIManager : MonoBehaviour
{
    public async Task<T> ShowUI<T>(AssetKey uiKey) where T : MonoBehaviour
    {
        var uiPrefab = await AssetManager.Instance.LoadGameObjectAsync(uiKey, uiRoot);
        return uiPrefab?.GetComponent<T>();
    }
}
```

## Advanced Features

### Custom Asset Categories
To add new asset categories:

1. Modify the `AssetKey` enum to include new number ranges
2. Add corresponding cases to the `GetCategory()` method in `AssetKeyExtensions`
3. Update the custom editor's color mapping if desired

### Validation and Debugging
The system includes built-in validation:
- Checks for duplicate keys
- Validates AssetReference integrity
- Provides debug information in the inspector
- Logs issues to the console

### Performance Considerations
- Use preloading for frequently accessed assets
- Release assets when no longer needed using `ReleaseAsset()`
- Monitor loaded asset count in the AssetManager inspector

## Asset Key Organization

### VFX Assets (100-199)
- `VFX_BlockExplosion = 100`
- `VFX_LevelComplete = 101`
- `VFX_ComboEffect = 102`
- etc.

### UI Assets (200-299)
- `UI_HealthBar = 200`
- `UI_ScoreCounter = 201`
- `UI_PowerUpButton = 202`
- etc.

### GameObject Assets (300-399)
- `GameObject_Block = 300`
- `GameObject_Wall = 301`
- `GameObject_Floor = 302`
- etc.

### Audio Assets (400-499)
- `Audio_BackgroundMusic = 400`
- `Audio_SFX_BlockMove = 401`
- `Audio_SFX_BlockDestroy = 402`
- etc.

### Material Assets (500-599)
- `Material_BlockRed = 500`
- `Material_BlockBlue = 501`
- `Material_BlockGreen = 502`
- etc.

## Migration from Existing Systems

### From Direct References
```csharp
// Old way:
[SerializeField] private GameObject explosionPrefab;

// New way:
var explosion = await AssetManager.Instance.LoadGameObjectAsync(
    AssetKey.VFX_BlockExplosion
);
```

### From String-Based Systems
```csharp
// Old way:
var asset = LoadAsset("BlockExplosion");

// New way:
var asset = await AssetManager.Instance.LoadGameObjectAsync(
    AssetKey.VFX_BlockExplosion
);
```

## Best Practices

1. **Use Descriptive Names**: Make AssetKey names clear and consistent
2. **Organize by Category**: Keep related assets grouped together
3. **Validate Regularly**: Use the validation tools to catch issues early
4. **Preload Strategically**: Preload frequently used assets at scene start
5. **Clean Up Resources**: Release assets when switching scenes or states
6. **Document Dependencies**: Use the description field in asset entries

## Troubleshooting

### Common Issues

**"No asset reference found for key"**
- Ensure the AssetKey is added to the AssetReferenceCollection
- Check that the AssetReference is properly assigned

**"Invalid asset reference provided"**
- Verify the AssetReference points to a valid Addressable asset
- Check that the asset exists in the Addressables groups

**"AssetCollection is not assigned"**
- Make sure the AssetManager has a reference to your AssetReferenceCollection

### Debug Tools
- Use the validation button in the custom editor
- Check the console for detailed error messages
- Monitor the loaded assets count in the AssetManager inspector
