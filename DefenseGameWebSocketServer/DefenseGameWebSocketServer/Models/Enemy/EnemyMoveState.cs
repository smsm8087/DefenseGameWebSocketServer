using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Models.DataModels;

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

    public void Update(Enemy enemy, float deltaTime, PlayerManager playerManager)
    {
        if (!enemy.IsAlive) return;


        switch(enemy.targetType)
        {
            case TargetType.None:
                // 타겟이 없으면 이동하지 않음
                return;
            case TargetType.SharedHp:
                {
                    // 공유 HP 타겟은 목표 지점으로 이동
                    float dirX = enemy.targetX - enemy.x;
                    float dirY = enemy.targetY - enemy.y;
                    float len = MathF.Sqrt(dirX * dirX + dirY * dirY);
                    // 속도 적용
                    enemy.x += dirX / len * enemy.currentSpeed * deltaTime;
                    enemy.y = enemy.baseY;
                    var sharedHpData = GameDataManager.Instance.GetData<SharedData>("shared_data", enemy.waveData.shared_hp_id);
                    if (len <= sharedHpData.radius)
                    {
                        // 목표 지점 도달 시 Attack 상태로 전환
                        enemy.ChangeState(EnemyState.Attack);
                        return;
                    }
                }
                break;
            case TargetType.Player:
                {
                    // 플레이어 타겟형은 플레이어의 위치로 이동
                    var players = playerManager.GetAllPlayers().ToArray();
                    if (players.Length > 0)
                    {
                        enemy.UpdateAggro(players);
                        if (enemy.AggroTarget != null)
                        {
                            enemy.targetX = enemy.AggroTarget.x;
                            enemy.targetY = enemy.AggroTarget.y;

                            float dirX = enemy.targetX - enemy.x;
                            float dirY = enemy.targetY - enemy.y;
                            float len = MathF.Sqrt(dirX * dirX + dirY * dirY);
                            // 속도 적용
                            enemy.x += dirX / len * enemy.currentSpeed * deltaTime;

                            enemy.UpdateFloating(deltaTime);

                            enemy.y += enemy.floatYOffset;
                            enemy.y = MathF.Max(enemy.y, 0); // 바닥보다 아래로 내려가지 않도록
                            enemy.y = MathF.Min(enemy.y, 2.5f); // 최대 높이 제한

                            //"player" 타입의 적은 플레이어와의 거리 계산
                            if (len <= enemy.enemyBaseData.aggro_radius)
                            {
                                enemy.ChangeState(EnemyState.RangedAttack);
                            }
                        }
                    }
                }
                break;
        }
    }

    public void Exit(Enemy enemy)
    {
        Console.WriteLine($"[Enemy {enemy.id}] → Move 상태 종료");
    }

    public EnemyState GetStateType() => EnemyState.Move;
}
