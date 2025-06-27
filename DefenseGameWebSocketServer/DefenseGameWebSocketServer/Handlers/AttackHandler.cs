using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.MessageModel;
using DefenseGameWebSocketServer.Model;
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
        
        // 적 명중 여부 확인
        int hitEnemyCount = await _enemyManager.CheckDamaged(_playerManager, msg);

        // 결과 로깅
        if (hitEnemyCount > 0)
        {
            (float,float) ult_gauges = _playerManager.addUltGauge(playerId);
            var successResponse = new UpdateUltGaugeMessage(ult_gauges.Item1, ult_gauges.Item2);

            await broadcaster.SendToAsync(playerId, successResponse);
        }
    }
}
