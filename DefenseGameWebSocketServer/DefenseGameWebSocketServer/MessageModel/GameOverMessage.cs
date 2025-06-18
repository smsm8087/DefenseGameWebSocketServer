namespace DefenseGameWebSocketServer.Model
{
    public class GameOverMessage : BaseMessage
    {
        public string message { get; set; }
        public GameOverMessage(
            string message
        )
        {
            type = "game_over";
            this.message = message;
        }
    }
}
