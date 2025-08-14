using DefenseGameWebSocketServer.Handlers;
using DefenseGameWebSocketServer.Managers;
using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models;
using System.Text.Json;

namespace DefenseGameWebSocketServer.Manager
{
    public enum GameResult
    {
        None,
        GameOver,
        Clear
    }


    public class GameManager
    {
        private BulletManager _bulletManager;
        private SharedHpManager _sharedHpManager;
        private WaveScheduler _waveScheduler;
        private PlayerManager _playerManager;
        private EnemyManager _enemyManager;
        private WebSocketBroadcaster _broadcaster;
        private PartyMemberManager _partyMemberManager;
        private RevivalManager _revivalManager;
        private CancellationTokenSource _cts;
        private Func<bool> _hasPlayerCount;
        private Func<int> _getPlayerCount;
        private Func<List<string>> _getPlayerList;
        private bool _isGameLoopRunning = false;
        private Task _gameLoopTask;
        private int waveId = 0; // 현재 웨이브 ID
        private GameResult _gameResult = GameResult.None;

        private Room _room; // 현재 방 정보
        // 직업 관리를 위한 필드 추가
        private readonly List<string> _availableJobs = new List<string> 
        { 
            "tank", "programmer", "sniper", "maid"
        };
        private readonly HashSet<string> _assignedJobs = new HashSet<string>();
        private readonly object _jobLock = new object();
        private readonly NotificationService _notificationService;

        public GameManager(Room room, WebSocketBroadcaster broadcaster, int wave_id)
        {
            this.waveId = wave_id;
            _broadcaster = broadcaster;

            _sharedHpManager = new SharedHpManager(waveId);
            _playerManager = new PlayerManager();
            _partyMemberManager = new PartyMemberManager(_playerManager, broadcaster);
            _revivalManager = new RevivalManager(_playerManager, broadcaster);
            _playerManager.SetRevivalManager(_revivalManager);
            _cts = new CancellationTokenSource();
            _hasPlayerCount = () => _playerManager._playersDict.Count > 0;
            _getPlayerCount = () => _playerManager._playersDict.Count;
            _getPlayerList = () => _playerManager.GetAllPlayerIds().ToList();

            _bulletManager = new BulletManager((IWebSocketBroadcaster)broadcaster, _playerManager);
            _enemyManager = new EnemyManager((IWebSocketBroadcaster)broadcaster, _bulletManager, _playerManager);
            _waveScheduler = new WaveScheduler((IWebSocketBroadcaster)broadcaster, _cts, _hasPlayerCount,_getPlayerCount, _getPlayerList, _sharedHpManager, _enemyManager, _playerManager, async ()=> await GameClear());
            _room = room;
            _notificationService = new NotificationService(_broadcaster);

        }
        
        public Task NotifyPlayer(string playerId, string message)
        {
            return _notificationService.SendNoticeAsync(playerId, message);
        }

        public Task AskPlayerConfirm(string playerId, string question, Action onOk, Action onCancel)
        {
            return _notificationService.SendConfirmAsync(playerId, question, onOk, onCancel);
        }

        public void SetPlayerData(string playerId, string nickName, string job_type)
        {
            _playerManager.AddOrUpdatePlayer(new Player(playerId, nickName , 0,0,job_type));
        }

        public async Task TryConnectGame()
        {
            if (_cts == null) _cts = new CancellationTokenSource();
            if (_sharedHpManager == null) _sharedHpManager = new SharedHpManager(waveId);
            if (_waveScheduler == null) _waveScheduler = new WaveScheduler(_broadcaster, _cts, _hasPlayerCount, _getPlayerCount, _getPlayerList, _sharedHpManager, _enemyManager, _playerManager, async () => await GameClear());

            await _broadcaster.BroadcastAsync(new
            {
                type = "started_game"
            });
        }
        public async Task InitializeGame(List<RoomInfo> roomInfos)
        {
            //씬로딩완료 게임 이니셜라이징
            for (int i = 0; i < roomInfos.Count; i++)
            {
                await SettingGame(roomInfos[i].playerId, roomInfos[i].nickName, roomInfos[i].jobType);
            }

            await _broadcaster.BroadcastAsync(new
            {
                type = "player_list",
                players = _playerManager.GetAllPlayers().Select(p => new PlayerInfo
                {
                    id = p.id,
                    nickname = p.nickname,
                    job_type = p.jobType,
                })
            });
            TryStartWave(this.waveId);
        }
        public async Task SettingGame(string playerId, string nickName, string job_type)
        {
            SetPlayerData(playerId, nickName, job_type);

            if (_playerManager.TryGetPlayer(playerId, out Player player))
            {
                var initialMsg = new
                {
                    type = "initial_game",
                    wave_id = this.waveId
                };
                await _broadcaster.SendToAsync(playerId, initialMsg);

                await _broadcaster.BroadcastAsync(new
                {
                    type = "player_join",
                    playerInfo = new PlayerInfo
                    {
                        id = playerId,
                        nickname = player.nickname,
                        job_type = player.jobType,
                        currentHp = player.currentHp,
                        currentUlt = player.currentUlt,
                        currentMaxHp = player.currentMaxHp,
                        currentUltGauge = player.playerBaseData.ult_gauge + player.addData.addUlt,
                        currentMoveSpeed = player.currentMoveSpeed,
                        currentAttackSpeed = player.currentAttackSpeed,
                        currentCriPct = player.playerBaseData.critical_pct + player.addData.addCriPct,
                        currentCriDmg = player.playerBaseData.critical_dmg + player.addData.addCriDmg,
                        currentAttack = player.playerBaseData.attack_power + player.addData.addAttackPower,
                        playerBaseData = player.playerBaseData,
                    }
                });
            }
            

            // 파티 정보 브로드캐스트 (새 플레이어 참여)
            await _partyMemberManager.OnPlayerJoined(playerId);

            // 새 플레이어에게 파티 정보 전송
            await _partyMemberManager.SendPartyInfoToPlayer(playerId);
        }

