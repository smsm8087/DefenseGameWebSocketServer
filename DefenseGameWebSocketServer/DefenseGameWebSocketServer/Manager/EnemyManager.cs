using DefenseGameWebSocketServer.MessageModel;
using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models;
using DefenseGameWebSocketServer.Models.DataModels;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;


namespace DefenseGameWebSocketServer.Manager
{

    public class EnemyManager
    {
        public List<Enemy> _enemies = new List<Enemy>();
        private List<(float, float)> enemiesSpawnList = new List<(float, float)>();
        private (float, float) targetPosition = (0f, 0f);

        private readonly Random _rand = new();
        private readonly IWebSocketBroadcaster _broadcaster;
        private CancellationTokenSource _cts;
        private bool _isRunning = false;
        private readonly ConcurrentQueue<EnemyBroadcastEvent> _broadcastEvents = new();
        public EnemyManager(IWebSocketBroadcaster broadcaster)
        {
            _broadcaster = broadcaster;
        }
        public void setCancellationTokenSource(CancellationTokenSource cts)
        {
            _cts = cts;
        }
        public void Stop()
        {
            lock (_enemies)
            {
                _enemies.Clear();
                _isRunning = false;
                Console.WriteLine("[EnemyManager] FSM 멈춤");
            }
        }
        public void InitializeEnemySpawnPoints(List<(float,float)> spawnPointList, (float,float) targetPosition)
        {
            enemiesSpawnList = spawnPointList;
            this.targetPosition = targetPosition;
            Console.WriteLine("[EnemyManager] 적 스폰 포인트 초기화됨");
        }
        public async Task StartAsync()
        {
            if (_isRunning) return;
            _isRunning = true;

            Console.WriteLine("[EnemyManager] FSM 시작됨");

            float targetFrameTime = 0.1f; // 100ms (예시)

            var sw = new Stopwatch();
            sw.Start();
            long lastTicks = sw.ElapsedTicks;
            // 적 상태 동기화 리스트
            List<EnemySyncPacket> syncList = new();

            while (!_cts.Token.IsCancellationRequested)
            {
                long nowTicks = sw.ElapsedTicks;
                float deltaTime = (nowTicks - lastTicks) / (float)Stopwatch.Frequency;
                lastTicks = nowTicks;

                lock (_enemies)
                {
                    foreach (var enemy in _enemies)
                    {
                        enemy.UpdateFSM(targetFrameTime);
                        if (enemy.state == EnemyState.Move)
                        {
                            syncList = SyncEnemy();
                        }
                    }
                }
                // Move 상태 sync 패킷
                if (syncList.Count > 0)
                {
                    var msg = new EnemySyncMessage(syncList);
                    await _broadcaster.BroadcastAsync(msg);
                }

                // FSM 이벤트 처리
                while (_broadcastEvents.TryDequeue(out var evt))
                {
                    switch (evt.Type)
                    {
                        case EnemyState.Attack:
                            //prepare animation 재생
                            await _broadcaster.BroadcastAsync(evt.Payload);
                            break;
                        case EnemyState.Dead:
                            await _broadcaster.BroadcastAsync(new EnemyDieMessage(new List<string> { evt.EnemyRef.id }));
                            lock (_enemies)
                            {
                                _enemies.Remove(evt.EnemyRef);
                            }
                            break;
                    }
                }

                // 정확한 프레임 맞추기
                var elapsed = (sw.ElapsedTicks - nowTicks) / (float)Stopwatch.Frequency;
                int sleepMs = Math.Max(0, (int)((targetFrameTime - elapsed) * 1000));
                await Task.Delay(sleepMs, _cts.Token);
            }
            Console.WriteLine("[EnemyManager] FSM 종료됨");
            _isRunning = false;
        }
        
