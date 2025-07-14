using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.MessageModel;
using DefenseGameWebSocketServer.Model;
using System.Text.Json;
using System.Threading.Tasks;

namespace DefenseGameWebSocketServer.Handlers
{
    public class StartRevivalHandler
    {
        public async Task HandleAsync(string playerId, string rawMessage, IWebSocketBroadcaster broadcaster, RevivalManager revivalManager)
        {
            Console.WriteLine($"[서버] StartRevivalHandler 시작 - playerId: {playerId}");
            Console.WriteLine($"[서버] 받은 메시지: {rawMessage}");
            
            var msg = JsonSerializer.Deserialize<StartRevivalRequest>(rawMessage);
            if (msg == null) 
            {
                Console.WriteLine($"[서버] StartRevivalRequest 파싱 실패");
                return;
            }

            Console.WriteLine($"[서버] 부활 요청: reviver={playerId}, target={msg.targetId}");
            
            bool success = await revivalManager.StartRevival(playerId, msg.targetId);
            
            Console.WriteLine($"[서버] 부활 요청 결과: {success}");
            
            if (!success)
            {
                Console.WriteLine($"[StartRevivalHandler] {playerId}가 {msg.targetId} 부활 시작 실패");
            }
            else
            {
                Console.WriteLine($"[StartRevivalHandler] {playerId}가 {msg.targetId} 부활 시작 성공");
            }
        }
    }
}