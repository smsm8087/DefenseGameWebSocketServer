using DefenseGameWebSocketServer.Manager;

public class WaveScheduler
{
    private readonly IWebSocketBroadcaster _broadcaster;
    private readonly CancellationTokenSource _cts;
    private readonly object _lock = new();
    private readonly Func<bool> _hasPlayerCount;
    private readonly SharedHpManager _sharedHpManager;
    private EnemyManager _enemyManager;
    private CountDownScheduler _countDownScheduler;

    private int _wave = 0;
    private bool _isRunning = false;
    private int maxWave = 10; // 최대 웨이브 수

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

            //count down 스케줄러
            _countDownScheduler = new CountDownScheduler(_broadcaster, _cts, _hasPlayerCount);
            //enemyManager 시작
            _enemyManager.setCancellationTokenSource(_cts);
            _ = _enemyManager.StartAsync();

            //웨이브 스케줄러 시작
            _ = StartAsync();
            // Reset shared HP manager
            _sharedHpManager.Reset();
        }
    }
    public void Stop()
    {
        lock (_lock)
        {
            if (!_isRunning) return;
            _isRunning = false;
            //init wave
            _wave = 0;
            //enemyManager 중지
            _enemyManager.Stop();

            Console.WriteLine("[WaveScheduler] 중지");
        }
    }
    public async Task StartAsync()
    {
        Console.WriteLine("[WaveScheduler] 웨이브 스케줄러 시작됨");
        
        //5초후 시작
        await Task.Delay(5000, _cts.Token);

        //countdown 시작
        await _countDownScheduler.StartAsync();

        while (!_cts.Token.IsCancellationRequested && _wave <= maxWave)
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