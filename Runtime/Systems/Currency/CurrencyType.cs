using System;
using UnityEngine;

/// <summary>
/// Enum representing different types of currencies in the game
/// </summary>
public enum CurrencyType
{
    Coin = 0,
    Lives = 1,
    BoosterHammer = 2,
    BoosterShuffle = 3,
    BoosterUndo = 4,
    BoosterHint = 5,
    BoosterDynamite = 6,
    BoosterFreezeTime = 7,
    BoosterExtraTime = 8
}

/// <summary>
/// Enum representing regeneration behavior for currencies
/// </summary>
public enum RegenerationType
{
    None = 0,        // No regeneration (like coins, boosters)
    OverTime = 1     // Regenerates over time with max cap (like lives)
}
