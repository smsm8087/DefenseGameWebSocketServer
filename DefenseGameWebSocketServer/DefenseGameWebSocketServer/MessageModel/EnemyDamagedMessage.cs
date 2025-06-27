using DefenseGameWebSocketServer.Model;

namespace DefenseGameWebSocketServer.MessageModel
{
    public class EnemyDamageInfo
    {
        public string enemyId { get; set; }
        public int currentHp { get; set; }
        public int maxHp { get; set; }
        public int damage { get; set; }
        public bool isCritical { get; set; }
    }
    public class EnemyDamagedMessage : BaseMessage
    {
        public List<EnemyDamageInfo> damagedEnemies { get; set; }

        public EnemyDamagedMessage()
        {
            type = "enemy_damaged";
            damagedEnemies = new List<EnemyDamageInfo>();
        }
    }
}