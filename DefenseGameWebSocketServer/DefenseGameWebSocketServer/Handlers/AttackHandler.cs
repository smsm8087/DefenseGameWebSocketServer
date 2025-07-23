using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.MessageModel;
using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models;
using System.Text.Json;

public class AttackHandler
{
    private readonly EnemyManager _enemyManager;
    private readonly PlayerManager _playerManager;
    private readonly WaveScheduler _waveScheduler;

    public AttackHandler(EnemyManager enemyManager, PlayerManager playerManager, WaveScheduler waveScheduler)
    {
        _enemyManager = enemyManager;
        _playerManager = playerManager;
        _waveScheduler = waveScheduler;
    }

    public async Task HandleAsync(string playerId, string rawMessage, IWebSocketBroadcaster broadcaster)
    {
        var msg = JsonSerializer.Deserialize<PlayerAttackRequest>(rawMessage);
        if (msg == null)
        {
            Room room = RoomManager.Instance.GetRoomByPlayerId(playerId);
            LogManager.Error($"[AttackHandler] 잘못된 메시지 수신: {rawMessage}", room.RoomCode, playerId);
            return;
        }
        
        // 적 명중 여부 확인
        int hitEnemyCount = await _enemyManager.CheckDamaged(_playerManager, msg);
        var bossFsm = _waveScheduler.GetBossFSM();
        if(bossFsm != null)
        {
            await bossFsm.CheckDamaged(_playerManager, msg);
        }
        // 결과 로깅
        if (hitEnemyCount > 0)
        {
            (float,float) ult_gauges = _playerManager.addUltGauge(playerId);
            var successResponse = new UpdateUltGaugeMessage(ult_gauges.Item1, ult_gauges.Item2);

            await broadcaster.SendToAsync(playerId, successResponse);
        }
    }
}
