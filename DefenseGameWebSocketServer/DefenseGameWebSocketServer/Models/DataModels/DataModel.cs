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
        public int need_percent { get; set; }
    }
    public class PlayerData
    {
        public int id { get; set; }
        public string job_type { get; set; }
        public int hp { get; set; }
        public float ult_gauge { get; set; }
        public int attack_power { get; set; }
        public float attack_speed { get; set; }
        public float move_speed { get; set; }
        public int critical_pct { get; set; }
        public int critical_dmg { get; set; }
    }
    public class EnemyData
    {
        public int id { get; set; }
        public string type { get; set; }
        public string prefab_path { get; set; }
        public int hp { get; set; }
        public float speed { get; set; }
        public float attack { get; set; }
        public float defense { get; set; }
        public float base_width{ get; set; }
        public float base_height { get; set; }
        public float base_scale { get; set; }
        public float base_offsetx { get; set; }
        public float base_offsety { get; set; }
        public string target_type { get; set; }
        public List<float> spawn_left_pos { get; set; }
        public List<float> spawn_right_pos { get; set; }
        public string attack_type { get; set; }
        public int bullet_id { get; set; }
    }
    public class WaveRoundData
    {
        public int id { get; set; }
        public int wave_id { get; set; }
        public int round_index { get; set; }
        public List<int> enemy_ids { get; set; }
        public List<int> enemy_counts { get; set; }
        public float add_movespeed { get; set; }
        public int add_hp { get; set; }
        public float add_attack { get; set; }
        public float add_defense { get; set; }
    }
    public class WaveData
    {
        public int id { get; set; }
        public string title { get; set; }
        public string difficulty { get; set; }
        public int max_wave { get; set; }
        public int settlement_phase_round { get; set; }
        public string background { get; set; }
        public int shared_hp_id { get; set; }
    }
    public class SharedData
    {
        public int id { get; set; }
        public string prefab_path { get; set; }
        public float radius { get; set; }
        public List<float> pos { get; set; }
        public float hp { get; set; }
    }
    public class BulletData
    {
        public int id { get; set; }
        public string name { get; set; }
        public float speed { get; set; }
        public float range { get; set; }
        public string prefab_path { get; set; }
    }
}
