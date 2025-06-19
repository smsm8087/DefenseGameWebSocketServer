using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using System;
using System.Diagnostics;

public class EnemySyncScheduler
{
    private readonly EnemyManager _enemyManager;
    private readonly CancellationToken _token;
    private readonly object _lock = new();
    private bool _isRunning = false;

    public EnemySyncScheduler(EnemyManager enemyManager, CancellationToken token)
    {
        _enemyManager = enemyManager;
        _token = token;
    }
    public void Stop()
    {
        lock (_lock)
        {
            _isRunning = false;
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

            await _enemyManager.SyncEnemy(targetFrameTime);

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