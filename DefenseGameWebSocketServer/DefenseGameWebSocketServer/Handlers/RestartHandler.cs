using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public class RestartHandler
{
    public async Task HandleAsync(
        string playerId,
        IWebSocketBroadcaster broadcaster,
        Func<bool> restartGameFunc
    )
    {
        restartGameFunc?.Invoke();

        var response = new RestartMessage("restart", playerId);
        await broadcaster.BroadcastAsync(response);
    }
}