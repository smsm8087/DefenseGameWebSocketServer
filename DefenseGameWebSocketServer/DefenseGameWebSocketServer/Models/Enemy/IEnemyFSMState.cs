public interface IEnemyFSMState
{
    void Enter(Enemy enemy);
    void Update(Enemy enemy, float deltaTime);
    void Exit(Enemy enemy);
    EnemyState GetStateType();
}
