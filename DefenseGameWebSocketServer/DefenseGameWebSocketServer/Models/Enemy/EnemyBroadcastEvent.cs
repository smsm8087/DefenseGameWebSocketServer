public class EnemyBroadcastEvent
{
    public EnemyState Type;
    public Enemy EnemyRef;
    public object Payload;

    public EnemyBroadcastEvent(EnemyState type, Enemy enemy, object payload = null)
    {
        Type = type;
        EnemyRef = enemy;
        Payload = payload;
    }
}
