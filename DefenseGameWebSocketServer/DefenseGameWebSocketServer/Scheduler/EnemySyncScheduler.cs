using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using System;
using System.Diagnostics;

public class EnemySyncScheduler
{
    private readonly List<Enemy> _enemies;
    private readonly List<Enemy> _deadEnemies = new List<Enemy>();
    private readonly List<Enemy> _arrivedEnemies = new List<Enemy>(); // 도착한 적들 관리
    private readonly IWebSocketBroadcaster _broadcaster;
    private readonly CancellationToken _token;
    private readonly object _lock = new();
    private readonly SharedHpManager _sharedHpManager;
    private bool _isRunning = false;
    private DateTime _lastAttackTime = DateTime.Now;
    private const int ATTACK_INTERVAL_MS = 2000; // 2초

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
            _arrivedEnemies.Clear();
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
                    _arrivedEnemies.Add(enemy); // 도착한 적을 별도 리스트에 추가
                    _sharedHpManager.TakeDamage(1);
                    Console.WriteLine($"[서버] ID: {enemy.Id} X: {enemy.X} Y: {enemy.Y} → 크리스탈 도착");
                }
                _deadEnemies.Clear();

                syncList = _enemies.Select(e => new EnemySyncPacket(e.Id, e.X)).ToList();
            }

            var msg = new EnemySyncMessage(syncList);
            await _broadcaster.BroadcastAsync(msg);

            // 2초마다 도착한 적들이 공격
            if ((DateTime.Now - _lastAttackTime).TotalMilliseconds >= ATTACK_INTERVAL_MS)
            {
                await ProcessArrivedEnemiesAttack();
                _lastAttackTime = DateTime.Now;
            }

            // 정확한 프레임 간격 맞추기
            var elapsed = (sw.ElapsedTicks - nowTicks) / (float)Stopwatch.Frequency;
            int sleepMs = Math.Max(0, (int)((targetFrameTime - elapsed) * 1000));
            await Task.Delay(sleepMs, _token);
        }
        
        Console.WriteLine("[EnemySyncScheduler] 스케줄러 종료됨");
        _isRunning = false; 
    }

    private async Task ProcessArrivedEnemiesAttack()
    {
        if (_arrivedEnemies.Count > 0)
        {
            // 도착한 적 수만큼 데미지 처리
            for (int i = 0; i < _arrivedEnemies.Count; i++)
            {
                _sharedHpManager.TakeDamage(1);
            }
            
            Console.WriteLine($"[서버] 도착한 적 {_arrivedEnemies.Count}마리가 2초마다 공격! 총 데미지: {_arrivedEnemies.Count}");
            
            var sharedHpMsg = new SharedHpMessage(_sharedHpManager.getHpStatus().Item1, _sharedHpManager.getHpStatus().Item2);
            await _broadcaster.BroadcastAsync(sharedHpMsg);
        }
    }
    
    public void Dispose()
    {
        Console.WriteLine("[EnemySyncScheduler] Dispose 호출됨");
        Stop(); 
    }
}