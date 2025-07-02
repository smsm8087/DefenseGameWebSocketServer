using DefenseGameWebSocketServer.Models;
using DefenseGameWebSocketServer.Models.DataModels;

public enum EnemyState
{
    Move,
    Attack,
    Dead
}

public class Enemy
{
    public string id;
    public string type;
    public float x;
    public float y;
    public int currentHp = 100;
    public int maxHp = 100;
    public float targetX;
    public float targetY;
    public EnemyState state;
    private IEnemyFSMState _currentState;
    public float currentAttack = 1f;
    public float currentDefense = 10f;
    public float currentSpeed = 2f;

    public EnemyData enemyBaseData;
    //fsm
    public EnemyMoveState moveState = new EnemyMoveState();
    public EnemyAttackState attackState = new EnemyAttackState();
    public EnemyDeadState deadState = new EnemyDeadState();
    public Action<EnemyBroadcastEvent> OnBroadcastRequired;

    public WaveData waveData;
    public bool IsAlive => currentHp > 0;

    public Enemy(string id, EnemyData enemyData, float startX, float startY, float targetX, float targetY, WaveData waveData, WaveRoundData waveRoundData)
    {
        this.id = id;
        this.x = startX;
        this.y = startY;
        this.targetX = targetX;
        this.targetY = targetY;
        enemyBaseData = enemyData;
        this.currentHp = this.maxHp = enemyBaseData.hp + waveRoundData.add_hp;
        this.currentSpeed = enemyBaseData.speed + waveRoundData.add_movespeed;
        this.currentAttack = enemyBaseData.attack + waveRoundData.add_attack;
        this.currentDefense = enemyBaseData.defense + waveRoundData.add_defense;
        this.type = enemyBaseData.type;

        this.waveData = waveData;
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
        currentHp -= dmg;
        if (currentHp < 0) currentHp = 0;
    }
}