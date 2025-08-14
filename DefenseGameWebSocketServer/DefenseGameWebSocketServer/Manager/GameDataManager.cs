using DefenseGameWebSocketServer.Models.DataModels;
using DefenseGameWebSocketServer.Util;

namespace DefenseGameWebSocketServer.Manager
{
    public class GameDataManager
    {
        public static GameDataManager Instance { get; } = new GameDataManager();

        private Dictionary<string, object> _tableDict = new();

        public void LoadAllData()
        {
            _tableDict["card_data"] = CsvLoader.Load<CardData>("Data/card_data.csv");
            _tableDict["player_data"] = CsvLoader.Load<PlayerData>("Data/player_data.csv");
            _tableDict["skill_data"] = CsvLoader.Load<SkillData>("Data/skill_data.csv");
            _tableDict["enemy_data"] = CsvLoader.Load<EnemyData>("Data/enemy_data.csv");
            _tableDict["wave_data"] = CsvLoader.Load<WaveData>("Data/wave_data.csv");
            _tableDict["wave_round_data"] = CsvLoader.Load<WaveRoundData>("Data/wave_round_data.csv");
            _tableDict["shared_data"] = CsvLoader.Load<SharedData>("Data/shared_data.csv");
            _tableDict["bullet_data"] = CsvLoader.Load<BulletData>("Data/bullet_data.csv");

            _tableDict["boss_data"] = CsvLoader.Load<BossData>("Data/boss_data.csv");
            _tableDict["boss_pattern_data"] = CsvLoader.Load<BossPatternData>("Data/boss_pattern_data.csv");

            Console.WriteLine("[GameDataManager] 모든 데이터 로드 완료!");
        }

        public Dictionary<int, T> GetTable<T>(string tableName)
        {
            if (!_tableDict.TryGetValue(tableName, out var tableObj))
            {
                Console.WriteLine($"[Error] 테이블 {tableName} 없음");
                return null;
            }

            var table = tableObj as Dictionary<int, T>;

            if (table == null)
            {
                Console.WriteLine($"[Error] 테이블 {tableName} 타입 오류");
                return null;
            }

            return table;
        }

        public T GetData<T>(string tableName, int id) where T : class
        {
            var table = GetTable<T>(tableName);

            if (table == null)
                return null;

            if (table.TryGetValue(id, out var data))
                return data;

            Console.WriteLine($"[Warning] 테이블 {tableName} → ID {id} 없음");
            return null;
        }
    }
}
