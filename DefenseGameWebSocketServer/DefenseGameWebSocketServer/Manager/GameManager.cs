using DefenseGameWebSocketServer.Handlers;
using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DefenseGameWebSocketServer.Manager
{
    public class GameManager
    {
        private SharedHpManager _sharedHpManager;
        private WaveScheduler _waveScheduler;
        private PlayerManager _playerManager;
        private EnemyManager _enemyManager;
        private WebSocketBroadcaster _broadcaster;
        private PartyMemberManager _partyMemberManager;
        private CancellationTokenSource _cts;
        private Func<bool> _hasPlayerCount;
        private Func<int> _getPlayerCount;
        private Func<List<string>> _getPlayerList;
        private bool _isGameLoopRunning = false;
        private Task _gameLoopTask;
        
        // 직업 관리를 위한 필드 추가
        private readonly List<string> _availableJobs = new List<string> 
        { 
            "tank", "programmer"
        };
        private readonly HashSet<string> _assignedJobs = new HashSet<string>();
        private readonly object _jobLock = new object();

        public GameManager(WebSocketBroadcaster broadcaster)
        {
            _sharedHpManager = new SharedHpManager();
            _playerManager = new PlayerManager();
            _partyMemberManager = new PartyMemberManager(_playerManager, broadcaster);
            _cts = new CancellationTokenSource();
            _hasPlayerCount = () => _playerManager._playersDict.Count > 0;
            _getPlayerCount = () => _playerManager._playersDict.Count;
            _getPlayerList = () => _playerManager.GetAllPlayerIds().ToList();
            _broadcaster = broadcaster;
            _enemyManager = new EnemyManager((IWebSocketBroadcaster)broadcaster,_sharedHpManager);
            _waveScheduler = new WaveScheduler((IWebSocketBroadcaster)broadcaster, _cts, _hasPlayerCount,_getPlayerCount, _getPlayerList, _sharedHpManager, _enemyManager);
        }

        public void SetPlayerData(string playerId, string job_type)
        {
            _playerManager.AddOrUpdatePlayer(new Player(playerId,0,0,job_type));
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
            if (_waveScheduler == null) _waveScheduler = new WaveScheduler(_broadcaster, _cts, _hasPlayerCount,_getPlayerCount,_getPlayerList, _sharedHpManager, _enemyManager);

            string assignedJob = AssignJobToPlayer();
            SetPlayerData(playerId,assignedJob);
            if(_playerManager.TryGetPlayer(playerId, out Player player))
            {
                await _broadcaster.BroadcastAsync(new
                {
                    type = "player_join",
                    playerInfo = new PlayerInfo
                    {
                        id = playerId,
                        job_type = assignedJob,
                        currentHp = player.currentHp,
                        currentUlt = player.currentUlt,
                        currentMaxHp = player.playerBaseData.hp + player.addData.addHp,
                        currentUltGauge = player.playerBaseData.ult_gauge + player.addData.addUlt,
                        currentMoveSpeed = player.currentMoveSpeed,
                        currentCriPct = player.playerBaseData.critical_pct + player.addData.addCriPct,
                        currentCriDmg = player.playerBaseData.critical_dmg + player.addData.addCriDmg,
                        currentAttack = player.playerBaseData.attack_power + player.addData.addAttackPower,
                        playerBaseData = player.playerBaseData,
                    }
                });
            }

            // 파티 정보 브로드캐스트 (새 플레이어 참여)
            await _partyMemberManager.OnPlayerJoined(playerId);

            if (_broadcaster.ConnectedCount >= 1)
            {
                TryStartWave();
            }

            await _broadcaster.SendToAsync(playerId, new
            {
                type = "player_list",
                players = _playerManager.GetAllPlayers().Select(p => new PlayerInfo
                { 
                    id = p.id,              
                    job_type = p.jobType,
                })
            });

            // 새 플레이어에게 파티 정보 전송
            await _partyMemberManager.SendPartyInfoToPlayer(playerId);
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
            _waveScheduler = new WaveScheduler(_broadcaster, _cts, _hasPlayerCount,_getPlayerCount, _getPlayerList,  _sharedHpManager, _enemyManager);

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

            // 파티에서 플레이어 제거
            await _partyMemberManager.OnPlayerLeft(playerId);

            _playerManager.RemovePlayer(playerId);
            await _broadcaster.BroadcastAsync(new { type = "player_leave", playerId = playerId });
        }

        // 플레이어 체력 변화 시 호출할 수 있는 메서드들 추가
        public async Task OnPlayerDamaged(string playerId)
        {
            await _partyMemberManager.OnPlayerDamaged(playerId);
        }

        public async Task OnPlayerHealed(string playerId)
        {
            await _partyMemberManager.OnPlayerHealed(playerId);
        }

        public async Task OnPlayerUltGaugeChanged(string playerId)
        {
            await _partyMemberManager.OnPlayerUltGaugeChanged(playerId);
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
                        
                        // 공격 후 궁극기 게이지 변화 가능성 있으므로 파티 정보 업데이트
                        await OnPlayerUltGaugeChanged(playerId);
                    }
                    break;
                case MessageType.EnemyAttackHit:
                    {
                        if (!_isGameLoopRunning) return;
                        var enemyAttackHitHandler = new EnemyAttackHitHandler();
                        await enemyAttackHitHandler.HandleAsync(rawMessage, _broadcaster, _sharedHpManager, _enemyManager);
                        
                        // 적의 공격으로 플레이어가 데미지를 받았을 수 있으므로
                        foreach (var pid in _playerManager.GetAllPlayerIds())
                        {
                            await OnPlayerDamaged(pid);
                        }
                    }
                    break;
                case MessageType.SettlementReady:
                    {
                        var settlementReadyHandler = new SettlementReadyHandler();
                        await settlementReadyHandler.HandleAsync(playerId, rawMessage, _broadcaster, _waveScheduler, _playerManager);
                    }
                    break;
            }
        }
    }
}