using DefenseGameWebSocketServer.Handlers;
using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
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
        waveScheduler.PlayerReady();
    }
}