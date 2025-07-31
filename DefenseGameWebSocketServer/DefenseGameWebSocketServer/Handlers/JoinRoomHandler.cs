using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models;
using System.Text.Json;

public class JoinRoomHandler
{
    public async Task HandleAsync(string playerId, string rawMessage, IWebSocketBroadcaster broadcaster)
    {
        var msg = JsonSerializer.Deserialize<JoinRoomMessage>(rawMessage);
        if (msg == null)
        {
            Room room = RoomManager.Instance.GetRoomByPlayerId(playerId);
            LogManager.Error($"[JoinRoomHandler] 잘못된 메시지 수신: {rawMessage}", room?.RoomCode, playerId);
            return;
        }
        await broadcaster.SendToAsync(playerId, new
        {
            type = "room_joined",
            roomCode = msg.roomCode,
            playerId = playerId
        });
        
        //테스트
        var notificationService = new NotificationService(broadcaster);
        await notificationService.SendNoticeAsync(
            playerId,
            $"[{msg.roomCode}] 방 입장에 성공했습니다!"
        );
    }
}
