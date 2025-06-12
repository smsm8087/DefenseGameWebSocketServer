namespace DefenseGameWebSocketServer.Model
{
    public class SpawnEnemyMessage : BaseMessage
    {
        public string enemyId { get; set; }
        public int wave { get; set; }
        public SpawnEnemyMessage(
            string type,
            string enemyId,
            int wave
        )
        {
            this.type = type;
            this.enemyId = enemyId;
            this.wave = wave;
        }
    }
}
