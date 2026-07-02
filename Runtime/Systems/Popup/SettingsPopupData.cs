using System;

/// <summary>
/// Data class for Settings popup
/// </summary>
public class SettingsPopupData : ScreenData
{
    public bool isMusicEnabled;
    public bool isSFXEnabled;
    public Action onCloseSettings;
    
    public SettingsPopupData(bool isMusicEnabled, bool isSFXEnabled, Action onCloseSettings = null)
    {
        this.isMusicEnabled = isMusicEnabled;
        this.isSFXEnabled = isSFXEnabled;
        this.onCloseSettings = onCloseSettings;
    }
}
