using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models.DataModels;

namespace DefenseGameWebSocketServer.MessageModel
{
    public class SettlementTimerUpdateMessage : BaseMessage
    {
        public float duration { get; set; }
        public bool isReady { get; set; } = false; // 기본값 false로 설정
        public SettlementTimerUpdateMessage(
            float duration,
            bool isReady
        )
        {
            type = "settlement_timer_update";
            this.duration = duration;
            this.isReady = isReady;
        }
    }
}
