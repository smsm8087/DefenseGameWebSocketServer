using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using System;
using System.Diagnostics;

public class EnemySyncScheduler
{
    private readonly List<Enemy> _enemies;
    private readonly List<Enemy> _deadEnemies = new List<Enemy>();
    private readonly IWebSocketBroadcaster _broadcaster;
    private readonly CancellationToken _token;
    private readonly object _lock = new();
    private readonly SharedHpManager _sharedHpManager;
    private bool _isRunning = false;

    public EnemySyncScheduler(List<Enemy> enemies, IWebSocketBroadcaster broadcaster, CancellationToken token, SharedHpManager sharedHpManager)
    {
        _enemies = enemies;
        _broadcaster = broadcaster;
        _token = token;
        _sharedHpManager = sharedHpManager;
    }
    public void Stop()
    {
        lock (_lock)
        {
            _isRunning = false;
            _enemies.Clear();
            _deadEnemies.Clear();
            Console.WriteLine("[EnemySyncScheduler] 중지");
        }
    }
    public async Task StartAsync()
    {
        if (_isRunning) return;
        _isRunning = true;

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
                    if (enemy.CheckArrived())
                    {
                        _deadEnemies.Add(enemy);
                    }
                }
                foreach (var enemy in _deadEnemies)
                {
                    _enemies.Remove(enemy);
                    Console.WriteLine($"[서버] {enemy.Id} → 크리스탈 도착");
                }

                syncList = _enemies.Select(e => new EnemySyncPacket(e.Id, e.X)).ToList();
            }

            var msg = new EnemySyncMessage(syncList);
            await _broadcaster.BroadcastAsync(msg);

            //죽은 적 처리
            var deadEnemiesList = _deadEnemies.Select(e => e.Id).ToList();
            if(deadEnemiesList.Count > 0)
            {
                var dieMsg = new EnemyDieMessage(deadEnemiesList);
                await _broadcaster.BroadcastAsync(dieMsg);

                // 공유 HP 감소 처리
                for (int i = 0; i < deadEnemiesList.Count; i++)
                {
                    _sharedHpManager.TakeDamage();
                }
                var sharedHpMsg = new SharedHpMessage(_sharedHpManager.getHpStatus().Item1, _sharedHpManager.getHpStatus().Item2);
                await _broadcaster.BroadcastAsync(sharedHpMsg);
                _deadEnemies.Clear();
            }

            // 정확한 프레임 간격 맞추기
            var elapsed = (sw.ElapsedTicks - nowTicks) / (float)Stopwatch.Frequency;
            int sleepMs = Math.Max(0, (int)((targetFrameTime - elapsed) * 1000));
            await Task.Delay(sleepMs, _token);
        }
        Console.WriteLine("[EnemySyncScheduler] 스케줄러 종료됨");
        _isRunning = false; 
    }
    public void Dispose()
    {
        Console.WriteLine("[EnemySyncScheduler] Dispose 호출됨");
        Stop(); 
    }
}

