namespace DefenseGameWebSocketServer.Model
{
    public class EnemySyncPacket
    {
        public string enemyId { get; set; }
        public float x { get; set; }
        public EnemySyncPacket(
            string enemyId,
            float x
        )
        {
            this.enemyId = enemyId;
            this.x = x;
        }
    }

    public class EnemySyncMessage : BaseMessage
    {
        public List<EnemySyncPacket> enemies { get; set; }
        public EnemySyncMessage(
            List<EnemySyncPacket> enemies
        )
        {
            type = "enemy_sync";
            this.enemies = enemies;
        }
    }
}
