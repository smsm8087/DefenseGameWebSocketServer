using DefenseGameWebSocketServer.Model;
public class EnemyDieMessage : BaseMessage
{
    public string playerId { get; set; }
    public List<string> deadEnemyIds { get; set; }
    public EnemyDieMessage(
        List<string> deadEnemyIds,
        string playerId
    )
    {
        type = "enemy_die";
        this.playerId = playerId;
        this.deadEnemyIds = deadEnemyIds;
    }
}