namespace DefenseGameWebSocketServer.MessageModel
{
    public class RevivalStartMessage
    {
        public string type = "revival_started";
        public string reviverId { get; set; }
        public string targetId { get; set; }
        public float duration { get; set; }
        public float targetX { get; set; }
        public float targetY { get; set; }
    }

    public class RevivalProgressMessage
    {
        public string type = "revival_progress";
        public string targetId { get; set; }
        public float progress { get; set; }
    }

    public class RevivalCompletedMessage
    {
        public string type = "revival_completed";
        public string targetId { get; set; }
        public float reviveX { get; set; }
        public float reviveY { get; set; }
        public int currentHp { get; set; }
        public int maxHp { get; set; }
        public float invulnerabilityDuration { get; set; }
    }

    public class RevivalCancelledMessage
    {
        public string type = "revival_cancelled";
        public string targetId { get; set; }
        public string reviverId { get; set; }
        public string reason { get; set; }
    }

    public class PlayerDeadMessage
    {
        public string type = "player_dead";
        public string playerId { get; set; }
        public float deathX { get; set; }
        public float deathY { get; set; }
    }

    public class InvulnerabilityEndedMessage
    {
        public string type = "invulnerability_ended";
        public string playerId { get; set; }
    }
    
    public class StartRevivalRequest
    {
        public string type = "start_revival";
        public string targetId { get; set; }
    }

    public class UpdateRevivalRequest
    {
        public string type = "update_revival";
        public string targetId { get; set; }
        public float progress { get; set; }
    }

    public class CancelRevivalRequest
    {
        public string type = "cancel_revival";
        public string targetId { get; set; }
    }
}