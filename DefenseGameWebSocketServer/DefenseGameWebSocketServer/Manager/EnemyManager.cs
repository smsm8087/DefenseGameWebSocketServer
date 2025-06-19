using DefenseGameWebSocketServer.Model;

namespace DefenseGameWebSocketServer.Manager
{

    public class EnemyManager
    {
        public List<Enemy> _enemies = new List<Enemy>();
        public List<Enemy> _deadEnemies = new List<Enemy>();
        private List<(float, float)> enemiesSpawnList = new List<(float, float)>();
        private (float, float) targetPosition = (0f, -2.9f);
        private readonly Random _rand = new();
        private readonly IWebSocketBroadcaster _broadcaster;
        private readonly CancellationTokenSource _cts;
        public EnemyManager(IWebSocketBroadcaster broadcaster, CancellationTokenSource cts)
        {
            _broadcaster = broadcaster;
            _cts = cts;
            // 초기 적 스폰 위치 설정
            enemiesSpawnList.Add((-46f, -2f));
            enemiesSpawnList.Add((45f, -2f));
        }
        public async Task SpawnEnemy(int _wave)
        {
            int enemyCount = 3 + _wave;

            for (int i = 0; i < enemyCount; i++)
            {
                string enemyId = Guid.NewGuid().ToString();
                string enemyType = GetRandomEnemyType();
                int randomSpawnIndex = _rand.Next(0, 2);
                var spawnPosition = enemiesSpawnList[randomSpawnIndex];

                lock (_enemies)
                {
                    _enemies.Add(new Enemy(
                        enemyId,
                        enemyType,
                        spawnPosition.Item1,
                        spawnPosition.Item2,
                        targetPosition.Item1,
                        targetPosition.Item2
                    ));
                }
                Console.WriteLine($"[EnemyManager] 적 생성: {enemyId}, 타입: {enemyType}, 위치: ({spawnPosition.Item1}, {spawnPosition.Item2})");
                var msg = new SpawnEnemyMessage(
                    enemyId,
                    _wave,
                    spawnPosition.Item1,
                    spawnPosition.Item2
                );

                await _broadcaster.BroadcastAsync(msg);
                await Task.Delay(1000, _cts.Token);
            }
        }
        public async Task SyncEnemy(float targetFrameTime)
        {
            List<EnemySyncPacket> syncList;

            lock (_enemies)
            {
                foreach (var enemy in _enemies)
                {
                    enemy.Update(targetFrameTime);
                }
                syncList = _enemies.Select(e => new EnemySyncPacket(e.Id, e.X)).ToList();
            }

            var msg = new EnemySyncMessage(syncList);
            await _broadcaster.BroadcastAsync(msg);
        }
        public Enemy GetEnemy(string enemyId)
        {
            return _enemies.FirstOrDefault(e => e.Id == enemyId);
        }

        public void RemoveEnemy(string enemyId)
        {
            _enemies.RemoveAll(e => e.Id == enemyId);
        }
        public void ClearEnemies()
        {
            _enemies.Clear();
            _deadEnemies.Clear();
        }
        private string GetRandomEnemyType()
        {
            string[] types = { "Goblin", "Orc", "Skeleton" };
            return types[_rand.Next(types.Length)];
        }
    }
}
