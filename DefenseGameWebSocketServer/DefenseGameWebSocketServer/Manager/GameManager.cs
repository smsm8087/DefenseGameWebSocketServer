using DefenseGameWebSocketServer.Handlers;
using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models.DataModels;  // 추가
using System;                                        // 추가
using System.Collections.Generic;                    // 추가
using System.Linq;                                   // 추가
using System.Threading;                              // 추가
using System.Threading.Tasks;                        // 추가

namespace DefenseGameWebSocketServer.Manager
{
    public class GameManager
    {
        private SharedHpManager _sharedHpManager;
        private WaveScheduler _waveScheduler;
        private PlayerManager _playerManager;
        private EnemyManager _enemyManager;
        private WebSocketBroadcaster _broadcaster;
        private CancellationTokenSource _cts;
        private Func<bool> _hasPlayerCount;
        private bool _isGameLoopRunning = false;
        private Task _gameLoopTask;
        
        // 직업 관리를 위한 필드 추가
        private readonly List<string> _availableJobs = new List<string> 
        { 
            "tank", "programmer"
        };
        private readonly HashSet<string> _assignedJobs = new HashSet<string>();
        private readonly object _jobLock = new object();

        public GameManager(WebSocketBroadcaster broadcaster, Func<bool> hasPlayerCount)
        {
            _sharedHpManager = new SharedHpManager();
            _playerManager = new PlayerManager();
            _cts = new CancellationTokenSource();
            _hasPlayerCount = hasPlayerCount;
            _broadcaster = broadcaster;
            _enemyManager = new EnemyManager((IWebSocketBroadcaster)broadcaster,_sharedHpManager);
            _waveScheduler = new WaveScheduler((IWebSocketBroadcaster)broadcaster, _cts, _hasPlayerCount, _sharedHpManager, _enemyManager);
            
        }

        public void SetPlayerData(string playerId)
        {
            _playerManager.AddOrUpdatePlayer(new Player(playerId,0,0));
        }

        private string AssignJobToPlayer()
        {
            lock (_jobLock)
            {
                Random random = new Random();
                
                if (_assignedJobs.Count == 0)
                {
                    int randomIndex = random.Next(_availableJobs.Count);
                    string assignedJob = _availableJobs[randomIndex];
                    _assignedJobs.Add(assignedJob);
                    return assignedJob;
                }
                else
                {
                    var remainingJobs = _availableJobs.Where(job => !_assignedJobs.Contains(job)).ToList();
                    
                    if (remainingJobs.Count == 0)
                    {
                        remainingJobs = _availableJobs.ToList();
                        _assignedJobs.Clear();
                    }
                    
                    int randomIndex = random.Next(remainingJobs.Count);
                    string assignedJob = remainingJobs[randomIndex];
                    _assignedJobs.Add(assignedJob);
                    return assignedJob;
                }
            }
        }

        public async Task InitializeGame(string playerId)
        {
            if (_cts == null) _cts = new CancellationTokenSource();
            if( _sharedHpManager == null) _sharedHpManager = new SharedHpManager();
            if (_waveScheduler == null) _waveScheduler = new WaveScheduler(_broadcaster, _cts, _hasPlayerCount, _sharedHpManager, _enemyManager);

            string assignedJob = AssignJobToPlayer();
    
            if (_playerManager.TryGetPlayer(playerId, out Player player))
            {
                player.SetJobType(assignedJob);
            }
    
            await _broadcaster.BroadcastAsync(new { 
                type = "player_join", 
                playerId = playerId,
                jobType = assignedJob 
            });

            if (_broadcaster.ConnectedCount >= 1)
            {
                TryStartWave();
            }

            await _broadcaster.SendToAsync(playerId, new
            {
                type = "player_list",
                players = _playerManager.GetAllPlayers().Select(p => new { 
                    id = p.id,              
                    job_type = p.jobType 
                })
            });
        }

        private int GetJobId(string jobType)
        {
            return jobType switch
            {
                "tank" => 1,
                "programmer" => 2,
                _ => 1 // 기본값
            };
        }

