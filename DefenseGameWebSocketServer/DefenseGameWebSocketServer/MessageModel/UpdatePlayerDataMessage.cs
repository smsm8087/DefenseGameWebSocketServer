namespace DefenseGameWebSocketServer.Model
{
    public class UpdatePlayerDataMessage : BaseMessage
    {
        public PlayerInfo playerInfo { get; set; }
        public UpdatePlayerDataMessage(
            PlayerInfo playerInfo
        )
        {
            type = "update_player_data";
            this.playerInfo = playerInfo;
        }
    }
}
