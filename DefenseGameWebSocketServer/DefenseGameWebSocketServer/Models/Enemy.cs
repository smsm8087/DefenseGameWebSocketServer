using DefenseGameWebSocketServer.Models;
using DefenseGameWebSocketServer.Models.DataModels;

public enum EnemyState
{
    Move,
    Attack,
    RangedAttack,
    Dead
}
public enum TargetType
{
    SharedHp,
    Player,
    None
}
public class Enemy
{
    public string id;
    public string type;
    public float x;
    public float y;
    public int currentHp;
    public int maxHp;
    public float targetX;
    public float targetY;
    public EnemyState state;
    private IEnemyFSMState _currentState;
    public float currentAttack;
    public float currentDefense;
    public float currentSpeed;
    public string killedPlayerId;

    public EnemyData enemyBaseData;
    //fsm
    public EnemyMoveState moveState = new EnemyMoveState();
    public EnemyAttackState attackState = new EnemyAttackState();
    public EnemyDeadState deadState = new EnemyDeadState();
    public EnemyRangedAttackState rangedAttackState = new EnemyRangedAttackState();

    public Action<EnemyBroadcastEvent> OnBroadcastRequired;

    public WaveData waveData;
    public bool IsAlive => currentHp > 0;
    public TargetType targetType;
    public Player AggroTarget { get; private set; }
    private int attackCount;
    private DateTime lastAggroChangeTime;
    private const int MaxAttackBeforeReaggro = 3;
    private const float AggroCooldown = 5f;

    public bool isRangedAttackPending { get; private set; } // 원거리 공격 대기 상태

    public BulletData bulletData;
    public Enemy(string id, EnemyData enemyData, float startX, float startY, float targetX, float targetY, WaveData waveData, WaveRoundData waveRoundData, BulletData bulletData = null)
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
        this.bulletData = bulletData;

        targetType = enemyBaseData.target_type.ToLower() switch
        {
            "player" => TargetType.Player,
            "shared_hp" => TargetType.SharedHp,
            _=> TargetType.None
        };

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
            case EnemyState.RangedAttack:
                isRangedAttackPending = true;
                _currentState = rangedAttackState;
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
    public void TakeDamage(int dmg, string playerId)
    {
        currentHp -= dmg;
        if (currentHp < 0) currentHp = 0;

        if (currentHp == 0 && state != EnemyState.Dead)
        {
            killedPlayerId = playerId;
            ChangeState(EnemyState.Dead);
        }
    }
    public void SetAggroTarget(Player player)
    {
        AggroTarget = player;
        targetX = player.x;
        targetY = player.y;
        lastAggroChangeTime = DateTime.UtcNow;
        attackCount = 0;
    }

    public void UpdateAggro(Player[] players)
    {
        if (targetType != TargetType.Player) return;

        if (AggroTarget == null || (DateTime.UtcNow - lastAggroChangeTime).TotalSeconds >= AggroCooldown || attackCount >= MaxAttackBeforeReaggro)
        {
            var rand = new Random();
            AggroTarget = players[rand.Next(players.Length)];
            targetX = AggroTarget.x;
            targetY = AggroTarget.y;
            lastAggroChangeTime = DateTime.UtcNow;
            attackCount = 0;
        }
    }

    public void OnAttackPerformed()
    {
        attackCount++;
        isRangedAttackPending = false; // 공격이 수행되면 원거리 공격 대기 상태 해제
    }
}