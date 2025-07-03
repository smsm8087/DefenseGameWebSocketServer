using System.Numerics;

namespace DefenseGameWebSocketServer.Models
{
    public class EnemyRangedAttackState : IEnemyFSMState
    {
        public void Enter(Enemy enemy)
        {
            Console.WriteLine($"[Enemy {enemy.id}] → Attack 상태 진입");
            
            //attack 준비해라 메시지 브로드캐스트
            enemy.OnBroadcastRequired?.Invoke(new EnemyBroadcastEvent(
                    EnemyState.RangedAttack,
                    enemy,
                    new EnemyAttackMessage(enemy.id)
            ));
        }

        public void Update(Enemy enemy, float deltaTime)
        {
            
        }

        public void Exit(Enemy enemy)
        {
            Console.WriteLine($"[Enemy {enemy.id}] → Attack 상태 종료");
        }
        private void FireBullet(Enemy enemy)
        {
            if (enemy.bulletData == null)
            {
                Console.WriteLine($"[Enemy {enemy.id}] bulletData 없음! 발사 실패");
                return;
            }

            var player = enemy.AggroTarget;
            if (player == null) return;

            Vector2 playerPosition = new Vector2(player.x, player.y);
            Vector2 enemyPosition = new Vector2(enemy.x, enemy.y);

            //var dir = (playerPosition - enemyPosition).Normalized();

            //var bulletMsg = new EnemyBulletMessage(
            //    "enemy_bullet",
            //    enemy.id,
            //    enemy.Position,
            //    dir,
            //    enemy.Attack
            //);

            //enemy.OnBroadcastRequired?.Invoke(new EnemyBroadcastEvent(
            //    EnemyState.Attack,
            //    enemy,
            //    bulletMsg
            //));

            Console.WriteLine($"[Enemy {enemy.id}] 총알 발사 → 대상 {player.id}");
        }

        public EnemyState GetStateType() => EnemyState.RangedAttack;
    }
}
