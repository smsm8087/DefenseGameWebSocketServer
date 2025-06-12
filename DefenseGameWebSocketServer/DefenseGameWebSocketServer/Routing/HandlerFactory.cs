using System.Collections.Concurrent;
using DefenseGameWebSocketServer.Model;

public class HandlerFactory
{
    private readonly Dictionary<string, IMessageHandler> _handlers;

    public HandlerFactory()
    {
        _handlers = new Dictionary<string, IMessageHandler>
        {
            //여기에 새로운 핸들러타입 추가
            { "move", new MoveHandler() },
            { "spawn_enemy", new SpawnEnemyHandler() }
        };
    }

    public IMessageHandler? GetHandler(string type)
    {
        return _handlers.TryGetValue(type, out var handler) ? handler : null;
    }
}