/// <summary>
/// Signal fired when any popup is opened
/// Used for pausing game systems like timer bombs
/// </summary>
public class PopupOpenedSignal : Signal
{
    public PopupOpenedSignal()
    {
    }
}

/// <summary>
/// Signal fired when any popup is closed
/// Used for resuming game systems like timer bombs
/// </summary>
public class PopupClosedSignal : Signal
{
    public PopupClosedSignal()
    {
    }
}