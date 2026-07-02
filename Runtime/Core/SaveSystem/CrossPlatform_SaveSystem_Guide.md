# Cross-Platform Save System Guide

## Overview

The SaveSystem has been optimized for cross-platform compatibility, specifically supporting Android and iOS platforms alongside desktop platforms.

## Platform-Specific Features

### Android Support
- **Storage Location**: `/storage/emulated/0/Android/data/<packagename>/files/SaveData/`
- **Permissions**: Uses Application.persistentDataPath which is automatically writable
- **File Operations**: Custom async file I/O for better compatibility
- **Backup Strategy**: Automatic backup creation before saves

### iOS Support  
- **Storage Location**: `Documents` directory (backed up to iTunes/iCloud)
- **Permissions**: Uses Application.persistentDataPath which is automatically writable
- **File Operations**: Custom async file I/O optimized for iOS filesystem
- **App Store Compliance**: Uses approved storage locations

### Desktop Support (Windows/Mac/Linux)
- **Storage Location**: Standard persistent data path
- **File Operations**: Standard .NET async file operations
- **Development**: Full file system access for debugging

## Key Features

### 🛡️ Atomic Saves
- Writes to temporary file first, then moves to final location
- Prevents data corruption if save is interrupted
- Works on all platforms including mobile

### 🔄 Automatic Backup System  
- Creates backup before each save operation
- Falls back to backup if main save file is corrupted
- Multiple backup creation options

### 📱 Mobile Optimizations
- Platform-specific file I/O methods for Android/iOS
- Proper exception handling for mobile filesystem quirks
- Fallback directory creation if subfolder fails

### 🔍 Debug Support
- Platform information logging
- Path information for troubleshooting
- Detailed error messages with context

## Usage Examples

### Basic Save/Load
```csharp
var saveSystem = new SaveSystem();

// Save
bool success = await saveSystem.SaveAsync(playerData);

// Load
PlayerData loadedData = await saveSystem.LoadAsync();
```

### Platform Information
```csharp
string info = saveSystem.GetPlatformInfo();
Debug.Log(info);
// Output: "Platform: Android, Persistent Data Path: /storage/emulated/0/Android/data/com.company.game/files, Save Folder: .../SaveData"
```

### Manual Backups
```csharp
// Create timestamped backup
await saveSystem.CreateBackupAsync(playerData);

// Create named backup
await saveSystem.CreateBackupAsync(playerData, "before_update_v2");
```

## File Structure

```
Application.persistentDataPath/
└── SaveData/
    ├── playerdata.json           (Main save file)
    ├── playerdata_backup.json    (Automatic backup)
    └── playerdata_backup_20250721_143000.json (Manual backup)
```

## Error Handling

The system handles common mobile platform issues:

- **Storage Permission Issues**: Automatic fallback to root persistent data path
- **File Lock Issues**: Proper file stream disposal with `using` statements  
- **Interrupted Saves**: Atomic save operations prevent corruption
- **Missing Directories**: Automatic directory creation with fallbacks

## Platform Testing

### Android Testing
1. Build to Android device
2. Check logs for "Android persistent data path" message
3. Verify save/load operations work after app restart
4. Test with low storage conditions

### iOS Testing  
1. Build to iOS device  
2. Check logs for "iOS persistent data path" message
3. Verify saves persist after app backgrounding/foregrounding
4. Test backup restoration functionality

## Troubleshooting

### Common Issues

**Save fails on mobile devices:**
- Check `GetPlatformInfo()` output for correct paths
- Verify Application.persistentDataPath is accessible
- Check device storage space

**Data not persisting between sessions:**
- Ensure PlayerData.ToJson() returns valid JSON
- Check for exceptions in save process
- Verify file permissions on target device

**Backup system not working:**
- Confirm backup file exists in save directory
- Check backup creation logs
- Test manual backup creation

### Debug Commands

```csharp
// Get detailed platform info
Debug.Log(saveSystem.GetPlatformInfo());

// Check if save files exist
bool exists = saveSystem.SaveFileExists();

// Get save file size
long size = saveSystem.GetSaveFileSize();

// Get last save time
DateTime? lastSave = saveSystem.GetLastSaveTime();
```

## Best Practices

1. **Always await save operations** - Don't fire-and-forget saves
2. **Handle save failures gracefully** - Show user feedback for save issues
3. **Test on target platforms** - Mobile behavior can differ from editor
4. **Monitor file sizes** - Large saves may cause performance issues on mobile
5. **Use manual backups** - Before major updates or risky operations

## Platform Permissions

### Android
- No special permissions required
- Uses app-private storage
- Data cleared when app uninstalled

### iOS  
- No special permissions required
- Uses Documents directory (included in iTunes backup)
- Data persists through app updates but not reinstalls

The save system is now fully cross-platform compatible and handles the unique requirements of mobile platforms while maintaining desktop compatibility.
