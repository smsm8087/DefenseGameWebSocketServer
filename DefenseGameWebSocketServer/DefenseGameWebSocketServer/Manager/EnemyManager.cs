using DefenseGameWebSocketServer.MessageModel;
using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models.DataModels;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;


namespace DefenseGameWebSocketServer.Manager
{

    public class EnemyManager
    {
        public List<Enemy> _enemies = new List<Enemy>();
        private readonly Random _rand = new();
        private readonly IWebSocketBroadcaster _broadcaster;
        private CancellationTokenSource _cts;
        private bool _isRunning = false;
        private readonly ConcurrentQueue<EnemyBroadcastEvent> _broadcastEvents = new();
        private readonly BulletManager _bulletManager;
        private readonly PlayerManager _playerManager;
        public EnemyManager(IWebSocketBroadcaster broadcaster, BulletManager bulletManager, PlayerManager playerManager)
        {
            _broadcaster = broadcaster;
            _bulletManager = bulletManager;
            _playerManager = playerManager;
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
                    foreach (var enemy in _enemies.ToList())
                    {
                        if (enemy.state == EnemyState.Dead)
                            continue;

                        enemy.UpdateFSM(targetFrameTime, _playerManager);
                        if (enemy.state == EnemyState.Move || 
                            enemy.state == EnemyState.Attack || 
                            enemy.state == EnemyState.RangedAttack
                        )
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
                        case EnemyState.Move:
                        case EnemyState.RangedAttack:
                        case EnemyState.Attack:
                            //prepare animation 재생
                            await _broadcaster.BroadcastAsync(evt.Payload);
                            break;
                        case EnemyState.Dead:
                            await _broadcaster.BroadcastAsync(new EnemyDieMessage(new List<string> { evt.EnemyRef.id }, evt.EnemyRef.killedPlayerId));
                            lock (_enemies)
                            {
                                var target = _enemies.FirstOrDefault(e => e.id == evt.EnemyRef.id);
                                if (target != null)
                                {
                                    _enemies.Remove(target);
                                    Console.WriteLine($"[EnemyManager] Enemy {target.id} 제거 완료");
                                }
                                else
                                {
                                    Console.WriteLine($"[EnemyManager] Enemy {evt.EnemyRef.id} 제거 실패");
                                }
                            }
                            break;
                    }
                }
                //bulletManager 업데이트
                await _bulletManager.Update(deltaTime);

                // 정확한 프레임 맞추기
                var elapsed = (sw.ElapsedTicks - nowTicks) / (float)Stopwatch.Frequency;
                int sleepMs = Math.Max(0, (int)((targetFrameTime - elapsed) * 1000));
                await Task.Delay(sleepMs, _cts.Token);
            }
            Console.WriteLine("[EnemyManager] FSM 종료됨");
            _isRunning = false;
        }
        public async Task SpawnEnemy(int waveIndex, WaveData waveData, List<WaveRoundData> waveRoundDataList, SharedHpManager sharedHpManager)
        {
            WaveRoundData roundData = waveRoundDataList.Find(x => x.round_index == waveIndex);
            if (roundData == null)
            {
                Console.WriteLine($"[EnemyManager] 라운드 {waveIndex} 데이터가 없습니다.");
                return;
            }

            if (roundData.enemy_ids.Count != roundData.enemy_counts.Count)
            {
                Console.WriteLine("[EnemyManager] enemy_ids와 enemy_counts 수가 일치하지 않습니다.");
                return;
            }

            for (int i = 0; i < roundData.enemy_ids.Count; i++)
            {
                int enemyDataId = roundData.enemy_ids[i];
                int spawnCount = roundData.enemy_counts[i];
                var enemyData = GetEnemyData(enemyDataId);
                if (enemyData == null) continue;

                for (int j = 0; j < spawnCount; j++)
                {
                    string enemyId = Guid.NewGuid().ToString();
                    float spawnX, spawnY;
                    int randomSide = _rand.Next(0, 2);
                    var spawnPos = randomSide == 0 ? enemyData.spawn_left_pos : enemyData.spawn_right_pos;
                    spawnX = spawnPos[0];
                    spawnY = spawnPos[1];

                    Enemy enemy;
                    if (enemyData.target_type == "player")
                    {
                        var targetPlayer = _playerManager.GetAlivePlayers().FirstOrDefault();
                        if (targetPlayer == null) continue;

                        var bulletData = GameDataManager.Instance.GetData<BulletData>("bullet_data", enemyData.bullet_id);

                        enemy = new Enemy(
                            enemyId,
                            enemyData,
                            spawnX,
                            spawnY,
                            targetPlayer.x,
                            targetPlayer.y,
                            waveData,
                            roundData,
                            bulletData
                        );

                        enemy.SetAggroTarget(targetPlayer);
                    }
                    else
                    {
                        enemy = new Enemy(
                            enemyId,
                            enemyData,
                            spawnX,
                            spawnY,
                            sharedHpManager.GetPosition()[0],
                            sharedHpManager.GetPosition()[1],
                            waveData,
                            roundData
                        );
                    }

                    enemy.OnBroadcastRequired = evt => _broadcastEvents.Enqueue(evt);
                    lock (_enemies) _enemies.Add(enemy);

                    var msg = new SpawnEnemyMessage(enemyId, spawnX, spawnY, enemyDataId);
                    await _broadcaster.BroadcastAsync(msg);
                    await Task.Delay(500, _cts.Token);
                }
            }
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
                        int currentDamage = Math.Max(0, playerDamage - enemyDefense);
                        enemy.TakeDamage(currentDamage, msg.playerId);
        
                        Console.WriteLine($"[AttackHandler] 적 {enemy.id} {playerDamage} 데미지 남은 HP: {enemy.currentHp}");

                        // 데미지 메시지 브로드캐스트
                        dmgMsg.damagedEnemies.Add(new EnemyDamageInfo
                        {
                            enemyId = enemy.id,
                            currentHp = enemy.currentHp,
                            maxHp = enemy.maxHp,
                            damage = currentDamage,
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
        public List<EnemySyncPacket> SyncEnemy()
        {
            lock (_enemies)
            {
                return _enemies.Select(e => new EnemySyncPacket(e.id, e.x, e.y,e.enemyBaseData.target_type)).ToList();
            }
        }
        private EnemyData GetEnemyData (int enemyDataId)
        {
            return GameDataManager.Instance.GetData<EnemyData>("enemy_data", enemyDataId);
        }
        public async void SpawnDustEnemy(float x, float y, int enemyDataId, SharedHpManager sharedHpManager, WaveData waveData, List<WaveRoundData> waveRoundDataList)
        {
            string enemyId = Guid.NewGuid().ToString();
            var dustEnemy = new Enemy(
                enemyId,
                GameDataManager.Instance.GetData<EnemyData>("enemy_data", enemyDataId),
                x,
                y,
                sharedHpManager.GetPosition()[0],
                sharedHpManager.GetPosition()[1],
                waveData,
                waveRoundDataList[0]
            );

            dustEnemy.OnBroadcastRequired = evt => _broadcastEvents.Enqueue(evt);
            lock (_enemies) _enemies.Add(dustEnemy);

            var msg = new SpawnEnemyMessage(enemyId,x, y, enemyDataId);
            Console.WriteLine($"[EnemyManager] 먼지 몬스터 소환 at ({x}, {y})");
            await _broadcaster.BroadcastAsync(msg);
        }
        public List<Enemy> GetEnemiesSnapshot()
        {
            lock (_enemies) { return _enemies.ToList(); }
        }
    }
}
