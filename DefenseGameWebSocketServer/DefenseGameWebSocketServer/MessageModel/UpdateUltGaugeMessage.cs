namespace DefenseGameWebSocketServer.Model
{
    public class UpdateUltGaugeMessage : BaseMessage
    {
        public float currentUlt { get; set; }
        public float maxUlt { get; set; }
        public UpdateUltGaugeMessage(
            float currentUlt,
            float maxUlt
        )
        {
            type = "update_ult_gauge";
            this.currentUlt = currentUlt;
            this.maxUlt = maxUlt;
        }
    }
}
