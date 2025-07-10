namespace DefenseGameWebSocketServer.MessageModel
{
    public class BossSyncMessage
    {
        public string type = "boss_sync";
        public float x;
        public float y;

        public BossSyncMessage(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }
}
