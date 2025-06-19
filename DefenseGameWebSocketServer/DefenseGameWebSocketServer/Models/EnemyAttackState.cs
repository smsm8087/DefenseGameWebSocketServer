using static System.Net.Mime.MediaTypeNames;

namespace DefenseGameWebSocketServer.Models
{
    public class EnemyAttackState : IEnemyFSMState
    {
        private DateTime _lastAttackTime; 

        public void Enter(Enemy enemy)
        {
            _lastAttackTime = DateTime.MinValue;
            Console.WriteLine($"[Enemy {enemy.id}] → Attack 상태 진입");
        }

        public void Update(Enemy enemy, float deltaTime)
        {
            if (!enemy.IsAlive)
            {
                enemy.ChangeState(EnemyState.Dead);
                return;
            }

            if ((DateTime.UtcNow - _lastAttackTime).TotalSeconds >= 1.0f)
            {
                Console.WriteLine($"[Enemy {enemy.id}] → 공격 시전!");
                enemy.OnBroadcastRequired?.Invoke(new EnemyBroadcastEvent(
                    EnemyState.Attack,
                    enemy,
                    new EnemyAttackMessage(enemy.id)
                ));
                _lastAttackTime = DateTime.UtcNow;
            }
        }

        public void Exit(Enemy enemy)
        {
            Console.WriteLine($"[Enemy {enemy.id}] → Attack 상태 종료");
        }

        public EnemyState GetStateType() => EnemyState.Attack;
    }
}
