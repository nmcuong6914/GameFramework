using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System;

public class ActionViewManager
{

    [System.Serializable]
    public class ActionViewMapping
    {
        public ActionType actionType;
        public List<ViewResponse> viewResponses = new List<ViewResponse>();
        public float delay = 0f; // Delay before triggering view responses
        public bool playSequentially = false; // If true, play responses one after another
    }

    [SerializeField] private List<ActionViewMapping> actionMappings = new List<ActionViewMapping>();
    private Dictionary<ActionType, ActionViewMapping> mappingDict = new Dictionary<ActionType, ActionViewMapping>();
    
    // Type-to-Type mapping dictionary for new async system
    private Dictionary<Type, Type> actionViewTypeMappings = new Dictionary<Type, Type>();

    public ActionViewManager()
    {
        InitializeMappings();
    }

    private void InitializeMappings()
    {
        mappingDict.Clear();
        foreach (var mapping in actionMappings)
        {
            mappingDict[mapping.actionType] = mapping;
        }
    }

    /// <summary>
    /// Register a mapping between action type and view response type (new async system)
    /// </summary>
    public void RegisterActionViewMapping<TAction, TViewResponse>()
        where TAction : GameAction
        where TViewResponse : ViewResponse
    {
        actionViewTypeMappings[typeof(TAction)] = typeof(TViewResponse);
        Debug.Log($"Registered action-view mapping: {typeof(TAction).Name} -> {typeof(TViewResponse).Name}");
    }

    /// <summary>
    /// Process an action asynchronously using type-to-type mapping
    /// </summary>
    public async Task ProcessActionAsync(GameAction action)
    {
        if (action == null) return;

        var actionType = action.GetType();
        
        if (!actionViewTypeMappings.ContainsKey(actionType))
        {
            return;
        }

        var viewResponseType = actionViewTypeMappings[actionType];
        
        // Create instance of the view response using ServiceLocator or reflection
        var viewResponse = CreateViewResponse(viewResponseType);
        
        if (viewResponse != null)
        {
            await viewResponse.PlayAsync(action);
        }
    }

    /// <summary>
    /// Process multiple actions sequentially
    /// </summary>
    public async Task ProcessActionsAsync(List<GameAction> actions)
    {
        if (actions == null || actions.Count == 0) return;

        foreach (var action in actions)
        {
            await ProcessActionAsync(action);
        }
    }

    /// <summary>
    /// Create view response instance using reflection or ServiceLocator
    /// </summary>
    private ViewResponse CreateViewResponse(Type viewResponseType)
    {
        try
        {
            // Always create new instance to avoid sharing state between concurrent actions
            // This is critical for ViewResponses with state like 'isPlaying' flags
            var instance = Activator.CreateInstance(viewResponseType) as ViewResponse;
            return instance;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create view response of type {viewResponseType.Name}: {ex.Message}");
            return null;
        }
    }

    // Legacy methods for backward compatibility - converted to async
    public async Task ProcessActionLegacyAsync(GameAction action)
    {
        if (action == null) return;

        await ProcessActionAsync(action);
    }

    public void AddViewResponse(ActionType actionType, ViewResponse response)
    {
        if (!mappingDict.ContainsKey(actionType))
        {
            var newMapping = new ActionViewMapping { actionType = actionType };
            actionMappings.Add(newMapping);
            mappingDict[actionType] = newMapping;
        }

        mappingDict[actionType].viewResponses.Add(response);
    }

    public void RemoveViewResponse(ActionType actionType, ViewResponse response)
    {
        if (mappingDict.ContainsKey(actionType))
        {
            mappingDict[actionType].viewResponses.Remove(response);
        }
    }
}
