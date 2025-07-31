using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models;
using System.Text.Json;

public class CreateRoomHandler
{
    public async Task HandleAsync(string playerId, string rawMessage, IWebSocketBroadcaster broadcaster)
    {
        var msg = JsonSerializer.Deserialize<CreateRoomMessage>(rawMessage);
        if (msg == null)
        {
            Room room = RoomManager.Instance.GetRoomByPlayerId(playerId);
            LogManager.Error("[CreateRoomHandler] 잘못된 메시지 수신", room.RoomCode, playerId);
            return;
        }
        //이미 플레이어는 상위에서 브로드캐스터에 add 되어있음
        await broadcaster.SendToAsync(playerId, new 
        {
            type = "room_created",
        });
        
        //테스트
        var notificationService = new NotificationService(broadcaster);
        await notificationService.SendNoticeAsync(
            playerId,
            "방이 성공적으로 생성되었습니다!"
        );
    }
}
