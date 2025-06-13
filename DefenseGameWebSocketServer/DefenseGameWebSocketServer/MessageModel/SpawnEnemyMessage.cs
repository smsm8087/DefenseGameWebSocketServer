namespace DefenseGameWebSocketServer.Model
{
    public class SpawnEnemyMessage : BaseMessage
    {
        public string enemyId { get; set; }
        public int wave { get; set; }
        public float spawnPosX { get; set; }
        public float spawnPosY { get; set; }
        public SpawnEnemyMessage(
            string type,
            string enemyId,
            int wave,
            float spawnPosX,
            float spawnPosY
        )
        {
            this.type = type;
            this.enemyId = enemyId;
            this.wave = wave;
            this.spawnPosX = spawnPosX;
            this.spawnPosY = spawnPosY;
        }
    }
}
