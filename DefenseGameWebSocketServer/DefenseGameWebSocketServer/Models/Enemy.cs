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
    public float speed = 2f;
    public int hp = 100;
    public int maxHp = 100;
    public float targetX;
    public float targetY;
    public EnemyState state;
    private IEnemyFSMState _currentState;
    public float targetRadius = 2.125f;
    public float attackDamage = 1f;
    public float baseWidth = 1f;   
    public float baseHeight = 1f;  
    public float scale = 1f;
    public float offSetX = 1f;
    public float offSetY = 1f;

    //fsm
    public EnemyMoveState moveState = new EnemyMoveState();
    public EnemyAttackState attackState = new EnemyAttackState();
    public EnemyDeadState deadState = new EnemyDeadState();
    public Action<EnemyBroadcastEvent> OnBroadcastRequired;


    public bool IsAlive => hp > 0;

    public Enemy(string id, string type, float startX, float startY, float targetX, float targetY)
    {
        this.id = id;
        this.x = startX;
        this.y = startY;
        this.targetX = targetX;
        this.targetY = targetY;
        this.hp = this.maxHp = 100;
        SetEnemySize(type);
        ChangeState(EnemyState.Move);
    }
    
    private void SetEnemySize(string type)
    {
        switch (type)
        {
            case "Dust":
                baseWidth = 0.5f;
                baseHeight = 0.5f;
                scale = 3f;
                offSetX = 0.008874312f;
                offSetY = 0.2873794f;
                break;
            default:
                baseWidth = 1f;
                baseHeight = 1f;
                scale = 1f;
                break;
        }
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