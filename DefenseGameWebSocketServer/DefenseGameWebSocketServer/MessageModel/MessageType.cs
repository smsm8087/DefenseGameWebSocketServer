namespace DefenseGameWebSocketServer.Model
{
    public class BaseMessage
    {
        public string type { get; set; }
    }
    
    public enum MessageType
    {
        GetRoomInfo,
        SceneLoaded,
        StartGame,
        CreateRoom,
        JoinRoom,
        Move,
        Restart,
        PlayerAnimation,
        PlayerAttack,
        EnemyAttackHit,
        AttackSuccess,
        SettlementReady,
        StartRevival,
        UpdateRevival,
        CancelRevival,
        Unknown,
    }

    public static class MessageTypeHelper
    {
        public static MessageType Parse(string type)
        {
            return type switch
            {
                "get_room_info" => MessageType.GetRoomInfo,
                "scene_loaded" => MessageType.SceneLoaded,
                "start_game" => MessageType.StartGame,
                "create_room" => MessageType.CreateRoom,
                "join_room" => MessageType.JoinRoom,
                "move" => MessageType.Move,
                "restart" => MessageType.Restart,
                "player_animation" => MessageType.PlayerAnimation,
                "player_attack" => MessageType.PlayerAttack,
                "enemy_attack_hit" => MessageType.EnemyAttackHit,
                "attack_success" => MessageType.AttackSuccess,
                "settlement_ready" => MessageType.SettlementReady,
                "start_revival" => MessageType.StartRevival,
                "update_revival" => MessageType.UpdateRevival,
                "cancel_revival" => MessageType.CancelRevival,
                _ => MessageType.Unknown,
            };
        }
    }
}