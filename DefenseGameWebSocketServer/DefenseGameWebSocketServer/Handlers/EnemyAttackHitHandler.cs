using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using System.Text.Json;

namespace DefenseGameWebSocketServer.Handlers
{
    public class EnemyAttackHitHandler
    {
        public async Task HandleAsync(string rawMessage, IWebSocketBroadcaster broadcaster, SharedHpManager _sharedHpManager, EnemyManager _enemyManager)
        {
            var msg = JsonSerializer.Deserialize<EnemyAttackMessage>(rawMessage);
            if (msg == null) return;

            //해당 적 공격력 가져오기
            var targetEnemy = _enemyManager._enemies.Find(e => e.id == msg.enemyId);
            if( targetEnemy == null)
            {
                Console.WriteLine($"[EnemyAttackHitHandler] 적 {msg.enemyId} 정보 없음");
                return;
            }
            // Shared HP 감소
            _sharedHpManager.TakeDamage(targetEnemy.attackDamage);

            // Shared HP 상태 브로드캐스트
            var hpMessage = new SharedHpMessage(_sharedHpManager.getHpStatus().Item1, _sharedHpManager.getHpStatus().Item2);
            await broadcaster.BroadcastAsync(hpMessage);

            Console.WriteLine($"[EnemyAttackHitHandler] 공유 HP 감소됨, 현재 HP: {_sharedHpManager.getHpStatus().Item1}");
        }
    }
}
