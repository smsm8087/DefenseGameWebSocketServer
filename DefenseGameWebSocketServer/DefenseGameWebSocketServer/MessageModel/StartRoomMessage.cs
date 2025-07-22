namespace DefenseGameWebSocketServer.Model
{
    public class StartRoomMessage : BaseMessage
    {
        public string playerId { get; set; }
        public string roomCode { get; set; }
        public List<string> players { get; set; }
        public StartRoomMessage(
            string playerId,
            string roomCode,
            List<string> players
        )
        {
            type = "start_game";
            this.playerId = playerId;
            this.roomCode = roomCode;
            this.players = players;
        }
    }
}
