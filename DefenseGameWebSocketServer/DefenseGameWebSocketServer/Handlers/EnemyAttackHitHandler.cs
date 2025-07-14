using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using Microsoft.AspNetCore.SignalR.Protocol;
using System;
using System.Text.Json;

namespace DefenseGameWebSocketServer.Handlers
{
    public class EnemyAttackHitMessage
    {
        public string type = "enemy_attack_hit";
        public string enemyId {get; set;}
    }
    public class EnemyAttackHitHandler
    {
        public async Task HandleAsync(string rawMessage, IWebSocketBroadcaster broadcaster, SharedHpManager _sharedHpManager, EnemyManager _enemyManager, RevivalManager _revivalManager = null)
        {
            var msg = JsonSerializer.Deserialize<EnemyAttackHitMessage>(rawMessage);
            if (msg == null) return;

            //해당 적 공격력 가져오기
            var targetEnemy = _enemyManager._enemies.Find(e => e.id == msg.enemyId);
            if( targetEnemy == null)
            {
                Console.WriteLine($"[EnemyAttackHitHandler] 적 {msg.enemyId} 정보 없음");
                return;
            }
            if (targetEnemy.targetType == TargetType.SharedHp)
            {
                // Shared HP 감소
                _sharedHpManager.TakeDamage(targetEnemy.currentAttack);
                // Shared HP 상태 브로드캐스트
                var hpMessage = new SharedHpMessage(_sharedHpManager.getHpStatus().Item1, _sharedHpManager.getHpStatus().Item2);
                await broadcaster.BroadcastAsync(hpMessage);
                Console.WriteLine($"[EnemyAttackHitHandler] 공유 HP 감소됨, 현재 HP: {_sharedHpManager.getHpStatus().Item1}");
            }
            else if (targetEnemy.targetType == TargetType.Player)
            {
                // 플레이어 공격
                if(targetEnemy.AggroTarget != null)
                {
                    //총알 발사
                    await BulletManager.Instance.SpawnBullet(targetEnemy, targetEnemy.AggroTarget);
                    
                    //부활 중단 체크
                    if (_revivalManager != null && targetEnemy.AggroTarget.IsBeingRevived)
                    {
                        await _revivalManager.CancelRevival(targetEnemy.AggroTarget.id, "hit_during_revival");
                        Console.WriteLine($"[EnemyAttackHitHandler] {targetEnemy.AggroTarget.id} 부활 중 피격으로 부활 중단");
                    }


                    targetEnemy.OnAttackPerformed(); // 공격 횟수 증가 및 상태 변경
                }
            }
        }
    }
}
