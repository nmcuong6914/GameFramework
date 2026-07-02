using UnityEngine;

public class DIExample : MonoBehaviour
{
    void Awake()
    {
        // Register SignalBus as a singleton
        ServiceLocator.Register<SignalBus>(new SignalBus());

        // Register a server (example)
        ServiceLocator.Register<IMyServer>(new MyServer());
    }

    void Start()
    {
        // Resolve and use SignalBus
        var signalBus = ServiceLocator.Resolve<SignalBus>();
        signalBus.Subscribe<MyCustomSignal>(OnCustomSignal);
        signalBus.Fire(new MyCustomSignal(42));
    }

    void OnCustomSignal(MyCustomSignal signal)
    {
        Debug.Log($"Received signal value: {signal.SomeValue}");
    }
}

// Example server interface and implementation
public interface IMyServer { }
public class MyServer : IMyServer { }
