using DefenseGameWebSocketServer.Models;

public enum EnemyState
{
    Move,
    Attack,
    Dead
}

public class Enemy
{
    public string id;
    public float x;
    public float y;
    public float speed;
    public int hp = 100;
    public int maxHp = 100;
    public float targetX;
    public float targetY;
    public EnemyState state;
    private IEnemyFSMState _currentState;
    public float targetRadius = 2.125f;
    public float attackDamage = 1f;

    //fsm
    public EnemyMoveState moveState = new EnemyMoveState();
    public EnemyAttackState attackState = new EnemyAttackState();
    public EnemyDeadState deadState = new EnemyDeadState();
    public Action<EnemyBroadcastEvent> OnBroadcastRequired;


    public bool IsAlive => hp > 0;

    public Enemy(string id, string type, float startX, float startY, float targetX, float targetY, float speed = 3f)
    {
        this.id = id;
        this.x = startX;
        this.y = startY;
        this.targetX = targetX;
        this.targetY = targetY;
        this.speed = speed;
        this.hp = this.maxHp = 100;
        ChangeState(EnemyState.Move);
    }
    public void UpdateFSM(float deltaTime)
    {
        _currentState?.Update(this, deltaTime);
    }

    public void ChangeState(EnemyState newState)
    {
        _currentState?.Exit(this);

        switch (newState)
        {
            case EnemyState.Move:
                _currentState = moveState;
                break;
            case EnemyState.Attack:
                _currentState = attackState;
                break;
            case EnemyState.Dead:
                _currentState = deadState;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }
        _currentState.Enter(this);
        state = _currentState.GetStateType();
    }
    public void TakeDamage(int dmg)
    {
        hp -= dmg;
        if (hp < 0) hp = 0;
    }
}