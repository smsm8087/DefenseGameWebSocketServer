using DefenseGameWebSocketServer.Manager;

public class EnemyDeadState : IEnemyFSMState
{
    public void Enter(Enemy enemy)
    {
        Console.WriteLine($"[Enemy {enemy.id}] → Dead 상태 진입");
        enemy.OnBroadcastRequired?.Invoke(new EnemyBroadcastEvent(
            EnemyState.Dead,
            enemy
        ));
    }

    public void Update(Enemy enemy, float deltaTime, PlayerManager playerManager)
    {
        // 죽은 상태 유지
    }

    public void Exit(Enemy enemy) { }

    public EnemyState GetStateType() => EnemyState.Dead;
}
