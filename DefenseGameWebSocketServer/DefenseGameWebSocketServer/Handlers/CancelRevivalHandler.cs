using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.MessageModel;
using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models;
using System.Text.Json;
using System.Threading.Tasks;

namespace DefenseGameWebSocketServer.Handlers
{
    public class CancelRevivalHandler
    {
        public async Task HandleAsync(string playerId, string rawMessage, IWebSocketBroadcaster broadcaster, RevivalManager revivalManager)
        {
            var msg = JsonSerializer.Deserialize<CancelRevivalRequest>(rawMessage);
            if (msg == null) return;

            await revivalManager.CancelRevival(msg.targetId, "player_cancelled");
            Room room = RoomManager.Instance.GetRoomByPlayerId(playerId);
            LogManager.Info($"[CancelRevivalHandler] {playerId}가 {msg.targetId} 부활 취소", room.RoomCode, playerId);
        }
    }
}