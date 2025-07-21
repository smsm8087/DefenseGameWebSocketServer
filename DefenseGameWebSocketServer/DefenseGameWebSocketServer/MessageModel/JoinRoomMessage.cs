namespace DefenseGameWebSocketServer.Model
{
    public class JoinRoomMessage : BaseMessage
    {
        public string playerId { get; set; }
        public string roomCode { get; set; }
        public JoinRoomMessage(
            string playerId,
            string roomCode
        )
        {
            type = "join_room";
            this.playerId = playerId;
            this.roomCode = roomCode;
        }
    }
}
