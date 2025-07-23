using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using System.Text.Json;

public class CreateRoomHandler
{
    public async Task HandleAsync(string playerId, string rawMessage, IWebSocketBroadcaster broadcaster)
    {
        var msg = JsonSerializer.Deserialize<CreateRoomMessage>(rawMessage);
        if (msg == null)
        {
            Console.WriteLine("[CreateRoomHandler] 잘못된 메시지 수신");
            return;
        }
        //이미 플레이어는 상위에서 브로드캐스터에 add 되어있음
        await broadcaster.SendToAsync(playerId, new 
        {
            type = "room_created",
        });
    }
}
