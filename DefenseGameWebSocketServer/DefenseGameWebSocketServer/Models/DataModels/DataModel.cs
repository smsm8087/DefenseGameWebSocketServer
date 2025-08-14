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
        public List<float> hit_offset { get; set; }
        public List<float> hit_size { get; set; }
        public float base_scale { get; set; }
    }
    public class SkillData
    {
        public int id { get; set; }
        public string job { get; set; }
        public string skill_type { get; set; }
        public string desc_ko { get; set; }
        public string default_key { get; set; }
        public float cooldown { get; set; }
        public float cast_time { get; set; }
        public float aoe_radius { get; set; }
        public float taunt_duration { get; set; }
        public float dash_distance { get; set; }
        public float dash_speed { get; set; }
        public float damage_multiplier { get; set; }
        public float knockback_distance { get; set; }
        public float stun_duration { get; set; }
        public float damage_reduction { get; set; }
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
        public float aggro_radius { get; set; }
        public List<float> bullet_offset { get; set; }
        public float aggro_attack_count { get; set; }
        public float aggro_cool_down { get; set; }
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
        public int boss_wave { get; set; }
        public int boss_table_id { get; set; }
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
    public class BossData
    {
        public int id { get; set; }
        public string name { get; set; }
        public int max_hp { get; set; }
        public string intro_clip { get; set; }
        public int pattern_table { get; set; }
        public bool use_random_pattern { get; set; }
        public List<float> spawn_pos { get; set; }
        public float aggro_cool_down { get; set; }
        public float speed { get; set; }
        public float range { get; set; }
        public float base_width { get; set; }
        public float base_height { get; set; }
        public float base_scale { get; set; }
        public float base_offsetx { get; set; }
        public float base_offsety { get; set; }
    }
    public class BossPatternData
    {
        public int id { get; set; }
        public int boss_id { get; set; }
        public string pattern_name { get; set; }
        public float delay_after_start { get; set; }
        public bool use_target { get; set; }
        public int enemy_table_id { get; set; }
        public int enemy_summon_count { get; set; }
    }
}
