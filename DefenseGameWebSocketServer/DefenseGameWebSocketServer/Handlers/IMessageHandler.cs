using System.Collections.Concurrent;
using System.Net.WebSockets;

public interface IMessageHandler
{
    Task HandleAsync(
        string playerId, 
        string rawMessage, 
        IWebSocketBroadcaster broadCaster,
        ConcurrentDictionary<string, (float x, float y)> playerPositions
    );
}
