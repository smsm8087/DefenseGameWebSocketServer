
using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Models.DataModels;

public class EnemyAttackState : IEnemyFSMState
{
    public void Enter(Enemy enemy)
    {
        Console.WriteLine($"[Enemy {enemy.id}] → Attack 상태 진입");
            
        //attack 준비해라 메시지 브로드캐스트
        enemy.OnBroadcastRequired?.Invoke(new EnemyBroadcastEvent(
                EnemyState.Attack,
                enemy,
                new EnemyChangeStateMessage(enemy.id,"attack")
        ));
    }

    public void Update(Enemy enemy, float deltaTime, PlayerManager playerManager)
    {
        // 공유HP 대상이 아닌데 Attack에 남아있으면 즉시 Move로
        if (enemy.targetType != TargetType.SharedHp)
        {
            enemy.ChangeState(EnemyState.Move);
            return;
        }

        // 공유HP 사거리 밖이면 Move로 복귀
        var sharedHpData = GameDataManager.Instance.GetData<SharedData>("shared_data", enemy.waveData.shared_hp_id);
        float dx = enemy.targetX - enemy.x;
        float dy = enemy.targetY - enemy.y;
        float len = MathF.Sqrt(dx * dx + dy * dy);

        if (len > sharedHpData.radius)
        {
            enemy.ChangeState(EnemyState.Move);
            return;
        }
    }

    public void Exit(Enemy enemy)
    {
        Console.WriteLine($"[Enemy {enemy.id}] → Attack 상태 종료");
    }

    public EnemyState GetStateType() => EnemyState.Attack;
}
