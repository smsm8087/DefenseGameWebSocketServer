namespace DefenseGameWebSocketServer.MessageModel
{
    public class BulletDestroyMessage
    {
        public string type = "bullet_destroy";
        public List<BulletTickMessage.BulletInfo> bullets;

        public BulletDestroyMessage(List<BulletTickMessage.BulletInfo> bullets)
        {
            this.bullets = bullets;
        }
    }
}