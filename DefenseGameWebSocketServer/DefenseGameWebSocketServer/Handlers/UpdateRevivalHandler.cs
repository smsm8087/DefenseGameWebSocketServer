using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.MessageModel;
using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models;
using System.Text.Json;

namespace DefenseGameWebSocketServer.Handlers
{
    public class UpdateRevivalHandler
    {
        public async Task HandleAsync(string playerId, string rawMessage, IWebSocketBroadcaster broadcaster, RevivalManager revivalManager)
        {
            Room room = RoomManager.Instance.GetRoomByPlayerId(playerId);
            LogManager.Info($"[UpdateRevivalHandler] UpdateRevivalHandler 시작 - playerId: {playerId}", room.RoomCode, playerId);
            LogManager.Info($"[UpdateRevivalHandler] 받은 메시지: {rawMessage}", room.RoomCode, playerId);
        
            var msg = JsonSerializer.Deserialize<UpdateRevivalRequest>(rawMessage);
            if (msg == null) 
            {
                LogManager.Error($"[UpdateRevivalHandler] UpdateRevivalRequest 파싱 실패: {rawMessage}", room.RoomCode, playerId);
                return;
            }

            LogManager.Info($"[UpdateRevivalHandler] 진행률 업데이트 요청: targetId={msg.targetId}, progress={msg.progress}", room.RoomCode, playerId);
        
            bool success = await revivalManager.UpdateRevival(playerId, msg.targetId, msg.progress);
        
            LogManager.Info($"[UpdateRevivalHandler] UpdateRevival 결과: {success}", room.RoomCode, playerId);
        
            if (!success)
            {
                LogManager.Info($"[UpdateRevivalHandler] 부활 진행 업데이트 실패: playerId={playerId}, targetId={msg.targetId}, progress={msg.progress}", room.RoomCode, playerId);
            }
        }
    }
}