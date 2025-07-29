namespace DefenseGameWebSocketServer.Model
{
    public class ChatRoomMessage : BaseMessage
    {
        public string playerId { get; set; }
        public string message { get; set; }
        public ChatRoomMessage(
            string playerId,
            string message
        )
        {
            type = "chat_room";
            this.playerId = playerId;
            this.message = message;
        }
    }
}