        public void TryStartWave(int wave_id)
        {
            if (_isGameLoopRunning) return;
            _isGameLoopRunning = true;

            //웨이브 기본 설정

            _waveScheduler.TryStart(wave_id);
            StartGameLoop();
        }

        private void StartGameLoop()
        {
            _gameLoopTask = Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine("[GameManager] 게임 루프 시작");
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        if (_sharedHpManager.isGameOver())
                        {
                            Console.WriteLine("[GameManager] 공유 HP가 0이 되어 게임 오버");
                            await GameOver();
                            break;
                        }

                        if (_playerManager.AreAllPlayersDead())
                        {
                            Console.WriteLine("[GameManager] 모든 플레이어가 죽어 게임 오버");
                            await GameOver();
                            break;
                        }

                        if (!_hasPlayerCount())
                        {
                            Console.WriteLine("[GameManager] 플레이어가 없어 게임 루프 종료");
                            Dispose();
                            break;
                        }

                        await _revivalManager.UpdateInvulnerabilities();
                        await _revivalManager.CheckAllRevivalDistancesAsync();

                        await Task.Delay(100, _cts.Token);
                    }
                    Console.WriteLine("[GameManager] 게임 루프 종료");
                }
                catch(Exception e)
                {
                    Console.WriteLine($"[GameManager] 게임 루프 중 예외 발생: {e.Message}");
                }
            });
        }

        public async Task GameOver()
        {
            _gameResult = GameResult.GameOver;
            _isGameLoopRunning = false;
            Console.WriteLine("[GameManager] 게임 오버");
            var msg = new GameResultMessage("gameover");
            await _broadcaster.BroadcastAsync(msg);
            Dispose();
        }
        public async Task GameClear()
        {
            _gameResult = GameResult.Clear;
            _isGameLoopRunning = false;
            Console.WriteLine("[GameManager] 게임 클리어!");
            await _broadcaster.BroadcastAsync(new GameResultMessage("clear"));
            Dispose();
        }
        public GameResult GetGameResult() => _gameResult;

        public bool RestartGame()
        {
            Stop();                         
            if(_waveScheduler != null) _waveScheduler?.Dispose();
            if (_cts != null) _cts.Dispose();                  

            _cts = new CancellationTokenSource();
            _waveScheduler = new WaveScheduler(_broadcaster, _cts, _hasPlayerCount,_getPlayerCount, _getPlayerList,  _sharedHpManager, _enemyManager, _playerManager, async () => await GameClear());
            _revivalManager = new RevivalManager(_playerManager, _broadcaster);

            // 게임 재시작 시 직업 할당 초기화
            lock (_jobLock)
            {
                _assignedJobs.Clear();
            }

            _isGameLoopRunning = false;
            TryStartWave(waveId);
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
            _playerManager.Dispose();
            _bulletManager.ClearBullets();
            _revivalManager.ClearAllRevivals();
        }

        public void Stop()
        {
            _isGameLoopRunning = false;
            if(_cts != null) _cts.Cancel();
            if(_waveScheduler != null) _waveScheduler.Stop();
            _playerManager.Dispose();
            _bulletManager.ClearBullets();
            _revivalManager.ClearAllRevivals();
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
            
            if (_playerManager.TryGetPlayer(playerId, out Player player) && player.IsDead)
            {
                await _revivalManager.OnPlayerDeath(playerId);
            }
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
                case MessageType.KickUser:
                    {
                        var kickUserHandler = new KickUserHandler();
                        await kickUserHandler.HandleAsync(playerId, rawMessage, _room, _broadcaster);
                    }
                    break;
                case MessageType.DeSelectCharacter:
                    {
                        var deselectCharacterHandler = new DeSelectCharacterHandler();
                        await deselectCharacterHandler.HandleAsync(playerId, rawMessage, _room, _broadcaster);
                    }
                break;
                case MessageType.SelectCharacter:
                    {
                        var selectCharacterHandler = new SelectCharacterHandler();
                        await selectCharacterHandler.HandleAsync(playerId, rawMessage, _room, _broadcaster);
                    }
                break;
                case MessageType.OutRoom:
                    {
                        var outRoomHandler = new OutRoomHandler();
                        await outRoomHandler.HandleAsync(playerId, rawMessage, _room, _broadcaster);
                    }
                    break;
                case MessageType.GetRoomInfo:
                    {
                        var getRoomInfoHandler = new GetRoomInfoHandler();
                        await getRoomInfoHandler.HandleAsync(playerId, rawMessage, _room, _broadcaster);
                    }
                    break;
                case MessageType.SceneLoaded:
                    {
                        var sceneLoadedHandler = new SceneLoadedHandler(_room , this);
                        await sceneLoadedHandler.HandleAsync(playerId, rawMessage);
                    }
                    break;
                case MessageType.StartGame:
                    {
                        var startGameHandler = new StartGameHandler(_room, this);
                        await startGameHandler.HandleAsync(playerId, rawMessage, _broadcaster);
                    }
                    break;
                case MessageType.CreateRoom:
                {
                    var createRoomHandler = new CreateRoomHandler();
                    await createRoomHandler.HandleAsync(playerId, rawMessage, _broadcaster);
                }
                break;
                case MessageType.JoinRoom:
                {
                    var joinRoomHandler = new JoinRoomHandler();
                    await joinRoomHandler.HandleAsync(playerId, rawMessage, _broadcaster);
                }
                break;
                case MessageType.ChatRoom:
                {
                    var chatRoomHandler = new ChatRoomHandler();
                    await chatRoomHandler.HandleAsync(playerId, rawMessage, _broadcaster);
                }
                break;
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
                        var AttackHandler = new AttackHandler(_enemyManager, _playerManager, _waveScheduler);
                        await AttackHandler.HandleAsync(playerId, rawMessage, _broadcaster);
                        
                        // 공격 후 궁극기 게이지 변화 가능성 있으므로 파티 정보 업데이트
                        await OnPlayerUltGaugeChanged(playerId);
                    }
                    break;
                
                case MessageType.EnemyAttackHit:
                    {
                        if (!_isGameLoopRunning) return;
                        var enemyAttackHitHandler = new EnemyAttackHitHandler(_room, playerId);
                        await enemyAttackHitHandler.HandleAsync(rawMessage, _broadcaster, _sharedHpManager, _enemyManager,_bulletManager, _playerManager, _revivalManager);
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
                
                case MessageType.StartRevival:
                {
                    var startRevivalHandler = new StartRevivalHandler();
                    await startRevivalHandler.HandleAsync(playerId, rawMessage, _broadcaster, _revivalManager);
                }
                    break;
                
                case MessageType.UpdateRevival:
                {
                    var updateRevivalHandler = new UpdateRevivalHandler();
                    await updateRevivalHandler.HandleAsync(playerId, rawMessage, _broadcaster, _revivalManager);
                }
                    break;
                
                case MessageType.CancelRevival:
                {
                    var cancelRevivalHandler = new CancelRevivalHandler();
                    await cancelRevivalHandler.HandleAsync(playerId, rawMessage, _broadcaster, _revivalManager);
                }
                    break;
                case MessageType.ConfirmResponse:
                {
                    var resp = JsonSerializer.Deserialize<ConfirmResponse>(rawMessage);
                    if (ConfirmationManager.TryRemove(resp.requestId, out var ctx))
                    {
                        if (resp.approved) ctx.OnApproved?.Invoke();
                        else               ctx.OnRejected?.Invoke();

                        LogManager.Info(
                            $"[ConfirmResponse] ReqId={resp.requestId}, approved={resp.approved}",
                            roomCode: _room.RoomCode,
                            playerId: playerId
                        );
                    }
                }
                    break;
                case MessageType.UseSkill:
                {
                    var useSkillHandler = new UseSkillHandler(_playerManager, _enemyManager, _broadcaster);
                    await useSkillHandler.HandleAsync(playerId, rawMessage);
                }
                    break;
            }
        }
    }
}