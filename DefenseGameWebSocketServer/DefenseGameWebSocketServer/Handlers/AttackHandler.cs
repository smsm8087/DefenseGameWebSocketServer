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
    Console.WriteLine($"[AttackHandler] 공격 요청 수신: {playerId}");
    
    var msg = JsonSerializer.Deserialize<PlayerAttackRequest>(rawMessage);
    if (msg == null)
    {
        Console.WriteLine("[AttackHandler] 잘못된 메시지 수신");
        return;
    }

    // 공격 박스 정보 로깅
    Console.WriteLine($"[AttackHandler] 공격 박스 - 중심: ({msg.attackBoxCenterX:F2}, {msg.attackBoxCenterY:F2}), 크기: ({msg.attackBoxWidth:F2}, {msg.attackBoxHeight:F2})");

    _playerManager.TryGetPlayer(playerId, out Player player);
    if (player == null)
    {
        Console.WriteLine($"[AttackHandler] 플레이어 {playerId} 정보 없음");
        return;
    }

    int playerAttackPower = player.AttackPower;
    Console.WriteLine($"[AttackHandler] 플레이어 {playerId} 공격력: {playerAttackPower}");

    // 적 명중 여부 확인
    bool hitEnemy = await _enemyManager.CheckDamaged(playerAttackPower, msg);

    // 결과 로깅
    if (hitEnemy)
    {
        var successResponse = new
        {
            type = "attack_success",
            playerId = playerId
        };

        await broadcaster.SendToAsync(playerId, successResponse);
        Console.WriteLine($"[AttackHandler] ✅ 적 명중! ULT 증가 신호 전송: {playerId}");
    }
    else
    {
        Console.WriteLine($"[AttackHandler] ❌ 빗나감. ULT 증가 없음: {playerId}");
    }
}
}
