namespace DefenseGameWebSocketServer.Model
{
    public class EnemySyncPacket
    {
        public string enemyId { get; set; }
        public float x { get; set; }
        public float y { get; set; }
        public string enemyType { get; set; } 
        public EnemySyncPacket(
            string enemyId,
            float x,
            float y,
            string enemyType
        )
        {
            this.enemyId = enemyId;
            this.x = x;
            this.y = y;
            this.enemyType = enemyType;
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
