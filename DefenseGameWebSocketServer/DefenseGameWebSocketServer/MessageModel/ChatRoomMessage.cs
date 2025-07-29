namespace DefenseGameWebSocketServer.Model
{
    public class ChatRoomMessage : BaseMessage
    {
        public string playerId { get; set; }
        public string nickName { get; set; }
        public string message { get; set; }
        public Dictionary<string, string> chatData { get; set; }
        
        public ChatRoomMessage(
            string playerId,
            string nickName,
            string message,
            Dictionary<string, string> chatData
        )
        {
            type = "chat_room";
            this.playerId = playerId;
            this.nickName = nickName;
            this.message = message;
            this.chatData = chatData;
        }
    }
}
