using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using System.Text.Json;

public class StartGameHandler
{
    public async Task HandleAsync(string playerId, string rawMessage, IWebSocketBroadcaster broadcaster, GameManager gameManager)
    {
        var msg = JsonSerializer.Deserialize<StartRoomMessage>(rawMessage);
        if (msg == null)
        {
            Console.WriteLine("[JoinRoomHandler] 잘못된 메시지 수신");
            return;
        }
        await gameManager.TryConnectGame();
    }
}
