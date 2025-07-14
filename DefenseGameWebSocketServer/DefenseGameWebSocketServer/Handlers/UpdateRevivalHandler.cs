using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.MessageModel;
using DefenseGameWebSocketServer.Model;
using System.Text.Json;
using System.Threading.Tasks;

namespace DefenseGameWebSocketServer.Handlers
{
    public class UpdateRevivalHandler
    {
        public async Task HandleAsync(string playerId, string rawMessage, IWebSocketBroadcaster broadcaster, RevivalManager revivalManager)
        {
            Console.WriteLine($"[서버] UpdateRevivalHandler 시작 - playerId: {playerId}");
            Console.WriteLine($"[서버] 받은 메시지: {rawMessage}");
        
            var msg = JsonSerializer.Deserialize<UpdateRevivalRequest>(rawMessage);
            if (msg == null) 
            {
                Console.WriteLine($"[서버] UpdateRevivalRequest 파싱 실패");
                return;
            }

            Console.WriteLine($"[서버] 진행률 업데이트 요청: targetId={msg.targetId}, progress={msg.progress}");
        
            bool success = await revivalManager.UpdateRevival(playerId, msg.targetId, msg.progress);
        
            Console.WriteLine($"[서버] UpdateRevival 결과: {success}");
        
            if (!success)
            {
                Console.WriteLine($"[서버] {playerId}의 부활 진행 업데이트 실패");
            }
        }
    }
}