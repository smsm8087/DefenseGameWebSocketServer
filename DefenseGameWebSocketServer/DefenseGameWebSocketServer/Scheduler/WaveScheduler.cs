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
    
    //페이즈 나누기
    private int _readyCount = 0;
    private GamePhase _currentPhase;
    private Dictionary<string, List<CardData>> _selectCardPlayerDict = new Dictionary<string, List<CardData>>();
    private WaveData waveData;
    private List<WaveRoundData> waveRoundDataList = new List<WaveRoundData>();

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
    public void TryStart(int wave_id)
    {
        lock (_lock)
        {
            if (_isRunning) return;
            _isRunning = true;

            initWave(wave_id);

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
    public void initWave(int wave_id)
    {
        waveData = GameDataManager.Instance.GetData<WaveData>("wave_data", wave_id);
        if(waveData == null)
        {
            Console.WriteLine($"[WaveScheduler] 웨이브 데이터가 없습니다. 웨이브 ID: {wave_id}");
            return;
        }
        
        var waveRoundData = GameDataManager.Instance.GetTable<WaveRoundData>("wave_round_data");
        foreach (var roundData in waveRoundData.Values)
        {
            if (roundData.wave_id == wave_id)
            {
                waveRoundDataList.Add(roundData);
            }
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

        while (!_cts.Token.IsCancellationRequested && _wave <= waveData.max_wave)
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
                        await _enemyManager.SpawnEnemy(_wave, waveData,waveRoundDataList,_sharedHpManager);
                        //웨이브 2부터는 카드선택 페이즈
                        if (_wave % waveData.settlement_phase_round == 0)
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

        _selectCardPlayerDict.Clear();
        foreach (var playerId in playerList)
        {
            List<CardData> cards = CardTableManager.Instance.DrawCards(3);
            var msg = new SettlementStartMessage(playerId, (int)settlementSeconds, cards);
            _selectCardPlayerDict[playerId] = cards; // 플레이어별 카드 저장

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

        await givePlayerRandomCard(playerList);

        //if (_wave >= waveData.max_wave)
        if (_wave >= 2)
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
    }

    private async Task StartBossPhaseAsync()
    {
        await _broadcaster.BroadcastAsync(new { type = "boss_start", x = -35, y = -2.9 });

        // Boss Phase 진행 (예시로 10초)
        await Task.Delay(10000);
    }

    public void PlayerReady(string playerId)
    {
        if(!_hasPlayerCount() || !_getPlayerList().Contains(playerId) || !_selectCardPlayerDict.ContainsKey(playerId))
        {
            Console.WriteLine($"[WaveScheduler] PlayerReady: {playerId}는 유효한 플레이어가 아닙니다.");
            return;
        }
        _selectCardPlayerDict.Remove(playerId);
        _readyCount++;
        Console.WriteLine($"[WaveScheduler] PlayerReady {_readyCount}/{_getPlayerCount()}");
    }
    private async Task givePlayerRandomCard(List<string> playerList)
    {
        //선택 안한 플레이어의 카드 랜덤 지급
        foreach (var playerId in playerList)
        {
            if (!_selectCardPlayerDict.ContainsKey(playerId)) continue; // 이미 선택한 플레이어는 건너뜀
            var cards = _selectCardPlayerDict[playerId];
            int randomIndex = new Random().Next(cards.Count);

            var selectedCard = cards[randomIndex];
            if (PlayerManager.Instance.TryGetPlayer(playerId, out Player player))
            {
                player.addCardId(selectedCard.id); // 플레이어에게 카드 추가
                Console.WriteLine($"[WaveScheduler] {playerId}에게 랜덤 카드 지급: {selectedCard.id}");
                var response = new UpdatePlayerDataMessage(new PlayerInfo
                {
                    id = playerId,
                    job_type = player.jobType,
                    currentMaxHp = player.playerBaseData.hp + player.addData.addHp,
                    currentUltGauge = player.playerBaseData.ult_gauge + player.addData.addUlt,
                    currentMoveSpeed = player.currentMoveSpeed,
                    currentAttackSpeed = player.currentAttackSpeed,
                    currentCriPct = player.playerBaseData.critical_pct + player.addData.addCriPct,
                    currentCriDmg = player.playerBaseData.critical_dmg + player.addData.addCriDmg,
                    currentAttack = player.playerBaseData.attack_power + player.addData.addAttackPower,
                    cardIds = player.CardIds,
                });
                await _broadcaster.SendToAsync(playerId, response);

                PlayerReady(playerId);
            }
            else
            {
                Console.WriteLine($"[WaveScheduler] {playerId} 플레이어 정보가 없습니다. 카드 지급 실패.");
            }
        }
        var finishMsg = new SettlementTimerUpdateMessage(0, _readyCount);
        await _broadcaster.BroadcastAsync(finishMsg);
    }
    public void Dispose()
    {
        Console.WriteLine("[WaveScheduler] Dispose 호출됨");
        Stop(); 
    }
}