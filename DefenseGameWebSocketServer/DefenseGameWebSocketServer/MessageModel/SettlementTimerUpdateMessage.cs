using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models.DataModels;

namespace DefenseGameWebSocketServer.MessageModel
{
    public class SettlementTimerUpdateMessage : BaseMessage
    {
        public float duration { get; set; }
        public int readyCount { get; set; }
        public SettlementTimerUpdateMessage(
            float duration,
            int readyCount
        )
        {
            type = "settlement_timer_update";
            this.duration = duration;
            this.readyCount = readyCount;
        }
    }
}
