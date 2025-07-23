using DefenseGameWebSocketServer.MessageModel;

namespace DefenseGameWebSocketServer.Manager
{
    public class BulletManager
    {
        private IWebSocketBroadcaster _broadcaster;
        private readonly List<Bullet> _bullets = new();
        private readonly PlayerManager _playerManager;
        public BulletManager(IWebSocketBroadcaster broadcaster, PlayerManager playerManager)
        {
            _broadcaster = broadcaster;
            _playerManager = playerManager;
        }
       
        public async Task Update(float deltaTime)
        {
            if (_bullets.Count == 0) return;

            List<BulletTickMessage.BulletInfo> activeBullets = new();
            List<BulletTickMessage.BulletInfo> destroyBulletIds = new();
            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                var bullet = _bullets[i];
                if (!bullet.isActive)
                {
                    destroyBulletIds.Add(new BulletTickMessage.BulletInfo {bulletId = bullet.bulletId });

                    if(bullet.hitPlayer != null)
                    {
                        Console.WriteLine($"[BulletManager] {bullet.bulletId} → {bullet.hitPlayer.id} 피격됨");
                        var playerHpMessage = new PlayerUpdateHpMessage(
                            bullet.hitPlayer.id,
                            new PlayerInfo
                            {
                                currentHp = bullet.hitPlayer.currentHp,
                                currentMaxHp = bullet.hitPlayer.currentMaxHp,
                            }
                        );
                        await _broadcaster.SendToAsync(bullet.hitPlayer.id, playerHpMessage);
                    }
                    else
                    {
                        Console.WriteLine($"[BulletManager] {bullet.bulletId} → Ground hit됨");
                    }
                    _bullets.RemoveAt(i);
                    continue;
                }

                bullet.Update(deltaTime);

                if (bullet.isActive)
                {
                    activeBullets.Add(new BulletTickMessage.BulletInfo
                    {
                        bulletId = bullet.bulletId,
                        x = bullet.x,
                        y = bullet.y
                    });
                }
            }

            if (activeBullets.Count > 0)
            {
                var tickMsg = new BulletTickMessage(activeBullets);
                await _broadcaster.BroadcastAsync(tickMsg);
            }
            if(destroyBulletIds.Count > 0)
            {
                var destroyMsg = new BulletDestroyMessage(destroyBulletIds);
                await _broadcaster.BroadcastAsync(destroyMsg);
            }
        }
        public async Task SpawnBullet(Enemy attacker, Player target)
        {
            if (attacker.bulletData == null || target == null)
                return;
            float dx = target.x - attacker.x;
            float bulletSpawnPosX = dx > 0 ? attacker.x + attacker.enemyBaseData.bullet_offset[0] * attacker.enemyBaseData.base_scale : attacker.x - attacker.enemyBaseData.bullet_offset[0] * attacker.enemyBaseData.base_scale;
            float bulletSpawnPosY = attacker.y + attacker.enemyBaseData.bullet_offset[1] * attacker.enemyBaseData.base_scale;

            dx = target.x - bulletSpawnPosX;
            //offset scale 적용
            float dy = target.y + target.playerBaseData.hit_offset[1] * 3f - bulletSpawnPosY;
            float mag = MathF.Sqrt(dx * dx + dy * dy);
            if (mag <= 0.01f) return;

            float dirX = dx / mag;
            float dirY = dy / mag;

            var bulletId = Guid.NewGuid().ToString();

            var bullet = new Bullet(
                bulletId: bulletId,
                attackerId: attacker.id,
                x: bulletSpawnPosX,
                y: bulletSpawnPosY,
                dirX: dirX,
                dirY: dirY,
                damage: (int)attacker.currentAttack,
                bulletData: attacker.bulletData,
                playerManager : _playerManager
            );
            bullet.bulletData = attacker.bulletData;

            // 리스트에 추가
            _bullets.Add(bullet);

            // 클라이언트에 브로드캐스트
            var msg = new BulletSpawnMessage(
                bulletId: bulletId,
                enemyId: attacker.id,
                startX: bulletSpawnPosX,
                startY: bulletSpawnPosY
            );

            await _broadcaster.BroadcastAsync(msg);
            Console.WriteLine($"[BulletManager] 총알 생성됨: {bulletId}");
        }
        public void ClearBullets()
        {
            _bullets.Clear();
            Console.WriteLine("[BulletManager] 모든 총알이 제거되었습니다.");
        }
    }
}