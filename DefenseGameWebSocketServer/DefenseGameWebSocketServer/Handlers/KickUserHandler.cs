using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models;
using System.Text.Json;

public class KickUserHandler
{
    public class KickUserMessage : BaseMessage
    {
        public string playerId { get; set; }
        public string roomCode { get; set; }
        public string targetUserId { get; set; }
        public KickUserMessage(
            string playerId,
            string roomCode,
            string targetUserId
        )
        {
            type = "kick_user";
            this.playerId = playerId;
            this.roomCode = roomCode;
            this.targetUserId = targetUserId;
        }
    }
    public async Task HandleAsync(string playerId, string rawMessage, Room room, WebSocketBroadcaster broadcaster)
    {
        var msg = JsonSerializer.Deserialize<KickUserMessage>(rawMessage);
        if (room == null)
        {
            LogManager.Error("[KickUserHandler] 플레이어가 속한 방을 찾을 수 없음", room.RoomCode, playerId);
            return;
        }
        if (msg == null)
        {
            LogManager.Error("[KickUserHandler] 잘못된 메시지 수신", room.RoomCode, playerId);
            return;
        }
        if(room.HostId != playerId)
        {
            LogManager.Error("[KickUserHandler] 호스트가 아닌 플레이어가 방에서 플레이어를 제거하려고 시도함", room.RoomCode, playerId);
            return;
        }
        RoomInfo roomInfo = room.GetRoomInfo(msg.targetUserId);
        if(roomInfo == null)
        {
            LogManager.Error("[KickUserHandler] 방에 참여하지 않았습니다", room.RoomCode, playerId);
        }
        //방에서 타겟플레이어 제거
        if(!RoomManager.Instance.RemovePlayer(room.RoomCode, msg.targetUserId))
        {
            LogManager.Error("[KickUserHandler] 플레이어 제거 실패", room.RoomCode, playerId);
            return;
        }
        
        //방에 참여한 플레이어가 있으면 방 정보 브로드캐스트
        await broadcaster.BroadcastAsync(new GetRoomInfoHandler.GetRoomInfoMessage(playerId, room.RoomCode, room.RoomInfos, room.HostId));

        await broadcaster.SendToAsync(msg.targetUserId, new OutRoomHandler.OutRoomMessage(msg.targetUserId, room.RoomCode, "success", ""));
        //방에서 플레이어 제거 후 브로드캐스터에 플레이어 제거
        broadcaster.Unregister(msg.targetUserId);
    }
}
