namespace DefenseGameWebSocketServer.MessageModel
{
    public class BulletTickMessage
    {
        public string type = "bullet_tick";
        public List<BulletInfo> bullets;

        public BulletTickMessage(List<BulletInfo> bullets)
        {
            this.bullets = bullets;
        }

        public class BulletInfo
        {
            public string bulletId;
            public float x;
            public float y;
        }
    }
}