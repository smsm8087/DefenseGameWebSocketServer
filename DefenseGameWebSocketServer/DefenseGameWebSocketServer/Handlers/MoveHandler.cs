using DefenseGameWebSocketServer.Model;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public class MoveHandler : IMessageHandler
{
    public async Task HandleAsync(
        string playerId, 
        string rawMessage,
        IWebSocketBroadcaster broadcaster,
        ConcurrentDictionary<string, (float x, float y)> playerPositions
    )
    {
        var msg = JsonSerializer.Deserialize<MoveMessage>(rawMessage);
        if (msg == null) return;

        playerPositions[playerId] = (msg.x, msg.y);

        var response = new MoveMessage("move", playerId, msg.x, msg.y, msg.isJumping, msg.isRunning);
        await broadcaster.BroadcastAsync(response);
    }
}