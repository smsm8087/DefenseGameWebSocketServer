namespace DefenseGameWebSocketServer.Model
{
    public class StartRoomMessage : BaseMessage
    {
        public string playerId { get; set; }
        public string roomCode { get; set; }
        public StartRoomMessage(
            string playerId,
            string roomCode
        )
        {
            type = "start_game";
            this.playerId = playerId;
            this.roomCode = roomCode;
        }
    }
}
