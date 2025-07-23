using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.MessageModel;
using DefenseGameWebSocketServer.Models;
using System.Text.Json;

namespace DefenseGameWebSocketServer.Handlers
{
    public class StartRevivalHandler
    {
        public async Task HandleAsync(string playerId, string rawMessage, IWebSocketBroadcaster broadcaster, RevivalManager revivalManager)
        {
            Room room = RoomManager.Instance.GetRoomByPlayerId(playerId);
            LogManager.Info($"[StartRevivalHandler] {playerId} 부활 요청 처리 시작", room.RoomCode, playerId);
            LogManager.Info($"[StartRevivalHandler] 받은 메시지: {rawMessage}", room.RoomCode, playerId);
            
            var msg = JsonSerializer.Deserialize<StartRevivalRequest>(rawMessage);
            if (msg == null) 
            {
                LogManager.Error($"[StartRevivalHandler] StartRevivalRequest 파싱 실패: {rawMessage}", room.RoomCode, playerId);
                return;
            }

            LogManager.Info($"[StartRevivalHandler] 부활 요청 reviver={playerId}, target={msg.targetId}", room.RoomCode, playerId);
            
            bool success = await revivalManager.StartRevival(playerId, msg.targetId);
            
            LogManager.Info($"[StartRevivalHandler] 부활 요청 처리 결과: {success}", room.RoomCode, playerId);
            
            if (!success)
            {
                LogManager.Info($"[StartRevivalHandler] {playerId}가 {msg.targetId} 부활 시작 실패", room.RoomCode, playerId);
            }
            else
            {
                LogManager.Info($"[StartRevivalHandler] {playerId}가 {msg.targetId} 부활 시작 성공", room.RoomCode, playerId);
            }
        }
    }
}