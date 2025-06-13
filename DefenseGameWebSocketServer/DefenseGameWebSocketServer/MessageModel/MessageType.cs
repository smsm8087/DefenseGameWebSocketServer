namespace DefenseGameWebSocketServer.Model
{
    public class BaseMessage
    {
        public string type { get; set; }
    }
    public enum MessageType
    {
        Move,
        SpawnEnemy,
        Unknown
    }
    public static class MessageTypeHelper
    {
        public static MessageType Parse(string type)
        {
            return type switch
            {
                "move" => MessageType.Move,
                "spawn_enemy" => MessageType.SpawnEnemy,
                _ => MessageType.Unknown,
            };
        }
    }
}
