using DefenseGameWebSocketServer.Model;

public class WaveScheduler
{
    private readonly IWebSocketBroadcaster _broadcaster;
    private readonly CancellationToken _token;
    private int _wave = 0;
    private readonly Random _rand = new();
    private bool _isRunning = false;
    private readonly object _lock = new();
    private List<Enemy> enemies = new List<Enemy>();
    private List<(float, float)> enemiesSpawnList = new List<(float, float)>();
    private (float,float) targetPosition = (0f, -2.9f);

    public WaveScheduler(IWebSocketBroadcaster broadcaster, CancellationToken token)
    {
        _broadcaster = broadcaster;
        _token = token;
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
            var enemySyncScheduler = new EnemySyncScheduler(enemies, _broadcaster, _token);
            _ = enemySyncScheduler.StartAsync();
        }
    }

    public async Task StartAsync()
    {
        Console.WriteLine("[WaveScheduler] 웨이브 스케줄러 시작됨");
        enemiesSpawnList.Add((-41.74f, -2.07f));
        enemiesSpawnList.Add((49.88f, -2.07f));
        while (!_token.IsCancellationRequested)
        {
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
                await Task.Delay(1000, _token);
            }

            await Task.Delay(8000, _token);
        }

        Console.WriteLine("[WaveScheduler] 웨이브 스케줄러 종료됨");
    }

    private string GetRandomEnemyType()
    {
        string[] types = { "Goblin", "Orc", "Skeleton" };
        return types[_rand.Next(types.Length)];
    }
}