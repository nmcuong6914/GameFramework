# Currency Cooldown Fix - Maximum Capacity Timing Issue

## Problem Description
When a currency reaches maximum capacity and then gets spent, the cooldown timer was not starting fresh. Instead, it was calculating time based on when the currency was last updated, even during the period when the currency was at maximum capacity and not regenerating.

## Issue Example

### Scenario:
1. Player has Lives: 10/10 (maximum capacity)
2. Player waits 3 minutes (currency at max, no regeneration needed)
3. Player uses 1 life → Lives: 9/10
4. **Expected**: Cooldown should start at full 5 minutes
5. **Actual Bug**: Cooldown was only 2 minutes (5 - 3 = 2)

## Root Cause Analysis

### The Problem in Code:
```csharp
// When at max capacity, lastUpdateTicks keeps ticking
// but no regeneration occurs

// Later when currency is spent:
public float GetTimeToNextRegeneration()
{
    var timeSinceLastUpdate = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - lastUpdateTicks);
    float elapsedSeconds = (float)timeSinceLastUpdate.TotalSeconds;
    float timeToNext = config.regenerationInterval - (elapsedSeconds % config.regenerationInterval);
    return timeToNext; // This includes time when currency was at max!
}
```

### Timeline Example:
```
Time 0: Lives = 10/10, lastUpdateTicks = T0
Time 3min: Lives = 10/10, lastUpdateTicks = T0 (still)
Time 3min: Player spends 1 life → Lives = 9/10
Time 3min: GetTimeToNextRegeneration() calculates:
           elapsedSeconds = 3 minutes = 180 seconds
           timeToNext = 300 - (180 % 300) = 120 seconds ❌ WRONG!
           
Should be: timeToNext = 300 seconds (full cooldown) ✅ CORRECT!
```

## Solution Implementation

### Strategy: Reset Timer When Leaving Max Capacity
When currency transitions from maximum capacity to below maximum (via spending), reset the regeneration timer to start the cooldown fresh.

### Code Changes:

#### 1. Enhanced Spend() Method
```csharp
public bool Spend(int value)
{
    if (value <= 0 || amount < value) return false;
    
    UpdateRegeneration();
    
    // Check if we were at max capacity before spending
    bool wasAtMaxCapacity = IsAtMaxCapacity;
    
    int oldAmount = amount;
    amount -= value;
    
    // If we were at max capacity and now we're not, reset the regeneration timer
    // This ensures cooldown starts fresh from full duration
    if (wasAtMaxCapacity && !IsAtMaxCapacity && CanRegenerate)
    {
        ResetRegenerationTimer();
    }
    
    AmountChanged?.Invoke(this, oldAmount, amount);
    return true;
}
```

#### 2. Enhanced SetAmount() Method
```csharp
public void SetAmount(int newAmount)
{
    UpdateRegeneration();
    
    // Check if we were at max capacity before setting
    bool wasAtMaxCapacity = IsAtMaxCapacity;
    
    int oldAmount = amount;
    amount = config != null ? Mathf.Clamp(newAmount, 0, config.maxAmount) : Mathf.Max(0, newAmount);
    
    // If we were at max capacity and now we're not, reset the regeneration timer
    // This ensures cooldown starts fresh from full duration
    if (wasAtMaxCapacity && !IsAtMaxCapacity && CanRegenerate && amount < oldAmount)
    {
        ResetRegenerationTimer();
    }
    
    if (amount != oldAmount)
    {
        AmountChanged?.Invoke(this, oldAmount, amount);
    }
}
```

## How the Fix Works

### 1. **Detection**: Check if currency was at max capacity before the operation
```csharp
bool wasAtMaxCapacity = IsAtMaxCapacity;
```

### 2. **Operation**: Perform the spend/set operation normally
```csharp
amount -= value; // or amount = newAmount;
```

### 3. **Cooldown Reset**: If transitioning from max to below max, reset timer
```csharp
if (wasAtMaxCapacity && !IsAtMaxCapacity && CanRegenerate)
{
    ResetRegenerationTimer(); // Sets lastUpdateTicks = DateTime.UtcNow.Ticks
}
```

### 4. **Result**: Next regeneration starts with full cooldown duration

## Fixed Timeline Example

### After Fix:
```
Time 0: Lives = 10/10, lastUpdateTicks = T0
Time 3min: Lives = 10/10, lastUpdateTicks = T0 (still)
Time 3min: Player spends 1 life → Lives = 9/10
Time 3min: ResetRegenerationTimer() called → lastUpdateTicks = T3
Time 3min: GetTimeToNextRegeneration() calculates:
           elapsedSeconds = 0 seconds (just reset)
           timeToNext = 300 - (0 % 300) = 300 seconds ✅ CORRECT!
```

## Edge Cases Handled

### 1. **Multiple Spends from Max**
- First spend: Resets timer ✅
- Subsequent spends: No reset (already below max) ✅

### 2. **Partial vs Full Spend**
- Spend 1 from 10/10 → Reset timer ✅
- Spend 5 from 10/10 → Reset timer ✅  
- Both scenarios get full cooldown ✅

### 3. **SetAmount() Scenarios**
- SetAmount(9) when at 10/10 → Reset timer ✅
- SetAmount(11) when at 10/10 → No reset (still at max) ✅
- SetAmount(15) when at 10/10 → No reset (increasing) ✅

### 4. **Non-Regenerating Currencies**
- `CanRegenerate` check prevents timer reset for currencies that don't regenerate ✅

## Benefits

### ✅ **Correct Cooldown Timing**
- Cooldown always starts at full duration when spending from max capacity
- No more shortened cooldowns due to "idle time" at max capacity

### ✅ **Intuitive Player Experience**
- Players get the full regeneration time they expect
- Consistent timing regardless of how long currency was at max

### ✅ **Backward Compatible**
- No breaking changes to existing functionality
- Only affects the specific edge case of spending from max capacity

### ✅ **Comprehensive Coverage**
- Handles both `Spend()` and `SetAmount()` methods
- Covers all ways currency amount can be reduced from max

## Testing Scenarios

### Scenario 1: Lives Regeneration
1. Player has Lives: 10/10
2. Wait 10 minutes (no regeneration needed)
3. Use 1 life → Lives: 9/10
4. **Expected**: 5 minutes until next life
5. **Result**: ✅ 5 minutes (full cooldown)

### Scenario 2: Energy System
1. Player has Energy: 100/100  
2. Wait 30 minutes (no regeneration needed)
3. Spend 20 energy → Energy: 80/100
4. **Expected**: Full regeneration interval until next energy
5. **Result**: ✅ Full interval (not shortened by wait time)

### Scenario 3: Rapid Spending
1. Player has Lives: 10/10
2. Spend 3 lives quickly → Lives: 7/10
3. **Expected**: Full cooldown from first spend
4. **Result**: ✅ Timer reset only on first spend

## Files Modified

- `Currency.cs`: Enhanced `Spend()` and `SetAmount()` methods with max capacity detection and timer reset logic

## Related Systems

This fix ensures compatibility with:
- Lives regeneration system
- Offline currency generation
- UI cooldown displays
- Any currency that uses regeneration timing
