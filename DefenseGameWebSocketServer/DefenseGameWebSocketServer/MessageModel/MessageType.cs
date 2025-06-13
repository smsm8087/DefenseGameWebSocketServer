namespace DefenseGameWebSocketServer.Model
{
    public class BaseMessage
    {
        public string type { get; set; }
    }
    public enum MessageType
    {
        Move,
        Unknown
    }
    public static class MessageTypeHelper
    {
        public static MessageType Parse(string type)
        {
            return type switch
            {
                "move" => MessageType.Move,
                _ => MessageType.Unknown,
            };
        }
    }
}
