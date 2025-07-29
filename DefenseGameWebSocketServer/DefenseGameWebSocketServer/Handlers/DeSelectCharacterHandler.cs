using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models;
using System.Text.Json;

public class DeSelectCharacterHandler
{
    public class DeSelectedCharacterMessage : BaseMessage
    {
        public string playerId { get; set; }
        public string roomCode { get; set; }
        public DeSelectedCharacterMessage(
            string playerId,
            string roomCode
        )
        {
            type = "deselected_character";
            this.playerId = playerId;
            this.roomCode = roomCode;
        }
    }
    public async Task HandleAsync(string playerId, string rawMessage, Room room, WebSocketBroadcaster broadcaster)
    {
        var msg = JsonSerializer.Deserialize<DeSelectedCharacterMessage>(rawMessage);
        if (room == null)
        {
            LogManager.Error("[SelectCharacterHandler] 플레이어가 속한 방을 찾을 수 없음", room.RoomCode, playerId);
            return;
        }
        if (msg == null)
        {
            LogManager.Error("[SelectCharacterHandler] 잘못된 메시지 수신", room.RoomCode, playerId);
            return;
        }
        RoomInfo roomInfo = room.GetRoomInfo(playerId);
        if(roomInfo == null)
        {
            LogManager.Error("[SelectCharacterHandler] 방에 참여하지 않았습니다", room.RoomCode, playerId);
        }
        roomInfo.isReady = false;

        await broadcaster.BroadcastAsync(new DeSelectedCharacterMessage(
            playerId,
            room.RoomCode
        ));
    }
}
