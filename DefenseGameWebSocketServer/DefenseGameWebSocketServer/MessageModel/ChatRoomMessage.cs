namespace DefenseGameWebSocketServer.Model
{
    public class ChatRoomMessage : BaseMessage
    {
        public string playerId { get; set; }
        public string nickName { get; set; }
        public string message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public ChatRoomMessage(
            string playerId,
            string nickName,
            string message
        )
        {
            type = "chat_room";
            this.playerId = playerId;
            this.nickName = nickName;
            this.message = message;
        }
    }
}
