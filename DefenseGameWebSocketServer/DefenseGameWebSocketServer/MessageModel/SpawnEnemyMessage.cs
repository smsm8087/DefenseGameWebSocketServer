namespace DefenseGameWebSocketServer.Model
{
    public class SpawnEnemyMessage : BaseMessage
    {
        public string enemyId { get; set; }
        public int wave { get; set; }
        public float spawnPosX { get; set; }
        public float spawnPosY { get; set; }
        public int enemyDataId { get; set; } 
        public SpawnEnemyMessage(
            string enemyId,
            int wave,
            float spawnPosX,
            float spawnPosY,
            int enemyDataId
        )
        {
            type = "spawn_enemy";
            this.enemyId = enemyId;
            this.wave = wave;
            this.spawnPosX = spawnPosX;
            this.spawnPosY = spawnPosY;
            this.enemyDataId = enemyDataId;
        }
    }
}
