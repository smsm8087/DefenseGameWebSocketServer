namespace DefenseGameWebSocketServer.Model
{
    public class SharedHpMessage : BaseMessage
    {
        public float currentHp { get; set; }
        public float maxHp { get; set; }
        public SharedHpMessage(
            string type,
            float currentHp,
            float maxHp
        )
        {
            this.type = type;
            this.currentHp = currentHp;
            this.maxHp = maxHp;
        }
    }
}
