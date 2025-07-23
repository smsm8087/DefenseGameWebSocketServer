using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using System.Text.Json;

public class JoinRoomHandler
{
    public async Task HandleAsync(string playerId, string rawMessage, IWebSocketBroadcaster broadcaster)
    {
        var msg = JsonSerializer.Deserialize<JoinRoomMessage>(rawMessage);
        if (msg == null)
        {
            Console.WriteLine("[JoinRoomHandler] 잘못된 메시지 수신");
            return;
        }
        await broadcaster.SendToAsync(playerId, new
        {
            type = "room_joined",
            roomCode = msg.roomCode,
            playerId = playerId
        });
    }
}