        //플레이어에게 공격을 받았을때.
        public async Task<int> CheckDamaged(PlayerManager _playerManager, PlayerAttackRequest msg)
        {
            var dmgMsg = new EnemyDamagedMessage();

            lock (_enemies) // lock으로 동시성 문제 방지
            {
                // 적 리스트 복사본을 만들어서 안전하게 순회
                var enemiesCopy = new List<Enemy>(_enemies);
        
                foreach (var enemy in enemiesCopy)
                {
                    if (IsEnemyInAttackBox(enemy, msg))
                    {
                        (int, bool) playerAttackData = _playerManager.getPlayerAttackPower(msg.playerId);
                        int playerDamage = playerAttackData.Item1;
                        bool isCritical = playerAttackData.Item2;
                        int enemyDefense = (int)enemy.currentDefense;
                        playerDamage -= enemyDefense; // 적의 방어력 적용
                        enemy.TakeDamage(playerDamage);
        
                        Console.WriteLine($"[AttackHandler] 적 {enemy.id} {playerDamage} 데미지 남은 HP: {enemy.currentHp}");

                        // 데미지 메시지 브로드캐스트
                        dmgMsg.damagedEnemies.Add(new EnemyDamageInfo
                        {
                            enemyId = enemy.id,
                            currentHp = enemy.currentHp,
                            maxHp = enemy.maxHp,
                            damage = playerDamage,
                            isCritical = isCritical
                        });
                    }
                }
            }

            if (dmgMsg.damagedEnemies.Count > 0)
            {
                await _broadcaster.SendToAsync(msg.playerId, dmgMsg);
            }

            return dmgMsg.damagedEnemies.Count;
        }
        //히트박스 검사
        private bool IsEnemyInAttackBox(Enemy enemy, PlayerAttackRequest msg)
        {
            // 몬스터 실제 크기 계산
            float enemyWidth = enemy.enemyBaseData.base_width * enemy.enemyBaseData.base_scale;
            float enemyHeight = enemy.enemyBaseData.base_height * enemy.enemyBaseData.base_scale;

            // 공격박스 범위
            
            float offsetX = enemy.enemyBaseData.base_offsetx * enemy.enemyBaseData.base_scale;
            float offsetY = enemy.enemyBaseData.base_offsety * enemy.enemyBaseData.base_scale;
            float attackLeft = msg.attackBoxCenterX - msg.attackBoxWidth / 2f;
            float attackRight = msg.attackBoxCenterX + msg.attackBoxWidth / 2f;
            float attackBottom = msg.attackBoxCenterY - msg.attackBoxHeight / 2f;
            float attackTop = msg.attackBoxCenterY + msg.attackBoxHeight / 2f;
    
            // 몬스터 박스 범위
            float enemyLeft = (enemy.x + offsetX) - enemyWidth / 2f;
            float enemyRight = (enemy.x + offsetX) + enemyWidth / 2f;
            float enemyBottom = (enemy.y + offsetY) - enemyHeight / 2f;
            float enemyTop = (enemy.y + offsetY) + enemyHeight / 2f;
    
            // AABB 충돌 체크
            return !(attackLeft > enemyRight || 
                     attackRight < enemyLeft || 
                     attackBottom > enemyTop || 
                     attackTop < enemyBottom);
        }

        public async Task SpawnEnemy(int _wave, WaveData waveData, List<WaveRoundData> waveRoundDataList)
        {
            WaveRoundData roundData =  waveRoundDataList.FirstOrDefault(x => x.round_index == _wave);
            if (roundData == null)
            {
                Console.WriteLine($"[EnemyManager] 라운드 {_wave} 데이터가 없습니다.");
                return;
            }
            List<int> enemyIds = roundData.enemy_ids;
            int enemyCount = roundData.enemy_counts;

            for (int i = 0; i < enemyCount; i++)
            {
                string enemyId = Guid.NewGuid().ToString();
                EnemyData enemyData = GetRandomEnemy(enemyIds);
                int randomSpawnIndex = _rand.Next(0, 2);
                var spawnPosition = enemiesSpawnList[randomSpawnIndex];

                lock (_enemies)
                {
                    var enemy = new Enemy(
                        enemyId,
                        enemyData,
                        spawnPosition.Item1,
                        spawnPosition.Item2,
                        targetPosition.Item1,
                        targetPosition.Item2,
                        waveData,
                        roundData
                    );

                    enemy.OnBroadcastRequired = evt => { _broadcastEvents.Enqueue(evt); };
                    _enemies.Add(enemy);

                }
                Console.WriteLine($"[EnemyManager] 적 생성: {enemyId}, 타입: {enemyData.type}, 위치: ({spawnPosition.Item1}, {spawnPosition.Item2})");
                var msg = new SpawnEnemyMessage(
                    enemyId,
                    _wave,
                    spawnPosition.Item1,
                    spawnPosition.Item2,
                    enemyData.id
                );

                await _broadcaster.BroadcastAsync(msg);
                await Task.Delay(1000, _cts.Token);
            }
        }
        public List<EnemySyncPacket> SyncEnemy()
        {
            List<EnemySyncPacket> syncList;

            lock (_enemies)
            {
                syncList = _enemies.Select(e => new EnemySyncPacket(e.id, e.x)).ToList();
            }
            return syncList;
        }
        private EnemyData GetRandomEnemy(List<int> enemyIds)
        {
            int randomIdx = _rand.Next(enemyIds.Count);
            int enemyId = enemyIds[randomIdx];
            EnemyData enemydata = GameDataManager.Instance.GetData<EnemyData>("enemy_data", enemyId);
            return enemydata;
        }
    }
}
