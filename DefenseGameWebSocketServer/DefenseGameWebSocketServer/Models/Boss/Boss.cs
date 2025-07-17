using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.MessageModel;
using DefenseGameWebSocketServer.Models.DataModels;

public enum BossState
{
    Idle,
    Moving,
    ExecutingPattern,
    Dead
}

public class Boss
{
    public int bossId;
    public float x, y, targetX, targetY;
    public int maxHp, currentHp;
    public float aggro_cool_down;
    public float speed, range;
    public BossData bossBaseData;
    public BossState State { get; private set; } = BossState.Idle;

    private DateTime _lastAggroCheck = DateTime.UtcNow;
    private Player _aggroTarget;
    public bool isDead = false;


    public void SetState(BossState newState)
    {
        State = newState;
    }


    public void TakeDamage(int amount)
    {
        currentHp -= amount;
        if (currentHp <= 0)
        {
            currentHp = 0;
            SetState(BossState.Dead);
        }
    }

    public void UpdateAggro(Player player)
    {
        if (State != BossState.Idle) return;

        if ((DateTime.UtcNow - _lastAggroCheck).TotalSeconds > aggro_cool_down || _aggroTarget == null)
        {
            var rand = new Random();
            _aggroTarget = player;
            targetX = _aggroTarget.x;
            targetY = _aggroTarget.y;
            _lastAggroCheck = DateTime.UtcNow;
        }
    }

    public void UpdateMovement(float deltaTime)
    {
        if (State != BossState.Moving) return;
        targetX = _aggroTarget.x;
        targetY = _aggroTarget.y;

        float dx = targetX - x;
        float dy = targetY - y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);

        if (dist > 0.1f)
        {
            x += dx / dist * speed * deltaTime;
        }
    }
}