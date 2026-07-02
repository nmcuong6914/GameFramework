/// <summary>
/// Signals related to currency and player data systems
/// </summary>

public class CurrencyChangedSignal : Signal
{
    public CurrencyType CurrencyType { get; }
    public int OldAmount { get; }
    public int NewAmount { get; }
    public int Delta { get; }
    
    public CurrencyChangedSignal(CurrencyType currencyType, int oldAmount, int newAmount)
    {
        CurrencyType = currencyType;
        OldAmount = oldAmount;
        NewAmount = newAmount;
        Delta = newAmount - oldAmount;
    }
}

public class PlayerDataLoadedSignal : Signal
{
    public PlayerData PlayerData { get; }
    public bool IsNewPlayer { get; }
    
    public PlayerDataLoadedSignal(PlayerData playerData, bool isNewPlayer)
    {
        PlayerData = playerData;
        IsNewPlayer = isNewPlayer;
    }
}

public class PlayerDataSaveSignal : Signal
{
    public PlayerData PlayerData { get; }
    public bool Success { get; }
    
    public PlayerDataSaveSignal(PlayerData playerData, bool success)
    {
        PlayerData = playerData;
        Success = success;
    }
}
