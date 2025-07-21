using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using System.Text.Json;

public class JoinRoomHandler
{
    public async Task HandleAsync(
        string rawMessage,
        IWebSocketBroadcaster broadcaster,
        RoomManager roomManager
    )
    {
        var msg = JsonSerializer.Deserialize<CreateRoomMessage>(rawMessage);
        if (msg == null)
        {
            Console.WriteLine("[JoinRoomHandler] 잘못된 메시지 수신");
            return;
        }
        if(roomManager.GetRoom(msg.roomCode) != null)
        {
            roomManager.AddPlayer(msg.roomCode, msg.playerId);
            await broadcaster.SendToAsync(msg.playerId, new
            {
                type = "room_joined",
                RoomCode = msg.roomCode,
                PlayerId = msg.playerId
            });
        }
    }
}