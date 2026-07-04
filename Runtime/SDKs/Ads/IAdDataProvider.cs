namespace BlockSort.Ads
{
    /// <summary>
    /// Interface providing game-specific data and callbacks for the AdsManager.
    /// Implemented by PlayerDataManager or other classes in the main game project.
    /// </summary>
    public interface IAdDataProvider
    {
        bool IsPaid { get; }
        int CurrentLevelIndex { get; }
        int GetAdShowCountToday(string placement);
        void IncrementAdShowCount(string placement);
        void ResetDailyAdCounters();
    }
}
