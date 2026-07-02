using UnityEngine;
using System.Threading.Tasks;

[System.Serializable]
public abstract class ViewResponse
{
    public ActionType triggerActionType;
    public bool isPlaying { get; protected set; }

    protected ViewResponse(ActionType actionType)
    {
        triggerActionType = actionType;
    }

    public abstract Task PlayAsync(GameAction action);
    public abstract void Stop();
}
