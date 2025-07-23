using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models;
using System.Text.Json;

public class StartGameHandler
{
    private readonly Room room;
    private readonly GameManager gameManager;
    public StartGameHandler(Room room, GameManager gameManager)
    {
        this.room = room;
        this.gameManager = gameManager;
    }
    public async Task HandleAsync(string playerId, string rawMessage, IWebSocketBroadcaster broadcaster)
    {
        var msg = JsonSerializer.Deserialize<StartRoomMessage>(rawMessage);
        if (msg == null)
        {
            LogManager.Error($"[StartGameHandler] 잘못된 메시지 수신: {rawMessage}", room.RoomCode, playerId);
            return;
        }
        string roomCode = msg.roomCode;
        Room temp_room = RoomManager.Instance.GetRoom(msg.roomCode);
        if (temp_room == null)
        {
            LogManager.Error($"[StartGameHandler] 방 {roomCode}가 존재하지 않음", room.RoomCode, playerId);
            return;
        }
        // 방장이 맞는지 확인
        if (room.HostId != playerId)
        {
            LogManager.Error($"[StartGameHandler] 플레이어 {playerId}는 방장 권한이 없음", room.RoomCode, playerId);
            return;
        }
       
        List<string> playerIds = msg.players;
        if (playerIds == null || playerIds.Count == 0)
        {
            LogManager.Error("[StartGameHandler] 플레이어 목록이 비어있음", room.RoomCode, playerId);
            return;
        }
        // 플레이어가 방에 있는지 확인
        foreach (var id in playerIds)
        {
            if (!RoomManager.Instance.ExistPlayer(roomCode, id))
            {
                LogManager.Error($"[StartGameHandler] 플레이어 {id}가 방에 존재하지 않음", room.RoomCode, playerId);
                return;
            }
        }
        await gameManager.TryConnectGame();
    }
}
