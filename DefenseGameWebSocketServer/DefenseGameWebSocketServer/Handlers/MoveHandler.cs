using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using System.Text.Json;

public class MoveHandler
{
    public async Task HandleAsync(
        string rawMessage,
        IWebSocketBroadcaster broadcaster,
        PlayerManager playerManager
    )
    {
        var msg = JsonSerializer.Deserialize<MoveMessage>(rawMessage);
        if (msg == null) return;

        playerManager.setPlayerPosition(msg.playerId, msg.x, msg.y);

        var response = new MoveMessage(msg.playerId, msg.x, msg.y, msg.isJumping, msg.isRunning);
        await broadcaster.BroadcastAsync(response);
    }
}