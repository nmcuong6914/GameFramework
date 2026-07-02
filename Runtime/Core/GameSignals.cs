using System.Collections.Generic;
using UnityEngine;

// Removed BlockContactGateSignal and all gate-related signals

public class OnPassedGateSignal : Signal
{
    public BlockController Block { get; }
    
    public OnPassedGateSignal(BlockController block)
    {
        Block = block;
    }
}

public class BlockRemovedSignal : Signal
{
    public BlockController RemovedBlock { get; }
    public int RemainingBlockCount { get; }
    
    public BlockRemovedSignal(BlockController removedBlock, int remainingBlockCount)
    {
        RemovedBlock = removedBlock;
        RemainingBlockCount = remainingBlockCount;
    }
}

public class LevelLoadedSignal : Signal
{
    public int LevelIndex { get; }
    public Level LoadedLevel { get; }
    
    public LevelLoadedSignal(int levelIndex, Level loadedLevel)
    {
        LevelIndex = levelIndex;
        LoadedLevel = loadedLevel;
    }
}

public class LevelFailedSignal : Signal
{
    public int LevelIndex { get; }
    public Level FailedLevel { get; }
    public LevelFailReason FailReason { get; }
    
    public LevelFailedSignal(int levelIndex, Level failedLevel, LevelFailReason failReason)
    {
        LevelIndex = levelIndex;
        FailedLevel = failedLevel;
        FailReason = failReason;
    }
}

public enum LevelFailReason
{
    TimeExpired,
    NoMovesLeft,
    OutOfLives,
    TimerBombExploded,
    Custom
}

public class NextLevelSignal : Signal
{
    public NextLevelSignal()
    {
    }
}

public class RetryLevelSignal : Signal
{
    public RetryLevelSignal()
    {
    }
}

public class WinGameSignal : Signal
{
    public int LevelIndex { get; }
    public Level CompletedLevel { get; }
    public int Score { get; }
    public Dictionary<CurrencyType, int> Rewards { get; }
    public bool UpdateCurrencyUIImmediately { get; }

    public WinGameSignal(int levelIndex, Level completedLevel, int score, Dictionary<CurrencyType, int> rewards, bool updateCurrencyUIImmediately = true)
    {
        LevelIndex = levelIndex;
        CompletedLevel = completedLevel;
        Score = score;
        Rewards = rewards;
        UpdateCurrencyUIImmediately = updateCurrencyUIImmediately;
    }
}

public class LoseGameSignal : Signal
{
    public int LevelIndex { get; }
    public Level FailedLevel { get; }
    public LevelFailReason FailReason { get; }

    public LoseGameSignal(int levelIndex, Level failedLevel, LevelFailReason failReason)
    {
        LevelIndex = levelIndex;
        FailedLevel = failedLevel;
        FailReason = failReason;
    }
}

public class GameInitializationCompleteSignal : Signal
{
    public GameInitializationCompleteSignal()
    {
    }
}

public class BlockStartDragSignal : Signal
{
    public BlockController Block { get; }
    
    public BlockStartDragSignal(BlockController block)
    {
        Block = block;
    }
}

public class BlockDropSignal : Signal
{
    public BlockController Block { get; }
    public Vector2Int DropPosition { get; }
    
    public BlockDropSignal(BlockController block, Vector2Int dropPosition)
    {
        Block = block;
        DropPosition = dropPosition;
    }
}

public class TimerBombExplodedSignal : Signal
{
    public BlockController Block { get; }

    public TimerBombExplodedSignal(BlockController block)
    {
        Block = block;
    }
}

public class BlockDestroyedByDynamiteSignal : Signal
{
    public BlockController Block { get; }

    public BlockDestroyedByDynamiteSignal(BlockController block)
    {
        Block = block;
    }
}

/// <summary>
/// Signal to request a fade in transition (show dark panel)
/// Fire this signal when you want to start a transition
/// </summary>
public class FadeInTransitionSignal : Signal
{
    public float? CustomDuration { get; }
    public System.Action OnComplete { get; }
    
    public FadeInTransitionSignal(float? customDuration = null, System.Action onComplete = null)
    {
        CustomDuration = customDuration;
        OnComplete = onComplete;
    }
}

/// <summary>
/// Signal to request a fade out transition (hide dark panel)
/// Fire this signal when you want to end a transition
/// </summary>
public class FadeOutTransitionSignal : Signal
{
    public float? CustomDuration { get; }
    public System.Action OnComplete { get; }
    
    public FadeOutTransitionSignal(float? customDuration = null, System.Action onComplete = null)
    {
        CustomDuration = customDuration;
        OnComplete = onComplete;
    }
}

/// <summary>
/// Signal fired when fade in transition completes
/// </summary>
public class TransitionInCompleteSignal : Signal
{
    public TransitionInCompleteSignal()
    {
    }
}

/// <summary>
/// Signal fired when fade out transition completes
/// </summary>
public class TransitionOutCompleteSignal : Signal
{
    public TransitionOutCompleteSignal()
    {
    }
}

/// <summary>
/// Signal fired when a booster mode is activated
/// </summary>
public class BoosterModeEnabledSignal : Signal
{
    public CurrencyType BoosterType { get; }
    
    public BoosterModeEnabledSignal(CurrencyType boosterType)
    {
        BoosterType = boosterType;
    }
}

/// <summary>
/// Signal fired when booster mode is deactivated/cancelled
/// </summary>
public class BoosterModeDisabledSignal : Signal
{
    public CurrencyType BoosterType { get; }
    public bool WasUsed { get; } // True if booster was actually used, false if cancelled
    
    public BoosterModeDisabledSignal(CurrencyType boosterType, bool wasUsed = false)
    {
        BoosterType = boosterType;
        WasUsed = wasUsed;
    }
}

/// <summary>
/// Signal fired to enable or disable all booster buttons
/// </summary>
public class BoostersEnabledSignal : Signal
{
    public bool Enabled { get; }
    
    public BoostersEnabledSignal(bool enabled)
    {
        Enabled = enabled;
    }
}