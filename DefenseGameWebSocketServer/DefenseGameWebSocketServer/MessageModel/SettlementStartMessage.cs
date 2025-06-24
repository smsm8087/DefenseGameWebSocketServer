using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models.DataModels;

namespace DefenseGameWebSocketServer.MessageModel
{
    public class SettlementStartMessage : BaseMessage
    {
        public string playerId { get; set; }
        public int duration { get; set; }
        public List<CardData> cards { get; set; }
        public SettlementStartMessage(
            string playerId,
            int duration,
            List<CardData> cards
        )
        {
            type = "settlement_start";
            this.playerId = playerId;
            this.duration = duration;
            this.cards = cards;
        }
    }
}
