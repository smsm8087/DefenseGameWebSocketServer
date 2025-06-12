using DefenseGameWebSocketServer.Model;

public class WaveScheduler
{
    private readonly SpawnEnemyHandler _spawnEnemyHandler;
    private readonly CancellationToken _token;
    private int _wave = 0;
    private readonly Random _rand = new();
    private bool _isRunning = false;
    private readonly object _lock = new();
    public WaveScheduler(IWebSocketBroadcaster broadcaster, CancellationToken token)
    {
        _spawnEnemyHandler = new SpawnEnemyHandler(broadcaster);
        _token = token;
    }

    public void TryStart()
    {
        lock (_lock)
        {
            if (_isRunning) return;
            _isRunning = true;
            _ = StartAsync(); // fire-and-forget
        }
    }


    public async Task StartAsync()
    {
        Console.WriteLine("[WaveScheduler] 웨이브 스케줄러 시작됨");

        while (!_token.IsCancellationRequested)
        {
            _wave++;
            Console.WriteLine($"[WaveScheduler] 웨이브 {_wave} 시작");

            int enemyCount = 3 + _wave; // 웨이브가 진행될수록 적 수 증가

            for (int i = 0; i < enemyCount; i++)
            {
                string enemyId = Guid.NewGuid().ToString();
                string enemyType = GetRandomEnemyType();

                await _spawnEnemyHandler.SendEnemyAsync(enemyId, enemyType,_wave);
                await Task.Delay(1000, _token); // 적 간격 1초
            }

            await Task.Delay(8000, _token); // 웨이브 간 간격
        }

        Console.WriteLine("[WaveScheduler] 웨이브 스케줄러 종료됨");
    }

    private string GetRandomEnemyType()
    {
        string[] types = { "Goblin", "Orc", "Skeleton" };
        return types[_rand.Next(types.Length)];
    }
}