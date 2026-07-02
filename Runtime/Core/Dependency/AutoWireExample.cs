using UnityEngine;

public class GameServer
{
    public string ServerName { get; }
    public GameServer() { ServerName = "MainServer"; }
}

public class GameManager
{
    public GameServer Server { get; }
    public GameManager(GameServer server)
    {
        Server = server;
    }
}

public class AutoWireExample : MonoBehaviour
{
    void Awake()
    {
        // Only register GameServer, not GameManager
        ServiceLocator.Register<GameServer>(new GameServer());
    }

    void Start()
    {
        // GameManager will be auto-wired with GameServer
        var manager = ServiceLocator.Resolve<GameManager>();
        Debug.Log($"GameManager's server: {manager.Server.ServerName}");
    }
}
