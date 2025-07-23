using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models;
using System.Text.Json;

public class StartGameHandler
{
    public async Task HandleAsync(string playerId, string rawMessage, IWebSocketBroadcaster broadcaster, GameManager gameManager)
    {
        var msg = JsonSerializer.Deserialize<StartRoomMessage>(rawMessage);
        if (msg == null)
        {
            Console.WriteLine("[JoinRoomHandler] 잘못된 메시지 수신");
            return;
        }
        string roomCode = msg.roomCode;
        Room room = RoomManager.Instance.GetRoom(msg.roomCode);
        if (room == null)
        {
            Console.WriteLine($"[StartGameHandler] 방 {roomCode}가 존재하지 않음");
            return;
        }
        // 방장이 맞는지 확인
        if (room.HostId != playerId)
        {
            Console.WriteLine($"[StartGameHandler] 플레이어 {playerId}는 방장 권한이 없음");
            return;
        }
       
        List<string> playerIds = msg.players;
        if (playerIds == null || playerIds.Count == 0)
        {
            Console.WriteLine("[StartGameHandler] 플레이어 목록이 비어있음");
            return;
        }
        // 플레이어가 방에 있는지 확인
        foreach (var id in playerIds)
        {
            if (!RoomManager.Instance.ExistPlayer(roomCode, id))
            {
                Console.WriteLine($"[StartGameHandler] 플레이어 {id}가 방에 존재하지 않음");
                return;
            }
        }
        await gameManager.TryConnectGame();
    }
}
