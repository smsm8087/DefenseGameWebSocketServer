namespace DefenseGameWebSocketServer.Model
{
    public class SharedHpMessage : BaseMessage
    {
        public float currentHp { get; set; }
        public float maxHp { get; set; }
        public SharedHpMessage(
            float currentHp,
            float maxHp
        )
        {
            this.type = "shared_hp_update";
            this.currentHp = currentHp;
            this.maxHp = maxHp;
        }
    }
}
