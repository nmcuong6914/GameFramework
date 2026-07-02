# AssetKeyRangeConfiguration System

## Overview
The `AssetKeyRangeConfiguration` system provides a configurable way to manage asset key ranges in Unity property drawers, eliminating hardcoded values and making the system more flexible and maintainable.

## How It Works

### 1. AssetKeyRangeConfiguration ScriptableObject
- Located at: `Assets/Scripts/Services/AssetReference/AssetKeyRangeConfiguration.asset`
- Contains configurable ranges for different asset categories (VFX, UI, GameObject, Audio, Material, Currency)
- Each range specifies:
  - **Category**: The type of asset (enum AssetCategory)
  - **MinValue**: Minimum enum value (inclusive)
  - **MaxValue**: Maximum enum value (inclusive)  
  - **DisplayPrefix**: Prefix to remove from display names (e.g., "Currency_")

### 2. Current Configuration
```
VFX:        100-199, prefix: "VFX_"
UI:         200-299, prefix: "UI_"
GameObject: 300-399, prefix: "GameObject_"
Audio:      400-499, prefix: "Audio_"
Material:   500-599, prefix: "Material_"
Currency:   600-699, prefix: "Currency_"
```

### 3. Property Drawers Integration

#### CurrencyConfigPropertyDrawer
- Automatically loads the range configuration at startup
- Uses `AssetCategory.Currency` range for filtering asset keys
- Falls back to hardcoded values (600-700) if configuration is missing

#### CurrencyConfigDataEditor  
- Loads range configuration when enabled
- Shows warning and creation button if configuration is missing
- Uses the configuration for asset key filtering in dropdowns

### 4. Benefits

✅ **Configurable**: Change ranges without code modifications
✅ **Maintainable**: No hardcoded magic numbers
✅ **Extensible**: Easy to add new asset categories
✅ **Fallback**: Works even without configuration file
✅ **User-Friendly**: Clean display names with prefix removal

### 5. Usage

1. **Automatic**: The system loads the configuration automatically
2. **Manual Creation**: Use "Create > BlockSort > Asset Key Range Configuration" 
3. **Customization**: Edit the asset in the inspector to change ranges
4. **Validation**: Property drawers validate and show warnings for missing assets

### 6. Key Features Added to CurrencyConfigDataEditor

- ➕ **Add Currency** button to create new entries
- ❌ **Individual Remove** buttons for each currency
- 🗑️ **Remove Last** button for quick removal
- 🚨 **Validation** with confirmation dialogs
- 📊 **Currency count display**
- 🔧 **Auto-configuration creation** if missing

### 7. Troubleshooting

**Issue**: "enum index is out of range"  
**Solution**: The property drawers now correctly map enum values to indices

**Issue**: "AssetKeyRangeConfiguration not found"  
**Solution**: Click "Create AssetKeyRangeConfiguration" button in the editor

**Issue**: No assets showing in dropdown  
**Solution**: Check that your AssetKey enum has values in the specified range

This system makes the asset management much more flexible and maintainable while providing a better user experience in the Unity Inspector.
