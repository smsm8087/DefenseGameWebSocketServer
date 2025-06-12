namespace DefenseGameWebSocketServer.Model
{
    public class BaseMessage
    {
        public string type { get; set; }
    }
    public enum MessageType
    {
        Move,
        PlayerJoin,
        PlayerList,
        PlayerLeave,
        Unknown
    }
    public static class MessageTypeHelper
    {
        public static MessageType Parse(string type)
        {
            return type switch
            {
                "move" => MessageType.Move,
                "player_join" => MessageType.PlayerJoin,
                "player_list" => MessageType.PlayerList,
                "player_leave" => MessageType.PlayerLeave,
                _ => MessageType.Unknown,
            };
        }
    }
}
