
using DefenseGameWebSocketServer.Manager;

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
            
    }

    public void Exit(Enemy enemy)
    {
        Console.WriteLine($"[Enemy {enemy.id}] → Attack 상태 종료");
    }

    public EnemyState GetStateType() => EnemyState.Attack;
}
