using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models.DataModels;

namespace DefenseGameWebSocketServer.MessageModel
{
    public class SettlementStartMessage : BaseMessage
    {
        public string playerId { get; set; }
        public float duration { get; set; }
        public List<CardData> cards { get; set; }
        public int alivePlayerCount { get; set; }
        public SettlementStartMessage(
            string playerId,
            float duration,
            List<CardData> cards,
            int alivePlayerCount
        )
        {
            type = "settlement_start";
            this.playerId = playerId;
            this.duration = duration;
            this.cards = cards;
            this.alivePlayerCount = alivePlayerCount;
        }
    }
}
