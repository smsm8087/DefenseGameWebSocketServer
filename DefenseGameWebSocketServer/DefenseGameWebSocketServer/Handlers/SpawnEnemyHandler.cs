using DefenseGameWebSocketServer.Model;
using System.Collections.Concurrent;
using System.Text.Json;

public class SpawnEnemyHandler : IMessageHandler
{
    public async Task HandleAsync(
        string playerId,
        string rawMessage,
        IWebSocketBroadcaster broadcaster,
        ConcurrentDictionary<string, (float x, float y)> playerPositions
    )
    {
        var msg = JsonSerializer.Deserialize<SpawnEnemyMessage>(rawMessage);
        if (msg == null) return;

        var response = new SpawnEnemyMessage("spawn_enemy", msg.enemyId, msg.wave);
        await broadcaster.BroadcastAsync(response);
    }

    // 서버 내부에서 수동 호출할 때 사용할 수 있음
    public async Task SendEnemyAsync(string enemyId, int wave, IWebSocketBroadcaster broadcaster)
    {
        var response = new SpawnEnemyMessage("spawn_enemy", enemyId, wave);
        await broadcaster.BroadcastAsync(response);
    }
}