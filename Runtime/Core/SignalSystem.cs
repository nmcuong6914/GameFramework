using System;
using System.Collections.Generic;

public abstract class Signal
{
    // Marker base class, no Name property needed
}

// Example of a custom signal with data:
public class MyCustomSignal : Signal
{
    public int SomeValue;
    public MyCustomSignal(int someValue) { SomeValue = someValue; }
}

public class SignalBus
{
    private readonly Dictionary<Type, Action<Signal>> listeners = new Dictionary<Type, Action<Signal>>();
    // Track wrappers for each callback to enable proper unsubscription
    private readonly Dictionary<Type, Dictionary<Delegate, Action<Signal>>> wrapperMap = new Dictionary<Type, Dictionary<Delegate, Action<Signal>>>();

    public void Subscribe<T>(Action<T> callback) where T : Signal
    {
        Type type = typeof(T);
        Action<Signal> wrapper = (signal) => callback((T)signal);
        
        // Store the wrapper in the map for later unsubscription
        if (!wrapperMap.ContainsKey(type))
            wrapperMap[type] = new Dictionary<Delegate, Action<Signal>>();
        wrapperMap[type][callback] = wrapper;
        
        if (listeners.ContainsKey(type))
            listeners[type] += wrapper;
        else
            listeners[type] = wrapper;
    }

    public void Unsubscribe<T>(Action<T> callback) where T : Signal
    {
        Type type = typeof(T);
        
        // Find and remove the specific wrapper for this callback
        if (wrapperMap.ContainsKey(type) && wrapperMap[type].ContainsKey(callback))
        {
            Action<Signal> wrapper = wrapperMap[type][callback];
            if (listeners.ContainsKey(type))
            {
                listeners[type] -= wrapper;
                // Remove the type entry if no more listeners
                if (listeners[type] == null || listeners[type].GetInvocationList().Length == 0)
                {
                    listeners.Remove(type);
                }
            }
            wrapperMap[type].Remove(callback);
            
            // Clean up empty wrapper map entry
            if (wrapperMap[type].Count == 0)
            {
                wrapperMap.Remove(type);
            }
        }
    }

    public void Fire(Signal signal)
    {
        Type type = signal.GetType();
        if (listeners.ContainsKey(type))
        {
            listeners[type]?.Invoke(signal);
        }
    }
}
