namespace Analytics
{
    /// <summary>
    /// Interface to provide game-specific data for analytics enrichment.
    /// Implemented by PlayerDataManager or other classes in the main game project.
    /// </summary>
    public interface IAnalyticsDataProvider
    {
        int GetCurrentLevelIndex();
        int GetRetentionDay();
        int GetCoinCount();
        int GetLivesCount();
    }
}
