using DefenseGameWebSocketServer.Model;

public class SpawnEnemyHandler
{
    private readonly IWebSocketBroadcaster _broadcaster;

    public SpawnEnemyHandler(IWebSocketBroadcaster broadcaster)
    {
        _broadcaster = broadcaster;
    }

    public async Task SendEnemyAsync(string enemyId, string enemyType, int wave)
    {
        var message = new SpawnEnemyMessage("spawn_enemy", enemyId, wave);
        await _broadcaster.BroadcastAsync(message);
    }
}