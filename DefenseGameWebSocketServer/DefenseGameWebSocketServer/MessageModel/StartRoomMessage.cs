namespace DefenseGameWebSocketServer.Model
{
    public class StartRoomMessage : BaseMessage
    {
        public string playerId { get; set; }
        public string roomCode { get; set; }
        public int playerCount { get; set; }
        public StartRoomMessage(
            string playerId,
            string roomCode,
            int playerCount
        )
        {
            type = "start_game";
            this.playerId = playerId;
            this.roomCode = roomCode;
            this.playerCount = playerCount;
        }
    }
}
