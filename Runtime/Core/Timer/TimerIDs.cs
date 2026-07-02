/// <summary>
/// Timer ID constants to avoid magic strings
/// </summary>
public static class TimerIDs
{
    // Level timers
    public const string LEVEL_TIMER = "LevelTimer";
    
    // Booster timers
    public const string FREEZE_TIME_COUNTDOWN = "FreezeTimeCountdown";
    
    // Timer bomb prefix - will be combined with block instance ID
    public const string TIMER_BOMB_PREFIX = "TimerBomb_";
    
    // Cooldown timers
    public const string HINT_COOLDOWN = "HintCooldown";
    public const string SHUFFLE_COOLDOWN = "ShuffleCooldown";
    
    // Lives regeneration
    public const string LIVES_REGENERATION = "LivesRegeneration";
}
