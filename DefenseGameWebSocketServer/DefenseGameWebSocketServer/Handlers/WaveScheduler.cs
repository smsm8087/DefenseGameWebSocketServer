public class WaveScheduler
{
    private readonly HandlerFactory _handlerFactory;
    private readonly IWebSocketBroadcaster _broadcaster;
    private readonly CancellationToken _token;
    private int _wave = 0;
    private readonly Random _rand = new();
    private bool _isRunning = false;
    private readonly object _lock = new();

    public WaveScheduler(HandlerFactory handlerFactory, IWebSocketBroadcaster broadcaster, CancellationToken token)
    {
        _handlerFactory = handlerFactory;
        _broadcaster = broadcaster;
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

            int enemyCount = 3 + _wave;

            for (int i = 0; i < enemyCount; i++)
            {
                string enemyId = Guid.NewGuid().ToString();
                string enemyType = GetRandomEnemyType();

                var handler = _handlerFactory.GetHandler("spawn_enemy") as SpawnEnemyHandler;
                if (handler != null)
                {
                    await handler.SendEnemyAsync(enemyId, _wave, _broadcaster);
                }

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