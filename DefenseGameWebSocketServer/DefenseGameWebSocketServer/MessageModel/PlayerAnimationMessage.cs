using DefenseGameWebSocketServer.Model;

public class PlayerAnimationMessage : BaseMessage
{
    public string playerId { get; set; }
    public string animation { get; set; } // ex: "attack", "jump", "run"

    public PlayerAnimationMessage(
        string playerId,
        string animation
    )
    {
        type = "player_animation";
        this.playerId = playerId;
        this.animation = animation;
    }
}