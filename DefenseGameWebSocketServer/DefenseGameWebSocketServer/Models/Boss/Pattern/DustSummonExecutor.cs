using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Models.DataModels;

public class DustSummonExecutor : IBossPatternExecutor
{
    public void Execute(Boss boss, BossPatternData pattern, IWebSocketBroadcaster broadcaster, EnemyManager enemyManager, SharedHpManager sharedHpManager, WaveData waveData, List<WaveRoundData> waveRoundDataList)
    {
        List<(float, float)> spawnPositions = new List<(float, float)>();
        
        // 1. 소환 위치 선정 
        float x = -10f + boss.x + new Random().NextSingle() * 20f; // -10 ~ +10 범위 랜덤
        float y = -3f;
        for (int i = 0; i < pattern.enemy_summon_count; i++)
        {
            // 소환 위치를 약간씩 랜덤하게 변경
            float offsetX = -5f + new Random().NextSingle() * 10f; // -1 ~ +1 범위 랜덤
            spawnPositions.Add((x + offsetX, y));
        }

        
       
        // 2. 마법진 생성 메시지 브로드캐스트
        broadcaster.BroadcastAsync( 
            new
            {
                type = "boss_dust_warning",
                spawnPositions = spawnPositions,
            }
        );

        // 3. 몇 초 뒤 먼지 몬스터 실제로 소환
        _ = Task.Delay(TimeSpan.FromSeconds(1.5)).ContinueWith(_ =>
        {
            for(int i = 0; i < pattern.enemy_summon_count; i++)
            {
                enemyManager.SpawnDustEnemy(spawnPositions[i].Item1, spawnPositions[i].Item2, pattern.enemy_table_id , sharedHpManager, waveData, waveRoundDataList);
            }
        });
    }
}
