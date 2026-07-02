using UnityEngine;
using System;

namespace BlockSort.Ads
{
    /// <summary>
    /// Signals for the ads system
    /// </summary>
    public class AdsInitializedSignal : Signal
    {
        public bool Success { get; }
        
        public AdsInitializedSignal(bool success)
        {
            Success = success;
        }
    }

    public class InterstitialAdClosedSignal : Signal
    {
        public string Placement { get; }
        
        public InterstitialAdClosedSignal(string placement)
        {
            Placement = placement;
        }
    }

    public class RewardedAdCompletedSignal : Signal
    {
        public string Placement { get; }
        public CurrencyType CurrencyType { get; }
        public int Amount { get; }
        
        public RewardedAdCompletedSignal(string placement, CurrencyType currencyType, int amount)
        {
            Placement = placement;
            CurrencyType = currencyType;
            Amount = amount;
        }
    }

    public class AdFailedSignal : Signal
    {
        public string AdType { get; }
        public string Placement { get; }
        public string Error { get; }
        
        public AdFailedSignal(string adType, string placement, string error)
        {
            AdType = adType;
            Placement = placement;
            Error = error;
        }
    }

    // Generic ad signals - simplified
    public class ShowInterstitialAdSignal : Signal
    {
        public string Placement { get; }
        public Action OnClosed { get; }
        
        public ShowInterstitialAdSignal(string placement, Action onClosed = null)
        {
            Placement = placement;
            OnClosed = onClosed;
        }
    }

    public class ShowRewardedAdSignal : Signal
    {
        public string Placement { get; }
        public Action OnCompleted { get; }
        public Action OnFailed { get; }
        
        public ShowRewardedAdSignal(string placement, Action onCompleted = null, Action onFailed = null)
        {
            Placement = placement;
            OnCompleted = onCompleted;
            OnFailed = onFailed;
        }
    }
}
