using DefenseGameWebSocketServer.Model;

public class PlayerUpdateHpMessage : BaseMessage
{
    public string playerId { get; set; }
    public PlayerInfo playerinfo { get; set; }

    public PlayerUpdateHpMessage(
        string playerId,
        PlayerInfo playerinfo
    )
    {
        type = "player_update_hp";
        this.playerId = playerId;
        this.playerinfo = playerinfo;
    }
}