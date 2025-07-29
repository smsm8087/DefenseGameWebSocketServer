using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models;
using System.Text.Json;

public class ChatRoomHandler
{
    public async Task HandleAsync(string playerId, string rawMessage, IWebSocketBroadcaster broadcaster)
    {
        var msg = JsonSerializer.Deserialize<ChatRoomMessage>(rawMessage);
        if (msg == null)
        {
            Room room = RoomManager.Instance.GetRoomByPlayerId(playerId);
            LogManager.Error("[ChatRoomHandler] 잘못된 메시지 수신", room.RoomCode, playerId);
            return;
        }
        //이미 플레이어는 상위에서 브로드캐스터에 add 되어있음
        await broadcaster.BroadcastAsync(new ChatRoomMessage(playerId, msg.nickName, msg.message, msg.chatData));
        //sendToAsync 대신 BroadcastAsync
    }
}
