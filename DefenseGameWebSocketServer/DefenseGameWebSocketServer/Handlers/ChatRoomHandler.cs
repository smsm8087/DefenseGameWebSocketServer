using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models;
using System.Text.Json;

public class ChatRoomHandler
{
    public async Task HandleAsync(string playerId, string rawMessage, IWebSocketBroadcaster broadcaster)
    {
        var msg = JsonSerializer.Deserialize<ChatRoomMessage>(rawMessage);
        Room room = RoomManager.Instance.GetRoomByPlayerId(playerId);
        if (msg == null)
        {
            LogManager.Error("[ChatRoomHandler] 잘못된 메시지 수신", room.RoomCode, playerId);
            return;
        }
        room.AddChatLog(playerId, msg.message);
        await broadcaster.BroadcastAsync(new ChatRoomMessage(playerId, msg.nickName, msg.message));
    }
}
