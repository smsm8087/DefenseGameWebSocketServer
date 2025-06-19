using DefenseGameWebSocketServer.Manager;

public class WaveScheduler
{
    private readonly IWebSocketBroadcaster _broadcaster;
    private readonly CancellationTokenSource _cts;
    private readonly object _lock = new();
    private readonly Func<bool> _hasPlayerCount;
    private readonly SharedHpManager _sharedHpManager;
    private readonly EnemyManager _enemyManager;

    private EnemySyncScheduler _enemySyncScheduler;
    private  CountDownScheduler _countDownScheduler;

    private int _wave = 0;
    private bool _isRunning = false;

    public WaveScheduler(IWebSocketBroadcaster broadcaster, CancellationTokenSource cts, Func<bool> hasPlayerCount, SharedHpManager sharedHpManager, EnemyManager enemyManager)
    {
        _broadcaster = broadcaster;
        _cts = cts;
        _hasPlayerCount = hasPlayerCount;
        _sharedHpManager = sharedHpManager;
        _enemyManager = enemyManager;
    }
    public void TryStart()
    {
        lock (_lock)
        {
            if (_isRunning) return;
            _isRunning = true;

            //웨이브 스케줄러 시작
            _ = StartAsync();

            //enemy sync 스케줄러 시작
            _enemySyncScheduler = new EnemySyncScheduler(_enemyManager, _cts.Token);
            _ = _enemySyncScheduler.StartAsync();

            //count down 스케줄러
            _countDownScheduler = new CountDownScheduler(_broadcaster, _cts, _hasPlayerCount);
        }
    }
    public void Stop()
    {
        lock (_lock)
        {
            if (!_isRunning) return;
            _isRunning = false;
            //init wave
            _enemyManager.ClearEnemies();
            _wave = 0;

            //init enemy sync scheduler
            _enemySyncScheduler.Dispose();

            Console.WriteLine("[WaveScheduler] 중지");

            // Reset shared HP manager
            _sharedHpManager.Reset(); 
        }
    }
    public async Task StartAsync()
    {
        Console.WriteLine("[WaveScheduler] 웨이브 스케줄러 시작됨");
        
        //5초후 시작
        await Task.Delay(5000, _cts.Token);

        //countdown 시작
        await _countDownScheduler.StartAsync();

        while (!_cts.Token.IsCancellationRequested)
        {
            _wave++;
            Console.WriteLine($"[WaveScheduler] 웨이브 {_wave} 시작");
            await _enemyManager.SpawnEnemy(_wave);
            await Task.Delay(8000, _cts.Token);
        }

        Console.WriteLine("[WaveScheduler] 웨이브 스케줄러 종료됨");
    }
   
    public void Dispose()
    {
        Console.WriteLine("[WaveScheduler] Dispose 호출됨");
        Stop(); 
    }
}