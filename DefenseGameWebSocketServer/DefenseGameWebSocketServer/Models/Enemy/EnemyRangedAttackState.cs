using DefenseGameWebSocketServer.Manager;
using System.Numerics;

public class EnemyRangedAttackState : IEnemyFSMState
{
    public void Enter(Enemy enemy)
    {
        Console.WriteLine($"[Enemy {enemy.id}] → Attack 상태 진입");

        //attack 준비해라 메시지 브로드캐스트
        enemy.OnBroadcastRequired?.Invoke(new EnemyBroadcastEvent(
                EnemyState.RangedAttack,
                enemy,
                new EnemyChangeStateMessage(enemy.id, "attack", enemy.AggroTarget)
        ));
    }

    public void Update(Enemy enemy, float deltaTime, PlayerManager playerManager)
    {
        if (enemy.AggroTarget == null)
        {
            var players = playerManager.GetAllPlayers().ToArray();
            if (players.Length > 0)
            {
                enemy.UpdateAggro(players);
                enemy.ChangeState(EnemyState.Move);
                return;
            }
        }

        float dx = enemy.AggroTarget.x - enemy.x;
        float dy = enemy.AggroTarget.y - enemy.y;
        float distanceSqr = dx * dx + dy * dy;
        float radius = enemy.enemyBaseData.aggro_radius;

        if (distanceSqr > radius * radius && !enemy.isRangedAttackPending)
        {
            enemy.ChangeState(EnemyState.Move);
            return;
        }
    }

    public void Exit(Enemy enemy)
    {
        Console.WriteLine($"[Enemy {enemy.id}] → Attack 상태 종료");
    }
    public EnemyState GetStateType() => EnemyState.RangedAttack;
}
