namespace GameFramework.Audio
{
    /// <summary>
    /// Interface providing game-specific audio settings (music and sound status) for the SoundManager.
    /// Implemented by PlayerDataManager or other classes in the main game project.
    /// </summary>
    public interface IAudioSettingsProvider
    {
        bool IsMusicEnabled { get; }
        bool IsSoundEnabled { get; }
    }
}
