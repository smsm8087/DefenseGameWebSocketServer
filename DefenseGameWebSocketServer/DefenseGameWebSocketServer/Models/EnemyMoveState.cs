using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Models.DataModels;

namespace DefenseGameWebSocketServer.Models
{
    public class EnemyMoveState : IEnemyFSMState
    {
        public void Enter(Enemy enemy)
        {
            Console.WriteLine($"[Enemy {enemy.id}] → Move 상태 진입");
            enemy.OnBroadcastRequired?.Invoke(new EnemyBroadcastEvent(
                EnemyState.Move,
                enemy,
                new EnemyChangeStateMessage(enemy.id,"idle")
            ));
        }

        public void Update(Enemy enemy, float deltaTime)
        {
            if (!enemy.IsAlive) return;

            // 플레이어 타겟형이면 어그로 업데이트
            if (enemy.targetType == TargetType.Player && PlayerManager.Instance != null)
            {
                var players = PlayerManager.Instance.GetAllPlayers().ToArray();
                if (players.Length > 0)
                {
                    enemy.UpdateAggro(players);
                    if (enemy.AggroTarget != null)
                    {
                        enemy.targetX = enemy.AggroTarget.x;
                        enemy.targetY = enemy.AggroTarget.y;
                    }
                }
            }

            float dirX = enemy.targetX - enemy.x;
            float dirY = enemy.targetY - enemy.y;
            float len = MathF.Sqrt(dirX * dirX + dirY * dirY);
            // 속도 적용
            enemy.x += dirX / len * enemy.currentSpeed * deltaTime;
            //enemy.y += dirY / len * enemy.currentSpeed * deltaTime;

            if (enemy.targetType == TargetType.SharedHp)
            {
                var sharedHpData = GameDataManager.Instance.GetData<SharedData>("shared_data", enemy.waveData.shared_hp_id);
                if (len <= sharedHpData.radius)
                {
                    // 목표 지점 도달 시 Attack 상태로 전환
                    enemy.ChangeState(EnemyState.Attack);
                    return;
                }
            } else
            {
                //"player" 타입의 적은 플레이어와의 거리 계산
                if(len <= enemy.enemyBaseData.aggro_radius)
                {
                    enemy.ChangeState(EnemyState.RangedAttack);
                }
            }
        }

        public void Exit(Enemy enemy)
        {
            Console.WriteLine($"[Enemy {enemy.id}] → Move 상태 종료");
        }

        public EnemyState GetStateType() => EnemyState.Move;
    }
}
