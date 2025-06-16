using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;

public class WaveScheduler
{
    private readonly IWebSocketBroadcaster _broadcaster;
    private readonly CancellationTokenSource _cts;
    private int _wave = 0;
    private readonly Random _rand = new();
    private bool _isRunning = false;
    private readonly object _lock = new();
    private List<Enemy> enemies = new List<Enemy>();
    private List<(float, float)> enemiesSpawnList = new List<(float, float)>();
    private (float,float) targetPosition = (0f, -2.9f);
    private readonly Func<bool> _hasPlayerCount;
    private readonly SharedHpManager _sharedHpManager;
    private  EnemySyncScheduler _enemySyncScheduler;
    private  CountDownScheduler _countDownScheduler;

    public WaveScheduler(IWebSocketBroadcaster broadcaster, CancellationTokenSource cts, Func<bool> hasPlayerCount, SharedHpManager sharedHpManager)
    {
        _broadcaster = broadcaster;
        _cts = cts;
        _hasPlayerCount = hasPlayerCount;
        _sharedHpManager = sharedHpManager;
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
            _enemySyncScheduler = new EnemySyncScheduler(enemies, _broadcaster, _cts.Token, _sharedHpManager);
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
            enemies.Clear();
            enemiesSpawnList.Clear();
            _wave = 0;

            //init enemy sync scheduler
            _enemySyncScheduler.Stop();

            Console.WriteLine("[WaveScheduler] 접속자가 없어서 중지됨");
            _cts.Cancel(); // Cancel the token to stop the scheduler
        }
    }

    public async Task StartAsync()
    {
        Console.WriteLine("[WaveScheduler] 웨이브 스케줄러 시작됨");
        enemiesSpawnList.Add((-41.74f, -2.07f));
        enemiesSpawnList.Add((49.88f, -2.07f));
        //5초후 시작
        await Task.Delay(5000, _cts.Token);

        //countdown 시작
        await _countDownScheduler.StartAsync();

        var deActive_msg = new CountDownMesasge("countdown", -1, string.Empty);
        await _broadcaster.BroadcastAsync(deActive_msg);

        while (!_cts.Token.IsCancellationRequested)
        {
            // 접속자 없으면 웨이브 스탑
            if (!_hasPlayerCount())
            {
                Stop();
                break;
            }

            _wave++;
            Console.WriteLine($"[WaveScheduler] 웨이브 {_wave} 시작");

            int enemyCount = 3 + _wave;

            for (int i = 0; i < enemyCount; i++)
            {
                string enemyId = Guid.NewGuid().ToString();
                string enemyType = GetRandomEnemyType();
                int randomSpawnIndex = _rand.Next(0, 2);
                var spawnPosition = enemiesSpawnList[randomSpawnIndex];
                
                lock(enemies)
                {
                    enemies.Add(new Enemy(
                        enemyId, 
                        enemyType, 
                        spawnPosition.Item1, 
                        spawnPosition.Item2, 
                        targetPosition.Item1, 
                        targetPosition.Item2
                    ));
                }

                var msg = new SpawnEnemyMessage(
                    "spawn_enemy", 
                    enemyId, 
                    _wave, 
                    spawnPosition.Item1, 
                    spawnPosition.Item2
                );

                await _broadcaster.BroadcastAsync(msg);
                await Task.Delay(1000, _cts.Token);
            }

            await Task.Delay(8000, _cts.Token);
        }

        Console.WriteLine("[WaveScheduler] 웨이브 스케줄러 종료됨");
    }

    private string GetRandomEnemyType()
    {
        string[] types = { "Goblin", "Orc", "Skeleton" };
        return types[_rand.Next(types.Length)];
    }
}