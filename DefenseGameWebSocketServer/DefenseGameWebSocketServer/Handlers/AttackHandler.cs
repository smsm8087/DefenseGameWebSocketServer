using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.MessageModel;
using System.Text.Json;

public class AttackHandler
{
    private readonly List<Enemy> _enemies;
    private readonly PlayerManager _playerManager;

    public AttackHandler(List<Enemy> enemies, PlayerManager playerManager)
    {
        _enemies = enemies;
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

        //적 리스트 돌면서 박스 충돌 체크
        var dmgMsg = new EnemyDamagedMessage();

        List<Enemy> deadEnemyList = new List<Enemy>();
        foreach (var enemy in _enemies)
        {
            if (IsEnemyInAttackBox(enemy, msg))
            {
                enemy.TakeDamage(playerAttackPower);
                if(enemy.Hp <= 0)
                {
                    deadEnemyList.Add(enemy);
                    Console.WriteLine($"[AttackHandler] 적 {enemy.Id} 처치됨");
                }
                else
                {
                    Console.WriteLine($"[AttackHandler] 적 {enemy.Id} 남은 HP: {enemy.Hp}");
                }
                Console.WriteLine($"[AttackHandler] Enemy {enemy.Id} 명중! {playerAttackPower} 데미지");

                //데미지 메시지 브로드캐스트
                dmgMsg.damagedEnemies.Add(new EnemyDamageInfo
                {
                    enemyId = enemy.Id,
                    currentHp = enemy.Hp,
                    maxHp = enemy.MaxHp,
                    damage = playerAttackPower
                });
            }
        }
        if (dmgMsg.damagedEnemies.Count > 0)
        {
            await broadcaster.BroadcastAsync(dmgMsg);
        }

        foreach (var enemy in deadEnemyList)
        {
            //적 리스트에서 제거
            _enemies.RemoveAll(e => e.Id == enemy.Id);
        }
        //죽은 적 처리
        var deadEnemiesList = deadEnemyList.Select(e => e.Id).ToList();
        if(deadEnemiesList.Count > 0)
        {
            var dieMsg = new EnemyDieMessage(deadEnemiesList);
            await broadcaster.BroadcastAsync(dieMsg);
        }
    }

    private bool IsEnemyInAttackBox(Enemy enemy, PlayerAttackRequest msg)
    {
        float halfWidth = msg.attackBoxWidth / 2f;
        float halfHeight = msg.attackBoxHeight / 2f;

        return enemy.X >= (msg.attackBoxCenterX - halfWidth) &&
                enemy.X <= (msg.attackBoxCenterX + halfWidth) &&
                enemy.Y >= (msg.attackBoxCenterY - halfHeight) &&
                enemy.Y <= (msg.attackBoxCenterY + halfHeight);
    }
}
