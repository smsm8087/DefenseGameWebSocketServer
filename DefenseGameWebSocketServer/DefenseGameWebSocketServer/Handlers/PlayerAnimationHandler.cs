using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using System.Text.Json;

public class PlayerAnimationHandler
{
    public async Task HandleAsync(
        string playerId,
        string rawMessage,
        IWebSocketBroadcaster broadcaster
    )
    {
        var msg = JsonSerializer.Deserialize<PlayerAnimationMessage>(rawMessage);
        if (msg == null) return;

        var response = new PlayerAnimationMessage(playerId, msg.animation);
        await broadcaster.BroadcastAsync(response);
    }
}