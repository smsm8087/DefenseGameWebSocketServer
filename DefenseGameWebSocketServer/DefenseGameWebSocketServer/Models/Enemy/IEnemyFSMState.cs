using DefenseGameWebSocketServer.Manager;

public interface IEnemyFSMState
{
    void Enter(Enemy enemy);
    void Update(Enemy enemy, float deltaTime, PlayerManager playerManager);
    void Exit(Enemy enemy);
    EnemyState GetStateType();
}
