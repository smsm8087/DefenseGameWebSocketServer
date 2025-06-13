using DefenseGameWebSocketServer.Model;
public class EnemyDieMessage : BaseMessage
{
    public List<string> deadEnemyIds { get; set; }
    public EnemyDieMessage(
        string type,
        List<string> deadEnemyIds
    )
    {
        this.type = type;
        this.deadEnemyIds = deadEnemyIds;
    }
}