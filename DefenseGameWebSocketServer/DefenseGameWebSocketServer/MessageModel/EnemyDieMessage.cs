using DefenseGameWebSocketServer.Model;
public class EnemyDieMessage : BaseMessage
{
    public List<string> deadEnemyIds { get; set; }
    public EnemyDieMessage(
        List<string> deadEnemyIds
    )
    {
        type = "enemy_die";
        this.deadEnemyIds = deadEnemyIds;
    }
}