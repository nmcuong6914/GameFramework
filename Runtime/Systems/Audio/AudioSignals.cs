using UnityEngine;

/// <summary>
/// Types of audio that can be played
/// </summary>
public enum GameAudioType
{
    Music,
    SFX,
    UI
}

/// <summary>
/// Generic signal for playing audio
/// Decouples the sound system from specific game events
/// </summary>
public class PlayAudioSignal : Signal
{
    public string AudioKey { get; private set; }
    public GameAudioType AudioType { get; private set; }
    public float Volume { get; private set; }

    public PlayAudioSignal(string audioKey, GameAudioType audioType, float volume = 1.0f)
    {
        AudioKey = audioKey;
        AudioType = audioType;
        Volume = volume;
    }
}

/// <summary>
/// Signal to stop specific audio
/// </summary>
public class StopAudioSignal : Signal
{
    public string AudioKey { get; private set; }
    public GameAudioType AudioType { get; private set; }

    public StopAudioSignal(string audioKey, GameAudioType audioType)
    {
        AudioKey = audioKey;
        AudioType = audioType;
    }
}

/// <summary>
/// Signal to control audio volume
/// </summary>
public class SetVolumeSignal : Signal
{
    public GameAudioType AudioType { get; private set; }
    public float Volume { get; private set; }

    public SetVolumeSignal(GameAudioType audioType, float volume)
    {
        AudioType = audioType;
        Volume = volume;
    }
}

/// <summary>
/// Signal to mute/unmute audio
/// </summary>
public class SetMuteSignal : Signal
{
    public GameAudioType AudioType { get; private set; }
    public bool Mute { get; private set; }

    public SetMuteSignal(GameAudioType audioType, bool mute)
    {
        AudioType = audioType;
        Mute = mute;
    }
}
