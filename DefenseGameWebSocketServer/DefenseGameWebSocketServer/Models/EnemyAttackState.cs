using static System.Net.Mime.MediaTypeNames;

namespace DefenseGameWebSocketServer.Models
{
    public class EnemyAttackState : IEnemyFSMState
    {
        public void Enter(Enemy enemy)
        {
            Console.WriteLine($"[Enemy {enemy.id}] → Attack 상태 진입");
            
            //attack 준비해라 메시지 브로드캐스트
            enemy.OnBroadcastRequired?.Invoke(new EnemyBroadcastEvent(
                    EnemyState.Attack,
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

        public EnemyState GetStateType() => EnemyState.Attack;
    }
}
