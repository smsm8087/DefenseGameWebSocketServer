using DefenseGameWebSocketServer.Handlers;
using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models.DataModels;
using Newtonsoft.Json;

public class SettlementReadyHandler
{
    public async Task HandleAsync(
        string playerId,
        string rawMessage,
        IWebSocketBroadcaster broadcaster,
        WaveScheduler waveScheduler,
        PlayerManager playerManager
    )
    {
        var msg = JsonConvert.DeserializeObject<SettlementReadyMessage>(rawMessage);
        playerManager.addCardToPlayer(playerId, msg.selectedCardId);
        playerManager.TryGetPlayer(playerId, out Player player);
        if (player == null)
        {
            Console.WriteLine($"[SettlementReadyHandler] 플레이어 {playerId} 정보가 없습니다.");
            return;
        }
        var response = new UpdatePlayerDataMessage(new PlayerInfo
        {
            id = playerId,
            currentMoveSpeed = player.currentMoveSpeed,
        });
        await broadcaster.SendToAsync(playerId, response);
        waveScheduler.PlayerReady(playerId);
    }
}