namespace DefenseGameWebSocketServer.Model
{
    public class BaseMessage
    {
        public string type { get; set; }
    }
    
    public enum MessageType
    {
        KickUser,
        DeSelectCharacter,
        SelectCharacter,
        OutRoom,
        GetRoomInfo,
        SceneLoaded,
        StartGame,
        CreateRoom,
        JoinRoom,
        ConfirmResponse,
        ChatRoom,
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
        UseSkill,
        Unknown,
    }

    public static class MessageTypeHelper
    {
        public static MessageType Parse(string type)
        {
            return type switch
            {
                "kick_user" => MessageType.KickUser,
                "deselect_character" => MessageType.DeSelectCharacter,
                "select_character" => MessageType.SelectCharacter,
                "out_room" => MessageType.OutRoom,
                "get_room_info" => MessageType.GetRoomInfo,
                "scene_loaded" => MessageType.SceneLoaded,
                "start_game" => MessageType.StartGame,
                "create_room" => MessageType.CreateRoom,
                "join_room" => MessageType.JoinRoom,
                "chat_room" => MessageType.ChatRoom,
                "confirm_response"  => MessageType.ConfirmResponse,
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
                "use_skill" => MessageType.UseSkill,
                _ => MessageType.Unknown,
            };
        }
    }
}