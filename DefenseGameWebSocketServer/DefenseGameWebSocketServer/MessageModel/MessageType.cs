namespace DefenseGameWebSocketServer.Model
{
    public class BaseMessage
    {
        public string type { get; set; }
    }
    public enum MessageType
    {
        Move,
        Restart,
        Unknown
    }
    public static class MessageTypeHelper
    {
        public static MessageType Parse(string type)
        {
            return type switch
            {
                "move" => MessageType.Move,
                "restart" => MessageType.Restart,
                _ => MessageType.Unknown,
            };
        }
    }
}
