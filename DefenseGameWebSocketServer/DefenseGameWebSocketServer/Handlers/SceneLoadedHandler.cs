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
            LogManager.Error($"[SceneLoadedHandler] 방 코드가 없습니다. 메시지: {message}", _room.RoomCode, playerId);
            return;
        }
        if(roomCode != _room.RoomCode)
        {
            LogManager.Error($"[SceneLoadedHandler] 방 코드 불일치. 메시지: {message}", _room.RoomCode, playerId);
            return;
        }
        var roomInfo = _room.GetRoomInfo(playerId);
        if(roomInfo == null)
        {
            LogManager.Error($"[SceneLoadedHandler] 플레이어 정보가 없습니다. 메시지: {message}", _room.RoomCode, playerId);
            return;
        }

        roomInfo.isLoading = true;
        LogManager.Info($"[{playerId}] 씬 로딩 상태 업데이트: 로딩 완료", _room.RoomCode, playerId);

        // 모든 플레이어가 로딩을 완료했는지 확인하고 게임 초기화 시도
        if (_room.AllPlayersLoading())
        {
            await _gameManager.InitializeGame(_room.RoomInfos);
        }
    }
}
