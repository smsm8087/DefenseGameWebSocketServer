namespace DefenseGameWebSocketServer.MessageModel
{
    public class BulletSpawnMessage
    {
        public string type = "bullet_spawn";
        public string bulletId;
        public string enemyId;
        public float x;
        public float y;

        public BulletSpawnMessage(string bulletId, string enemyId, float startX, float startY)
        {
            this.bulletId = bulletId;
            this.enemyId = enemyId;
            this.x = startX;
            this.y = startY;
        }
    }
}