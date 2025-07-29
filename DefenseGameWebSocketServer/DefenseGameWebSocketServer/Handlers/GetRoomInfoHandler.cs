using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models;
using System.Text.Json;

public class GetRoomInfoHandler
{
    public class GetRoomInfoMessage : BaseMessage
    {
        public string playerId { get; set; }
        public string roomCode { get; set; }
        public List<RoomInfo> RoomInfos { get; set; }
        public string hostId { get; set; }
        public GetRoomInfoMessage(
            string playerId,
            string roomCode,
            List<RoomInfo> RoomInfos,
            string hostId
        )
        {
            type = "room_info";
            this.playerId = playerId;
            this.roomCode = roomCode;
            this.RoomInfos = RoomInfos;
            this.hostId = hostId;
        }
    }
    public async Task HandleAsync(string playerId, string rawMessage, Room room, IWebSocketBroadcaster broadcaster)
    {
        var msg = JsonSerializer.Deserialize<GetRoomInfoMessage>(rawMessage);
        if (room == null)
        {
            LogManager.Error("[GetRoomInfoHandler] 플레이어가 속한 방을 찾을 수 없음", room.RoomCode, playerId);
            return;
        }
        if (msg == null)
        {
            LogManager.Error("[GetRoomInfoHandler] 잘못된 메시지 수신", room.RoomCode, playerId);
            return;
        }
        //이미 플레이어는 상위에서 브로드캐스터에 add 되어있음
        await broadcaster.BroadcastAsync(new GetRoomInfoMessage(playerId, room.RoomCode, room.RoomInfos, room.HostId));
        
    }
}
