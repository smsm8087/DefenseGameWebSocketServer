using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.MessageModel;
using System.Text.Json;

public class AttackHandler
{
    private readonly EnemyManager _enemyManager;
    private readonly PlayerManager _playerManager;

    public AttackHandler(EnemyManager enemyManager, PlayerManager playerManager)
    {
        _enemyManager = enemyManager;
        _playerManager = playerManager;
    }

    public async Task HandleAsync(string playerId, string rawMessage, IWebSocketBroadcaster broadcaster)
    {
        var msg = JsonSerializer.Deserialize<PlayerAttackRequest>(rawMessage);
        if (msg == null)
        {
            Console.WriteLine("[AttackHandler] 잘못된 메시지 수신");
            return;
        }

        //플레이어 공격력 가져오기
        _playerManager.TryGetPlayer(playerId, out Player player);
        if (player == null)
        {
            Console.WriteLine($"[AttackHandler] 플레이어 {playerId} 정보 없음");
            return;
        }

        int playerAttackPower = player.AttackPower;
        await _enemyManager.CheckDamaged(playerAttackPower, msg);
    }
}
