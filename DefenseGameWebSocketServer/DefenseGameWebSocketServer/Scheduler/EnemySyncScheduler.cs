using DefenseGameWebSocketServer.Model;
using System;
using System.Diagnostics;

public class EnemySyncScheduler
{
    private readonly List<Enemy> _enemies;
    private readonly IWebSocketBroadcaster _broadcaster;
    private readonly CancellationToken _token;

    public EnemySyncScheduler(List<Enemy> enemies, IWebSocketBroadcaster broadcaster, CancellationToken token)
    {
        _enemies = enemies;
        _broadcaster = broadcaster;
        _token = token;
    }

    public async Task StartAsync()
    {
        float targetFrameTime = 0.1f; // 100ms
        var sw = new Stopwatch();
        sw.Start();
        long lastTicks = sw.ElapsedTicks;

        while (!_token.IsCancellationRequested)
        {
            long nowTicks = sw.ElapsedTicks;
            float deltaTime = (nowTicks - lastTicks) / (float)Stopwatch.Frequency;
            lastTicks = nowTicks;

            List<EnemySyncPacket> syncList;
            lock (_enemies)
            {
                foreach (var enemy in _enemies)
                {
                    enemy.Update(targetFrameTime);
                }
                syncList = _enemies.Select(e => new EnemySyncPacket(e.Id, e.X)).ToList();
            }

            var msg = new EnemySyncMessage("enemy_sync", syncList);
            await _broadcaster.BroadcastAsync(msg);

            // 정확한 프레임 간격 맞추기
            var elapsed = (sw.ElapsedTicks - nowTicks) / (float)Stopwatch.Frequency;
            int sleepMs = Math.Max(0, (int)((targetFrameTime - elapsed) * 1000));
            await Task.Delay(sleepMs, _token);
        }
    }
}

