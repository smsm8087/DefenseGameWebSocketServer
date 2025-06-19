using DefenseGameWebSocketServer.Model;
public class EnemyAttackMessage : BaseMessage
{
    public string enemyId { get; set; }
    public EnemyAttackMessage(
        string enemyId
    )
    {
        type = "enemy_attack";
        this.enemyId = enemyId;
    }
}