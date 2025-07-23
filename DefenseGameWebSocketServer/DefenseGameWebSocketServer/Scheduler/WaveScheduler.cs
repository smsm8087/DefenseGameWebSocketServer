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
    private readonly PlayerManager _playerManager;
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
    private Boss _boss;
    private BossFSM _bossFSM;
    private readonly Action _onGameClear;

    public WaveScheduler(IWebSocketBroadcaster broadcaster, CancellationTokenSource cts, Func<bool> hasPlayerCount, Func<int> getPlayerCount, Func<List<string>> getPlayerList, SharedHpManager sharedHpManager, EnemyManager enemyManager, PlayerManager playerManager,  Action onGameClear)
    {
        _broadcaster = broadcaster;
        _cts = cts;
        _hasPlayerCount = hasPlayerCount;
        _getPlayerCount = getPlayerCount;
        _getPlayerList = getPlayerList;
        _sharedHpManager = sharedHpManager;
        _enemyManager = enemyManager;
        _playerManager = playerManager;
        _onGameClear = onGameClear;
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
                    await StartBossPhaseAsync(waveData, waveRoundDataList);
                    break;
            }
        }
        Console.WriteLine("[WaveScheduler] 웨이브 스케줄러 종료됨");
    }
    private async Task StartSettlementPhaseAsync()
    {
        _readyCount = 0;

        Console.WriteLine("[WaveScheduler] Settlement Phase 시작");

        // 살아있는 플레이어만 필터링
        var playerList = _getPlayerList();
        var alivePlayerList = new List<string>();
   
        foreach (var playerId in playerList)
        {
            if (_playerManager.TryGetPlayer(playerId, out Player player) && !player.IsDead)
            {
                alivePlayerList.Add(playerId);
            }
        }
   
        if (alivePlayerList.Count == 0)
        {
            Console.WriteLine("[WaveScheduler] 살아있는 플레이어가 없습니다. Settlement Phase를 건너뜁니다.");
            _currentPhase = GamePhase.Wave;
            return;
        }

        float settlementSeconds = 60f;

        _selectCardPlayerDict.Clear();
   
        // 살아있는 플레이어에게만 카드 선택 메시지 전송
        foreach (var playerId in alivePlayerList)
        {
            List<CardData> cards = CardTableManager.Instance.DrawCards(3);
            var msg = new SettlementStartMessage(playerId, (int)settlementSeconds, cards, alivePlayerList.Count);
            _selectCardPlayerDict[playerId] = cards;
            await _broadcaster.SendToAsync(playerId, msg);
        }
   
        var settlementDuration = TimeSpan.FromSeconds(settlementSeconds);
        var settlementTask = Task.Delay(settlementDuration, _cts.Token);

        //타이머 시작
        _ = BroadcastSettlementTimer(settlementSeconds, alivePlayerList);

        // 살아있는 플레이어 수를 기준으로 대기
        while (_readyCount < alivePlayerList.Count && !settlementTask.IsCompleted)
        {
            await Task.Delay(100);
        }

        await givePlayerRandomCard(alivePlayerList);

        if (_wave == waveData.boss_wave)
        {
            Console.WriteLine("[WaveScheduler] Settlement Phase 완료 → Boss Phase 진입");
            _currentPhase = GamePhase.Boss;
        } 
        else
        {
            _currentPhase = GamePhase.Wave;
        }
    }
    private async Task BroadcastSettlementTimer(float duration, List<string> alivePlayerList)
    {
        float remaining = duration;

        while (remaining > 0 && !_cts.Token.IsCancellationRequested)
        {
            bool isReady = _readyCount >= alivePlayerList.Count;
            if (isReady) break;
        
            var msg = new SettlementTimerUpdateMessage(remaining, _readyCount);

            // 살아있는 플레이어에게만 전송
            foreach (var playerId in alivePlayerList)
            {
                await _broadcaster.SendToAsync(playerId, msg);
            }
        
            await Task.Delay(100, _cts.Token);
            remaining -= 0.1f;
        }
    }
    public BossFSM GetBossFSM()
    {
        return _bossFSM;
    }
    private async Task StartBossPhaseAsync(WaveData waveData, List<WaveRoundData> waveRoundDataList)
    {
        await Task.Delay(2000);
        int boss_table_id = waveData.boss_table_id;
        var bossData = GameDataManager.Instance.GetData<BossData>("boss_data", boss_table_id);
        if (bossData == null)
        {
            Console.WriteLine($"[WaveScheduler] 보스 데이터가 없습니다. 보스 ID: {boss_table_id}");
            return;
        }
        Console.WriteLine($"[WaveScheduler] Boss Phase 시작: {bossData.id}");
        // 보스 페이즈 시작
        List<float> bossPosition = bossData.spawn_pos;
        await _broadcaster.BroadcastAsync(new { type = "boss_start", x = bossPosition[0], y = bossPosition[1], maxHp = bossData.max_hp });

        await Task.Delay(5000);
        await StartBossPhase(bossData, bossPosition[0], bossPosition[1], waveData, waveRoundDataList); // 보스 페이즈 시작
    }

    

    public async Task StartBossPhase(BossData bossData, float x, float y, WaveData waveData, List<WaveRoundData> waveRoundDataList)
    {
        var patternData = GameDataManager.Instance.GetTable<BossPatternData>("boss_pattern_data")
            .Values.Where(p => p.boss_id == bossData.id).ToList();

        _boss = new Boss
        {
            bossId = bossData.id,
            maxHp = bossData.max_hp,
            currentHp = bossData.max_hp,
            aggro_cool_down = bossData.aggro_cool_down,
            range = bossData.range,
            speed = bossData.speed,
            bossBaseData = bossData,
            x = x,
            y = y,
        };

        _bossFSM = new BossFSM(_boss, patternData, _broadcaster, _enemyManager, _sharedHpManager, waveData, waveRoundDataList, _playerManager);
        float deltaTime = 0.1f; // Update 주기 (100ms)
        while(_boss != null && !_boss.isDead)
        {
            UpdateBoss(deltaTime);
            await Task.Delay(TimeSpan.FromSeconds(deltaTime), _cts.Token);
        }
        Console.WriteLine("[WaveScheduler] 보스 처치 완료 → 게임 클리어 콜백 호출");
        _onGameClear?.Invoke();
    }

    public void UpdateBoss(float deltaTime)
    {
        _bossFSM?.Update(deltaTime);
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
    private async Task givePlayerRandomCard(List<string> alivePlayerList)
    {
        //선택 안한 플레이어의 카드 랜덤 지급
        foreach (var playerId in alivePlayerList)
        {
            if (!_selectCardPlayerDict.ContainsKey(playerId)) continue; // 이미 선택한 플레이어는 건너뜀
            var cards = _selectCardPlayerDict[playerId];
            int randomIndex = new Random().Next(cards.Count);

            var selectedCard = cards[randomIndex];
            if (_playerManager.TryGetPlayer(playerId, out Player player))
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
    
        // 살아있는 플레이어에게만 전송
        foreach (var playerId in alivePlayerList)
        {
            await _broadcaster.SendToAsync(playerId, finishMsg);
        }
    }
    public void Dispose()
    {
        Console.WriteLine("[WaveScheduler] Dispose 호출됨");
        Stop(); 
    }
}