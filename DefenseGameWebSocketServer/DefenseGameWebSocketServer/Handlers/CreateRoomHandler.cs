using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.MessageModel;
using DefenseGameWebSocketServer.Model;
using System.Text.Json;

public class CreateRoomHandler
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
            Console.WriteLine("[CreateRoomHandler] 잘못된 메시지 수신");
            return;
        }
        if(roomManager.RoomExists(msg.roomCode))
        {
            Console.WriteLine($"[CreateRoomHandler] 방 {msg.roomCode} 이미 존재");
            return;
        }
        roomManager.CreateRoom(msg.roomCode, msg.playerId);
        await broadcaster.SendToAsync(msg.playerId, new
        {
            type = "room_created",
        });
    }
}