        public void TryStartWave()
        {
            if (_isGameLoopRunning) return;
            _isGameLoopRunning = true;

            _waveScheduler.TryStart();
            StartGameLoop();
        }

        private void StartGameLoop()
        {
            _gameLoopTask = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    if (_sharedHpManager.isGameOver())
                    {
                        await GameOver();
                        break;
                    }
                    if(!_hasPlayerCount())
                    {
                        Dispose();
                        break;
                    }

                    await Task.Delay(100, _cts.Token);
                }
            });
        }

        public async Task GameOver()
        {
            _isGameLoopRunning = false;

            var msg = new GameOverMessage("Game Over!!");
            await _broadcaster.BroadcastAsync(msg);
            Dispose();
            await Task.Delay(1000);
        }

        public bool RestartGame()
        {
            Stop();                         
            if(_waveScheduler != null) _waveScheduler?.Dispose();
            if (_cts != null) _cts.Dispose();                  

            _cts = new CancellationTokenSource();
            _sharedHpManager = new SharedHpManager();
            _waveScheduler = new WaveScheduler(_broadcaster, _cts, _hasPlayerCount, _sharedHpManager, _enemyManager);

            // 게임 재시작 시 직업 할당 초기화
            lock (_jobLock)
            {
                _assignedJobs.Clear();
            }

            _isGameLoopRunning = false;
            TryStartWave();
            return true;
        }

        public void Dispose()
        {
            Stop();
            _cts.Dispose();
            _waveScheduler.Dispose();

            _cts = null;
            _sharedHpManager = null;
            _waveScheduler = null;
        }

        public void Stop()
        {
            _isGameLoopRunning = false;
            if(_cts != null) _cts.Cancel();
            if(_waveScheduler != null) _waveScheduler.Stop();
        }

        public async Task RemovePlayer(string playerId)
        {
            // 플레이어 제거 시 할당된 직업도 해제
            if (_playerManager.TryGetPlayer(playerId, out Player player))
            {
                lock (_jobLock)
                {
                    if (!string.IsNullOrEmpty(player.jobType))
                    {
                        _assignedJobs.Remove(player.jobType);
                    }
                }
            }

            _playerManager.RemovePlayer(playerId);
            await _broadcaster.BroadcastAsync(new { type = "player_leave", playerId = playerId });
        }

        public async Task ProcessHandler(string playerId, MessageType msgType, string rawMessage)
        {
            switch (msgType)
            {
                case MessageType.Move:
                    {
                        var moveHandler = new MoveHandler();
                        await moveHandler.HandleAsync(rawMessage, _broadcaster, _playerManager);
                    }
                    break;
                case MessageType.Restart:
                    {
                        var restartHandler = new RestartHandler();
                        await restartHandler.HandleAsync(playerId, _broadcaster, RestartGame);
                    }
                    break;
                case MessageType.PlayerAnimation:
                    {
                        var playerAnimationHandler = new PlayerAnimationHandler();
                        await playerAnimationHandler.HandleAsync(playerId, rawMessage, _broadcaster);
                    }
                    break;
                case MessageType.PlayerAttack:
                    {
                        if (!_isGameLoopRunning) return;
                        var AttackHandler = new AttackHandler(_enemyManager, _playerManager);
                        await AttackHandler.HandleAsync(playerId, rawMessage, _broadcaster);
                    }
                    break;
                case MessageType.EnemyAttackHit:
                    {
                        if (!_isGameLoopRunning) return;
                        var enemyAttackHitHandler = new EnemyAttackHitHandler();
                        await enemyAttackHitHandler.HandleAsync(rawMessage, _broadcaster, _sharedHpManager, _enemyManager);
                    }
                    break;
                case MessageType.RequestPlayerData:
                {
                    var playerDataRequestHandler = new PlayerDataRequestHandler();
                    await playerDataRequestHandler.HandleAsync(playerId, rawMessage, _broadcaster, _playerManager);
                }
                    break;
            }
        }
    }
}