namespace DefenseGameWebSocketServer.Models.DataModels
{
    public class CardData
    {
        public int id { get; set; }
        public string title { get; set; }
        public string type { get; set; }
        public string grade { get; set; }
        public int value { get; set; }
        public int pct { get; set; }
    }
    public class PlayerData
    {
        public int id { get; set; }
        public string job_type { get; set; }
        public int hp { get; set; }
        public float ult_gauge { get; set; }
        public int attack_power { get; set; }
        public float move_speed { get; set; }
        public int critical_pct { get; set; }
        public int critical_dmg { get; set; }
    }
}
