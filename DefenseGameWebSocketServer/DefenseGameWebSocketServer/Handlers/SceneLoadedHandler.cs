using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Models;
using System.Text.Json;

public class SceneLoadedHandler
{
    private readonly GameManager _gameManager;
    private readonly Room _room;

    public SceneLoadedHandler(Room room, GameManager gameManager)
    {
        _room = room;
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

        if(!_room.PlayerLoadingStatus.ContainsKey(playerId))
        {
            Console.WriteLine($"[{playerId}] 플레이어 로딩 상태가 없습니다.");
            return;
        }
        _room.PlayerLoadingStatus[playerId] = true;
        Console.WriteLine($"[{playerId}] 씬 로딩완료!");

        // 모든 플레이어가 로딩을 완료했는지 확인하고 게임 초기화 시도
        if (_room.AllPlayersLoading())
        {
            await _gameManager.InitializeGame(_room.playerIds);
        }
    }
}
