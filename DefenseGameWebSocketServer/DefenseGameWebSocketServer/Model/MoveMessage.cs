namespace DefenseGameWebSocketServer.Model
{
    public class MoveMessage : BaseMessage
    {
        public string playerId { get; set; }
        public float x { get; set; }
        public float y { get; set; }
        public bool isJumping { get; set; }
        public bool isRunning { get; set; }
        public MoveMessage(
            string type,
            string playerId,
            float x,
            float y,
            bool isJumping,
            bool isRunning
        )
        {
            this.type = type;
            this.playerId = playerId;
            this.x = x;
            this.y = y;
            this.isJumping = isJumping;
            this.isRunning = isRunning;
        }
    }
}
