using DefenseGameWebSocketServer.Model;

namespace DefenseGameWebSocketServer.MessageModel
{
    public class BossDamageInfo
    {
        public string playerId { get; set; }
        public int currentHp { get; set; }
        public int maxHp { get; set; }
        public int damage { get; set; }
        public bool isCritical { get; set; }
    }
    public class BossDamagedMessage : BaseMessage
    {
        public BossDamageInfo damagedBoss { get; set; }

        public BossDamagedMessage()
        {
            type = "boss_damaged";
            damagedBoss = new BossDamageInfo();
        }
    }
}