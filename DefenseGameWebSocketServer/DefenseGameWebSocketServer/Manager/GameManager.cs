using DefenseGameWebSocketServer.Model;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace DefenseGameWebSocketServer.Manager
{
    public class GameManager
    {
        private SharedHpManager _sharedHpManager;
        private WaveScheduler _waveScheduler;
        private PlayerManager _playerManager;
        private WebSocketBroadcaster _broadcaster;
        private string _playerId;
        private readonly CancellationTokenSource _cts;
        public GameManager(WebSocketBroadcaster broadcaster, CancellationTokenSource cts, Func<bool> hasPlayerCount)
        {
            _sharedHpManager = new SharedHpManager();
            _playerManager = new PlayerManager();
            _waveScheduler = new WaveScheduler((IWebSocketBroadcaster)broadcaster, cts, hasPlayerCount, _sharedHpManager);
            _broadcaster = broadcaster;
            this._cts = cts;
        }
        
        public void SetPlayerData(string playerId)
        {
            _playerId = playerId;
            SetPlayerPosition(playerId);
        }
        public async Task InitializeGame()
        {
            // 전체 브로드캐스트 (join 알림)
            await _broadcaster.BroadcastAsync(new { type = "player_join", playerId = _playerId });

            //한명이상 접속했을때 웨이브 시작
            if (_broadcaster.ConnectedCount >= 1)
            {
                _waveScheduler.TryStart();
            }

            // 접속자에게 player_list만 개별 전송
            await _broadcaster.SendToAsync(_playerId, new
            {
                type = "player_list",
                players = _broadcaster.GetPlayerIds()
            });
        }
        public void RestartGame()
        {
            //한명이상 접속했을때 웨이브 시작
            if (_broadcaster.ConnectedCount >= 1)
            {
                _waveScheduler.TryStart();
                StartGame();
            }
        }
        public bool checkGameOver()
        {
            return _sharedHpManager.isGameOver();
        }
        public async Task StartGame()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                if (checkGameOver()) 
                {
                    await GameOver();
                    break;
                }
                await Task.Delay(100, _cts.Token); // 1초 대기 후 다시 확인
            }
        }
        public async Task GameOver()
        {
            //웨이브 리셋
            _waveScheduler.Reset();

            var msg = new GameOverMessage("game_over", "Game Over!!");
            await _broadcaster.BroadcastAsync(msg);
            await Task.Delay(1000);
            RestartGame();
        }
        public async Task ProcessHandler(MessageType msgType, string rawMessage)
        {
            switch (msgType)
            {
                case MessageType.Move:
                    {
                        var moveHandler = new MoveHandler();
                        await moveHandler.HandleAsync(_playerId, rawMessage, _broadcaster, _playerManager);
                    }
                    break;
            }
        }
        public void SetPlayerPosition(string playerId)
        {
            _playerManager.setPlayerPosition(playerId, 0, 0);
        }
        public async Task RemovePlayer(string playerId)
        {
            _playerManager.TryRemove(playerId);
            await _broadcaster.BroadcastAsync(new { type = "player_leave", playerId = playerId });
        }
    }
}
