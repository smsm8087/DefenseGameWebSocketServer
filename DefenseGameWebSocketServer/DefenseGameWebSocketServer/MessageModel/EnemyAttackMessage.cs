using DefenseGameWebSocketServer.Model;
public class EnemyChangeStateMessage : BaseMessage
{
    public string enemyId { get; set; }
    public string animName { get; set; }
    public string playerId { get; set; }
    public EnemyChangeStateMessage(
        string enemyId,
        string animName,
        Player? targetPlayer = null
    )
    {
        type = "enemy_change_state";
        this.enemyId = enemyId;
        this.animName = animName;
        this.playerId = targetPlayer?.id;
    }
}