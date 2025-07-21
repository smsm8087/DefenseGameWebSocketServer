using DefenseGameWebSocketServer.Manager;
using System.Text.Json;

public class ReadyHandler
{
    private readonly RoomManager _roomManager;

    public ReadyHandler(RoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    public async Task HandleAsync(string playerId, string message, IWebSocketBroadcaster broadcaster)
    {
        var doc = JsonDocument.Parse(message);
        var root = doc.RootElement;

        var roomCode = root.GetProperty("roomCode").GetString();
        _roomManager.SetReady(roomCode, playerId);
        Console.WriteLine($"[{playerId}] 준비 완료!");
    }
}
