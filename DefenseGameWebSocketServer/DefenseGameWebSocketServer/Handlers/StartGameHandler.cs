using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using System.Text.Json;

public class StartGameHandler
{
    public async Task HandleAsync(string playerId, string rawMessage, IWebSocketBroadcaster broadcaster, RoomManager roomManager, GameManager gameManager)
    {
        var msg = JsonSerializer.Deserialize<StartRoomMessage>(rawMessage);
        if (msg == null)
        {
            Console.WriteLine("[JoinRoomHandler] 잘못된 메시지 수신");
            return;
        }
        var roomCode = msg.roomCode;
        var room = roomManager.GetRoom(roomCode);

        if (room == null)
        {
            Console.WriteLine($"방 {roomCode} 없음");
            return;
        }

        if (room.HostId != playerId)
        {
            Console.WriteLine($"[{playerId}]는 방장이 아님");
            return;
        }

        if(room.GetPlayerCount() != msg.playerCount)
        {
            Console.WriteLine($"[{roomCode}] 플레이어 수 불일치: 기대 {msg.playerCount}, 현재 {room.GetPlayerCount()}");
            return;
        }

        if (room.AllPlayersReady())
        {
            Console.WriteLine($"[{roomCode}] 게임 시작!");
            

            room.IsGameStarted = true;
            await gameManager.TryConnectGame(room.PlayerReadyStatus.Keys.ToList());
        }
        else
        {
            Console.WriteLine($"[{roomCode}] 아직 준비 안된 플레이어 있음");
        }
    }
}
