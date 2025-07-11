﻿namespace DefenseGameWebSocketServer.Model
{
    public class BaseMessage
    {
        public string type { get; set; }
    }
    
    public enum MessageType
    {
        Move,
        Restart,
        PlayerAnimation,
        PlayerAttack,
        EnemyAttackHit,
        AttackSuccess,
        SettlementReady,
        Unknown,
    }

    public static class MessageTypeHelper
    {
        public static MessageType Parse(string type)
        {
            return type switch
            {
                "move" => MessageType.Move,
                "restart" => MessageType.Restart,
                "player_animation" => MessageType.PlayerAnimation,
                "player_attack" => MessageType.PlayerAttack,
                "enemy_attack_hit" => MessageType.EnemyAttackHit,
                "attack_success" => MessageType.AttackSuccess,
                "settlement_ready" => MessageType.SettlementReady,
                _ => MessageType.Unknown,
            };
        }
    }
}