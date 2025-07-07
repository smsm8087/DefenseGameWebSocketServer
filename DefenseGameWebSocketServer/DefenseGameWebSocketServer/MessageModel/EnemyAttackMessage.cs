using DefenseGameWebSocketServer.Model;
public class EnemyChangeStateMessage : BaseMessage
{
    public string enemyId { get; set; }
    public string animName { get; set; }
    public EnemyChangeStateMessage(
        string enemyId,
        string animName
    )
    {
        type = "enemy_change_state";
        this.enemyId = enemyId;
        this.animName = animName;
    }
}