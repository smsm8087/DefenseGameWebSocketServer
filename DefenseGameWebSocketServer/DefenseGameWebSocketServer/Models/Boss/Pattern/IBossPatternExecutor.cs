using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Models.DataModels;
public interface IBossPatternExecutor
{
    void Execute(Boss boss, BossPatternData pattern, IWebSocketBroadcaster broadcaster, EnemyManager enemyManager, SharedHpManager sharedHpManager, WaveData waveData, List<WaveRoundData> waveRoundDataList);
}
public static class PatternFactory
{
    public static IBossPatternExecutor Create(string patternName)
    {
        return patternName switch
        {
            //"LaserBeam" => new LaserBeamExecutor(),
            "DustSummon" => new DustSummonExecutor(),
            //"ConfuseDebuff" => new ConfuseDebuffExecutor(),
            _ => null
        };
    }
}
