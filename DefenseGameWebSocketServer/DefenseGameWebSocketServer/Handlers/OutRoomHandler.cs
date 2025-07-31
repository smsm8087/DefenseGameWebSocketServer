using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models;
using System.Text.Json;

public class OutRoomHandler
{
    public class OutRoomMessage : BaseMessage
    {
        public string playerId { get; set; }
        public string roomCode { get; set; }
        public string message { get; set; }
        public string hostId { get; set; }
        public OutRoomMessage(
            string playerId,
            string roomCode,
            string message,
            string hostId
        )
        {
            type = "out_room";
            this.playerId = playerId;
            this.roomCode = roomCode;
            this.message = message;
            this.hostId = hostId;
        }
    }

    public async Task HandleAsync(string playerId, string rawMessage, Room room, WebSocketBroadcaster broadcaster)
    {
        var msg = JsonSerializer.Deserialize<OutRoomMessage>(rawMessage);
        if (room == null)
        {
            LogManager.Error("[OutRoomHandler] 플레이어가 속한 방을 찾을 수 없음", room?.RoomCode, playerId);
            return;
        }
        if (msg == null)
        {
            LogManager.Error("[OutRoomHandler] 잘못된 메시지 수신", room.RoomCode, playerId);
            return;
        }
        RoomInfo roomInfo = room.GetRoomInfo(playerId);
        if (roomInfo == null)
        {
            LogManager.Error("[OutRoomHandler] 방에 참여하지 않았습니다", room.RoomCode, playerId);
        }
        //방에서 플레이어 제거
        if(!RoomManager.Instance.RemovePlayer(room.RoomCode, playerId))
        {
            LogManager.Error("[OutRoomHandler] 플레이어 제거 실패", room.RoomCode, playerId);
            return;
        }
        //플레이어가 호스트이면 호스트 아이디 변경
        if(playerId == room.HostId)
        {
            if(room.GetRoomInfo(msg.hostId) != null)
            {
               LogManager.Info("[OutRoomHandler] 호스트아이디 변경", room.RoomCode, room.HostId);
               room.HostId = msg.hostId;
            } else
            {
                LogManager.Error("[OutRoomHandler] 호스트 변경 실패", room.RoomCode, msg.hostId);
            }
        }
        //방에 참여한 플레이어가 없으면 방 제거
        if( room.GetPlayerCount() == 0)
        {
            RoomManager.Instance.RemoveRoom(room.RoomCode);
            LogManager.Info("[OutRoomHandler] 방 제거", room.RoomCode, playerId);
        }
        else
        {
            //방에 참여한 플레이어가 있으면 방 정보 브로드캐스트
            await broadcaster.BroadcastAsync(new GetRoomInfoHandler.GetRoomInfoMessage(playerId, room.RoomCode, room.RoomInfos, room.HostId));
        }

        await broadcaster.SendToAsync(playerId ,new OutRoomMessage(playerId, room.RoomCode, "success", ""));
        //방에서 플레이어 제거 후 브로드캐스터에 플레이어 제거
        broadcaster.Unregister(playerId);
    }
}