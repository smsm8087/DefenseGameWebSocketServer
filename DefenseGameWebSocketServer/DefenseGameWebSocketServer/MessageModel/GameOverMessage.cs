namespace DefenseGameWebSocketServer.Model
{
    public class GameOverMessage : BaseMessage
    {
        public string message { get; set; }
        public GameOverMessage(
            string type,
            string message
        )
        {
            this.type = type;
            this.message = message;
        }
    }
}
