using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.MessageModel;
using DefenseGameWebSocketServer.Models.DataModels;
using DefenseGameWebSocketServer.Model;
using System.Dynamic;
public enum GamePhase
{
    Wave,
    Settlement,
    Boss
}
public class WaveScheduler
{
    private readonly IWebSocketBroadcaster _broadcaster;
    private readonly CancellationTokenSource _cts;
    private readonly object _lock = new();
    private readonly Func<bool> _hasPlayerCount;
    private readonly Func<int> _getPlayerCount;
    private readonly Func<List<string>> _getPlayerList;
    private readonly SharedHpManager _sharedHpManager;
    private EnemyManager _enemyManager;
    private CountDownScheduler _countDownScheduler;

    private int _wave = 0;
    private bool _isRunning = false;
    private int maxWave = 10; // 최대 웨이브 수
    
    //페이즈 나누기
    private int _readyCount = 0;
    private GamePhase _currentPhase;


    public WaveScheduler(IWebSocketBroadcaster broadcaster, CancellationTokenSource cts, Func<bool> hasPlayerCount, Func<int> getPlayerCount, Func<List<string>> getPlayerList, SharedHpManager sharedHpManager, EnemyManager enemyManager)
    {
        _broadcaster = broadcaster;
        _cts = cts;
        _hasPlayerCount = hasPlayerCount;
        _getPlayerCount = getPlayerCount;
        _getPlayerList = getPlayerList;
        _sharedHpManager = sharedHpManager;
        _enemyManager = enemyManager;
    }
    public void TryStart()
    {
        lock (_lock)
        {
            if (_isRunning) return;
            _isRunning = true;

            //count down 스케줄러
            _countDownScheduler = new CountDownScheduler(_broadcaster, _cts, _hasPlayerCount);
            //enemyManager 시작
            _enemyManager.setCancellationTokenSource(_cts);
            _ = _enemyManager.StartAsync();

            //웨이브 스케줄러 시작
            _ = StartAsync();
            // Reset shared HP manager
            _sharedHpManager.Reset();
        }
    }
    public void Stop()
    {
        lock (_lock)
        {
            if (!_isRunning) return;
            _isRunning = false;
            //init wave
            _wave = 0;
            //enemyManager 중지
            _enemyManager.Stop();

            Console.WriteLine("[WaveScheduler] 중지");
        }
    }
    public async Task StartAsync()
    {
        Console.WriteLine("[WaveScheduler] 웨이브 스케줄러 시작됨");
        _currentPhase = GamePhase.Settlement;
        _readyCount = 0;

        //5초후 시작
        await Task.Delay(5000, _cts.Token);

        //countdown 시작
        await _countDownScheduler.StartAsync();

        while (!_cts.Token.IsCancellationRequested && _wave <= maxWave)
        {
            switch(_currentPhase)
            {
                case GamePhase.Wave:
                    {
                        _wave++;
                        Console.WriteLine($"[WaveScheduler] 웨이브 {_wave} 시작");
                        
                        // 웨이브 시작 메시지 전송
                        var waveStartMsg = new WaveStartMessage(_wave);
                        await _broadcaster.BroadcastAsync(waveStartMsg);

                        //적 소환
                        await _enemyManager.SpawnEnemy(_wave);
                        //웨이브 2부터는 카드선택 페이즈
                        if (_wave % 2 == 0)
                        {
                            Console.WriteLine($"[WaveScheduler] Wave {_wave} → Settlement 대기 (적 {_enemyManager._enemies.Count})");

                            while (_enemyManager._enemies.Count > 0)
                            {
                                await Task.Delay(200, _cts.Token);
                            }
                            _currentPhase = GamePhase.Settlement;
                        }
                        else
                        {
                            // 웨이브 사이 시간
                            await Task.Delay(8000, _cts.Token);
                        }
                    }
                    break;
                case GamePhase.Settlement:
                    await StartSettlementPhaseAsync();
                    break;
                case GamePhase.Boss:
                    await StartBossPhaseAsync();
                    break;
            }
        }
        Console.WriteLine("[WaveScheduler] 웨이브 스케줄러 종료됨");
    }
    private async Task StartSettlementPhaseAsync()
    {
        _readyCount = 0;

        Console.WriteLine("[WaveScheduler] Settlement Phase 시작");

        // 플레이어 마다 카드 3장 뽑기
        var playerList = _getPlayerList();
        if (playerList.Count == 0)
        {
            Console.WriteLine("[WaveScheduler] 플레이어가 없습니다. Settlement Phase를 건너뜁니다.");
            _currentPhase = GamePhase.Wave;
            return;
        }

        float settlementSeconds = 60f;

        foreach (var playerId in playerList)
        {
            List<CardData> cards = CardTableManager.Instance.DrawCards(3);
            var msg = new SettlementStartMessage(playerId, 60, cards);

            await _broadcaster.SendToAsync(playerId,msg);
        }
        var settlementDuration = TimeSpan.FromSeconds(settlementSeconds);
        var settlementTask = Task.Delay(settlementDuration, _cts.Token);

        //타이머 시작
        _ = BroadcastSettlementTimer(settlementSeconds);

        while (_readyCount < _getPlayerCount() && !settlementTask.IsCompleted)
        {
            await Task.Delay(100);
        }

        if(_wave >= maxWave)
        {
            Console.WriteLine("[WaveScheduler] Settlement Phase 완료 → Boss Phase 진입");
            _currentPhase = GamePhase.Boss;
        } 
        else
        {
            _currentPhase = GamePhase.Wave;
        }
    }
    private async Task BroadcastSettlementTimer(float duration)
    {
        float remaining = duration;

        while (remaining > 0 && !_cts.Token.IsCancellationRequested)
        {
            bool isReady = _readyCount >= _getPlayerCount();
            if (isReady) break;
            var msg = new SettlementTimerUpdateMessage(remaining, _readyCount);

            await _broadcaster.BroadcastAsync(msg);
            await Task.Delay(100, _cts.Token);
            remaining -= 0.1f;
        }
        var finishMsg = new SettlementTimerUpdateMessage(remaining, _readyCount);
        await _broadcaster.BroadcastAsync(finishMsg);
    }

    private async Task StartBossPhaseAsync()
    {
        await _broadcaster.BroadcastAsync(new { type = "boss_start" });

        // Boss Phase 진행 (예시로 10초)
        await Task.Delay(10000);
    }

    public void PlayerReady()
    {
        _readyCount++;
        Console.WriteLine($"[WaveScheduler] PlayerReady {_readyCount}/{_getPlayerCount()}");
    }
    public void Dispose()
    {
        Console.WriteLine("[WaveScheduler] Dispose 호출됨");
        Stop(); 
    }
}