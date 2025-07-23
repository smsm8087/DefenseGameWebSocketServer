using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.MessageModel;
using DefenseGameWebSocketServer.Models.DataModels;

namespace DefenseGameWebSocketServer.Model
{
    public class BossFSM
    {
        private readonly Boss _boss;
        private readonly List<BossPatternData> _patterns;
        private readonly IWebSocketBroadcaster _broadcaster;
        private readonly EnemyManager _enemyManager;
        private readonly PlayerManager _playerManager;
        private readonly SharedHpManager _sharedHpManager;
        private readonly WaveData _waveData;
        private readonly List<WaveRoundData> _waveRoundDataList;
        private int _index = 0;
        private float _stateTimer = 0f;
        private float _syncInterval = 0.2f; // 200ms마다 브로드캐스트
        private float _syncTimer = 0f;
        public BossFSM(
            Boss boss,
            List<BossPatternData> patterns,
            IWebSocketBroadcaster broadcaster,
            EnemyManager enemyManager,
            SharedHpManager sharedHpManager,
            WaveData waveData,
            List<WaveRoundData> waveRoundDataList,
            PlayerManager playerManager
        )
        {
            _boss = boss;
            _patterns = patterns;
            _broadcaster = broadcaster;
            _enemyManager = enemyManager;
            _sharedHpManager = sharedHpManager;
            _waveData = waveData;
            _waveRoundDataList = waveRoundDataList;
            _playerManager = playerManager;
        }

        public void Update(float deltaTime)
        {
            if (_boss.isDead || _patterns.Count == 0) return;

            _syncTimer += deltaTime;
            if (_syncTimer >= _syncInterval)
            {
                _syncTimer = 0f;
                var message = new BossSyncMessage(_boss.x, _boss.y);
                _broadcaster.BroadcastAsync(message);
            }
            _stateTimer -= deltaTime;


            switch (_boss.State)
            {
                case BossState.Idle:
                    if (_stateTimer <= 0f)
                    {
                        _boss.UpdateAggro(_playerManager.GetRandomPlayer());
                        _boss.SetState(BossState.Moving);
                    }
                    break;

                case BossState.Moving:
                    _boss.UpdateMovement(deltaTime);

                    if (BossReachedTarget())
                    {
                        if( _stateTimer <= 0f)
                        {
                            _boss.SetState(BossState.ExecutingPattern);
                        }
                    }
                    break;

                case BossState.ExecutingPattern:
                    var pattern = _patterns[_index];
                    _index = (_index + 1) % _patterns.Count;

                    var executor = PatternFactory.Create(pattern.pattern_name);
                    executor?.Execute(_boss, pattern, _broadcaster, _enemyManager, _sharedHpManager, _waveData, _waveRoundDataList);

                    _boss.SetState(BossState.Idle);
                    _stateTimer = pattern.delay_after_start;
                    break;

                case BossState.Dead:
                    _boss.isDead = true;
                    _broadcaster.BroadcastAsync(new { type = "boss_dead"});
                    break;
            }
        }

        private bool BossReachedTarget()
        {
            float dx = _boss.targetX - _boss.x;
            float dy = _boss.targetY - _boss.y;
            return (dx * dx + dy * dy) < _boss.range * _boss.range;
        }
        private bool IsBossInAttackBox(PlayerAttackRequest msg)
        {
            // 몬스터 실제 크기 계산
            float bossWidth = _boss.bossBaseData.base_width * _boss.bossBaseData.base_scale;
            float bossHeight = _boss.bossBaseData.base_height * _boss.bossBaseData.base_scale;

            // 공격박스 범위

            float offsetX = _boss.bossBaseData.base_offsetx * _boss.bossBaseData.base_scale;
            float offsetY = _boss.bossBaseData.base_offsety * _boss.bossBaseData.base_scale;
            float attackLeft = msg.attackBoxCenterX - msg.attackBoxWidth / 2f;
            float attackRight = msg.attackBoxCenterX + msg.attackBoxWidth / 2f;
            float attackBottom = msg.attackBoxCenterY - msg.attackBoxHeight / 2f;
            float attackTop = msg.attackBoxCenterY + msg.attackBoxHeight / 2f;

            // 몬스터 박스 범위
            float enemyLeft = (_boss.x + offsetX) - bossWidth / 2f;
            float enemyRight = (_boss.x + offsetX) + bossWidth / 2f;
            float enemyBottom = (_boss.y + offsetY) - bossHeight / 2f;
            float enemyTop = (_boss.y + offsetY) + bossHeight / 2f;

            // AABB 충돌 체크
            return !(attackLeft > enemyRight ||
                     attackRight < enemyLeft ||
                     attackBottom > enemyTop ||
                     attackTop < enemyBottom);
        }
        public async Task CheckDamaged(PlayerManager _playerManager, PlayerAttackRequest msg)
        {
            var dmgMsg = new BossDamagedMessage();

            if (IsBossInAttackBox(msg))
            {
                (int, bool) playerAttackData = _playerManager.getPlayerAttackPower(msg.playerId);
                int playerDamage = playerAttackData.Item1;
                bool isCritical = playerAttackData.Item2;
                _boss.TakeDamage(playerDamage);

                // 데미지 메시지 브로드캐스트
                dmgMsg.damagedBoss = new BossDamageInfo
                {
                    playerId = msg.playerId,
                    currentHp = _boss.currentHp,
                    maxHp = _boss.maxHp,
                    damage = playerDamage,
                    isCritical = isCritical
                };
                await _broadcaster.BroadcastAsync(dmgMsg);
            }
        }
    }
}