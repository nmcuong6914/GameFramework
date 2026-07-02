/// <summary>
/// Enum for all available sound IDs in the game
/// This provides type safety and prevents typos when referencing sounds
/// </summary>
public enum SoundID
{
    // Background Music
    BackgroundMusic,
    
    // Game SFX
    WinSFX,
    LoseSFX,
    BlockMove,
    BlockDestroy,
    BlockPlace,
    BlockMatch,
    BlockPickup,
    BlockDrop,
    PassGate,
    CollectReward,
    
    // UI Sounds
    ButtonClick
}
