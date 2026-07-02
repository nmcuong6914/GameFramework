using System;

public class PopupSignal : Signal
{
    public PopupType PopupKey { get; private set; }
    public ScreenData ScreenData { get; private set; }
    
    public PopupSignal(PopupType popupKey, ScreenData screenData)
    {
        PopupKey = popupKey;
        ScreenData = screenData;
    }
}
