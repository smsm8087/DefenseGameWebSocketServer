using DefenseGameWebSocketServer.Model;

public class RestartHandler
{
    public async Task HandleAsync(
        string playerId,
        IWebSocketBroadcaster broadcaster,
        Func<bool> restartGameFunc
    )
    {
        restartGameFunc?.Invoke();

        var response = new RestartMessage(playerId);
        await broadcaster.BroadcastAsync(response);
    }
}