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
       
        //전부다 준비완료상태인지 확인
        if (!room.AllPlayersReady())
        {
            var notificationService = new NotificationService(broadcaster);
            await notificationService.BroadCastNoticeAsync("준비가 안된 인원이 있습니다.");
            return;
        }
        await gameManager.TryConnectGame();
    }
}
