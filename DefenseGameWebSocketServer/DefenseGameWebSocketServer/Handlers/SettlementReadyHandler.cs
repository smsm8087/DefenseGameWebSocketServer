using DefenseGameWebSocketServer.Handlers;
using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models;
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
            Room room = RoomManager.Instance.GetRoomByPlayerId(playerId);
            LogManager.Info($"[SettlementReadyHandler] 플레이어 {playerId} 정보가 없습니다.", room.RoomCode, playerId);
            return;
        }
        var response = new UpdatePlayerDataMessage(new PlayerInfo
        {
            id = playerId,
            job_type = player.jobType,
            currentMaxHp = player.playerBaseData.hp + player.addData.addHp,
            currentUltGauge = player.playerBaseData.ult_gauge + player.addData.addUlt,
            currentMoveSpeed = player.currentMoveSpeed,
            currentAttackSpeed = player.currentAttackSpeed,
            currentCriPct = player.playerBaseData.critical_pct + player.addData.addCriPct,
            currentCriDmg = player.playerBaseData.critical_dmg + player.addData.addCriDmg,
            currentAttack = player.playerBaseData.attack_power + player.addData.addAttackPower,
            cardIds = player.CardIds,
        });
        await broadcaster.SendToAsync(playerId, response);
        waveScheduler.PlayerReady(playerId);
    }
}