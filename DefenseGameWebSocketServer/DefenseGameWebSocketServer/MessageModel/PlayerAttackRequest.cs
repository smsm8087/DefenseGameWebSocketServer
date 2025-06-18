using DefenseGameWebSocketServer.Model;

namespace DefenseGameWebSocketServer.MessageModel
{
    public class PlayerAttackRequest : BaseMessage
    {
        public string playerId { get; set; }
        public string targetEnemyId { get; set; }

        public float attackBoxCenterX { get; set; }
        public float attackBoxCenterY { get; set; }
        public float attackBoxWidth { get; set; }
        public float attackBoxHeight { get; set; }

        public PlayerAttackRequest(
            string playerId,
            string targetEnemyId,
            float attackBoxCenterX,
            float attackBoxCenterY,
            float attackBoxWidth,
            float attackBoxHeight
        )
        {
            type = "player_attack";
            this.playerId = playerId;
            this.targetEnemyId = targetEnemyId;
            this.attackBoxCenterX = attackBoxCenterX;
            this.attackBoxCenterY = attackBoxCenterY;
            this.attackBoxWidth = attackBoxWidth;
            this.attackBoxHeight = attackBoxHeight;
        }
    }
}