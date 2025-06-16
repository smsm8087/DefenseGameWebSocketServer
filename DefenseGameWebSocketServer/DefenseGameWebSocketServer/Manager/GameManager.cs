using DefenseGameWebSocketServer.Model;
using System.Collections.Concurrent;
using System.Runtime.InteropServices.JavaScript;

namespace DefenseGameWebSocketServer.Manager
{
    public class GameManager
    {
        private SharedHpManager _sharedHpManager;
        private WaveScheduler _waveScheduler;
        private PlayerManager _playerManager;
        private WebSocketBroadcaster _broadcaster;
        private string _playerId;

        public GameManager(WebSocketBroadcaster broadcaster, CancellationTokenSource cts, Func<bool> hasPlayerCount)
        {
            _sharedHpManager = new SharedHpManager();
            _playerManager = new PlayerManager();
            _waveScheduler = new WaveScheduler((IWebSocketBroadcaster)broadcaster, cts, hasPlayerCount, _sharedHpManager);
            _broadcaster = broadcaster;
        }
        public bool isGameOver()
        {
            return _sharedHpManager.isGameOver();
        }
        public async void InitializeGame(string playerId)
        {
            _playerId = playerId;
            SetPlayerPosition(playerId);
            // 전체 브로드캐스트 (join 알림)
            await _broadcaster.BroadcastAsync(new { type = "player_join", playerId });

            //한명이상 접속했을때 웨이브 시작
            if (_broadcaster.ConnectedCount >= 1)
            {
                _waveScheduler.TryStart();
            }

            // 접속자에게 player_list만 개별 전송
            await _broadcaster.SendToAsync(playerId, new
            {
                type = "player_list",
                players = _broadcaster.GetPlayerIds()
            });
        }
        public async void ProcessHandler(MessageType msgType, string rawMessage)
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
        public async void RemovePlayer(string playerId)
        {
            _playerManager.TryRemove(playerId);
            await _broadcaster.BroadcastAsync(new { type = "player_leave", playerId });
        }
    }
}
