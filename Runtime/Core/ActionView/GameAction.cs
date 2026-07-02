using UnityEngine;

[System.Serializable]
public abstract class GameAction
{
    public ActionType actionType;
    public Vector3 worldPosition;
    public Quaternion rotation = Quaternion.identity;
    public Transform targetTransform;
    public float timestamp;

    protected GameAction(ActionType type)
    {
        actionType = type;
        timestamp = Time.time;
    }

    public abstract GameAction Execute();
}

public enum ActionType
{
    None,
    BlockDestroy,
    BlockMove,
    BlockMerge,
    LevelComplete,
    PowerUpActivate,
    ComboTrigger,
    PassedGate,
    UnlockKey
}
