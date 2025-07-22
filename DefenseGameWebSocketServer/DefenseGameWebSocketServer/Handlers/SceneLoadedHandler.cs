using DefenseGameWebSocketServer.Manager;
using System.Text.Json;

public class SceneLoadedHandler
{
    private readonly RoomManager _roomManager;
    private readonly GameManager _gameManager;

    public SceneLoadedHandler(RoomManager roomManager, GameManager gameManager)
    {
        _roomManager = roomManager;
        _gameManager = gameManager;
    }

    public async Task HandleAsync(string playerId, string message)
    {
        var doc = JsonDocument.Parse(message);
        var root = doc.RootElement;

        var roomCode = root.GetProperty("roomCode").GetString();

        if(string.IsNullOrEmpty(roomCode))
        {
            Console.WriteLine("[SceneLoadedHandler] 방 코드가 없습니다.");
            return;
        }

        var room = _roomManager.GetRoom(roomCode);
        if(room == null)
        {
            Console.WriteLine($"방 {roomCode} 없음");
            return;
        }

        if(!room.PlayerLoadingStatus.ContainsKey(playerId))
        {
            Console.WriteLine($"[{playerId}] 플레이어 로딩 상태가 없습니다.");
            return;
        }
        _roomManager.SetLoadingComplete(roomCode, playerId);
        Console.WriteLine($"[{playerId}] 씬 로딩완료!");

        // 모든 플레이어가 로딩을 완료했는지 확인하고 게임 초기화 시도
        if (room.AllPlayersLoading())
        {
            await _gameManager.InitializeGame(room.PlayerLoadingStatus.Keys.ToList());
        }
    }
}
