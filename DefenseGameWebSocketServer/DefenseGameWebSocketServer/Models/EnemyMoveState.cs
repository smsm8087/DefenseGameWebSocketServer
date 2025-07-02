using DefenseGameWebSocketServer.Manager;

namespace DefenseGameWebSocketServer.Models
{
    public class EnemyMoveState : IEnemyFSMState
    {
        public void Enter(Enemy enemy)
        {
            Console.WriteLine($"[Enemy {enemy.id}] → Move 상태 진입");
        }

        public void Update(Enemy enemy, float deltaTime)
        {
            if (!enemy.IsAlive)
            {
                enemy.ChangeState(EnemyState.Dead);
                return;
            }

            float dirX = enemy.targetX - enemy.x;
            float dirY = enemy.targetY - enemy.y;
            float len = MathF.Sqrt(dirX * dirX + dirY * dirY);

            if(len > enemy.waveData.shared_hp_radius)
            {
                dirX /= len;
                dirY /= len;
                // 속도 적용
                enemy.x += dirX * enemy.currentSpeed * deltaTime;
                enemy.y += dirY * enemy.currentSpeed * deltaTime;
            }
            else
            {
                // 목표 지점 도달 시 Attack 상태로 전환
                enemy.ChangeState(EnemyState.Attack);
            }
        }

        public void Exit(Enemy enemy)
        {
            Console.WriteLine($"[Enemy {enemy.id}] → Move 상태 종료");
        }

        public EnemyState GetStateType() => EnemyState.Move;
    }
}
