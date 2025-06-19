using DefenseGameWebSocketServer.Model;

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

        public async Task InitializeGame(string playerId)
        {
            if (_cts == null) _cts = new CancellationTokenSource();
            if( _sharedHpManager == null) _sharedHpManager = new SharedHpManager();
            if (_waveScheduler == null) _waveScheduler = new WaveScheduler(_broadcaster, _cts, _hasPlayerCount, _sharedHpManager, _enemyManager);

            await _broadcaster.BroadcastAsync(new { type = "player_join", playerId = playerId });

            if (_broadcaster.ConnectedCount >= 1)
            {
                TryStartWave();
            }

            await _broadcaster.SendToAsync(playerId, new
            {
                type = "player_list",
                players = _playerManager.GetAllPlayerIds()
            });
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
            }
        }
    }
}
