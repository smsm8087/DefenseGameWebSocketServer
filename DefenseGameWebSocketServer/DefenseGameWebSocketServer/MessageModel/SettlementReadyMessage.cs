namespace DefenseGameWebSocketServer.Model
{
    public class SettlementReadyMessage : BaseMessage
    {
        string playerId { get; set; }
        public int selectedCardId { get; set; }
        public SettlementReadyMessage(
            string playerId, 
            int selectedCardId
        )
        {
            type = "settlement_ready";
            this.playerId = playerId;
            this.selectedCardId = selectedCardId;
        }
    }
}
