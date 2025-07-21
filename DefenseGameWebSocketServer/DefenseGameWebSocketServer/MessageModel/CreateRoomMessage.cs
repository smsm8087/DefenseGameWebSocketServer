namespace DefenseGameWebSocketServer.Model
{
    public class CreateRoomMessage : BaseMessage
    {
        public string playerId { get; set; }
        public string roomCode { get; set; }
        public CreateRoomMessage(
            string playerId,
            string roomCode
        )
        {
            type = "create_room";
            this.playerId = playerId;
            this.roomCode = roomCode;
        }
    }
}
