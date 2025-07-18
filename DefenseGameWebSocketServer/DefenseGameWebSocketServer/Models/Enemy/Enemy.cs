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

    public float baseY;     // 최초 Y위치
    public float floatYOffset = 0f;           // 현재 y 추가 오프셋
    private float floatTargetOffset = 0f;     // 목표 오프셋
    private float floatTimer = 0f;
    private float floatDuration = 0.5f;       // 얼마나 자주 변경할지 (초)
    private float floatRange = 0.05f;         // 위아래 이동 범위

    public bool isRangedAttackPending { get; private set; } // 원거리 공격 대기 상태
    public BulletData bulletData;
    public bool HasAttacked = false;

    public void UpdateFloating(float deltaTime)
    {
        floatTimer += deltaTime;

        // 목표 위치 갱신
        if (floatTimer >= floatDuration)
        {
            floatTimer = 0f;
            floatTargetOffset = (float)(new Random().NextDouble() * 2 - 1) * floatRange; // -range ~ +range
        }

        // 천천히 목표 위치로 이동 lerp 방식 사용
        floatYOffset = floatYOffset + (floatTargetOffset - floatYOffset) * deltaTime * 3f; 
    }
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
        this.baseY = startY;
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
    
        var alivePlayers = players.Where(p => !p.IsDead).ToArray();
        if (alivePlayers.Length == 0) return;

        if (AggroTarget == null || AggroTarget.IsDead ||
            (DateTime.UtcNow - lastAggroChangeTime).TotalSeconds >= enemyBaseData.aggro_cool_down || 
            attackCount >= enemyBaseData.aggro_attack_count)
        {
            var rand = new Random();
            AggroTarget = alivePlayers[rand.Next(alivePlayers.Length)];
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