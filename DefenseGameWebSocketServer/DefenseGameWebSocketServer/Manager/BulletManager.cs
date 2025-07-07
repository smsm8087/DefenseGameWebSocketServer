using DefenseGameWebSocketServer.MessageModel;

namespace DefenseGameWebSocketServer.Manager
{
    public class BulletManager
    {
        private static BulletManager _instance;
        public static BulletManager Instance => _instance ??= new BulletManager();

        private IWebSocketBroadcaster _broadcaster;
        private readonly List<Bullet> _bullets = new();

        public void Initialize(IWebSocketBroadcaster broadcaster)
        {
            _broadcaster = broadcaster;
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

                    var playerHpMessage = new PlayerUpdateHpMessage(
                        bullet.target.id,
                        new PlayerInfo
                        {
                            currentHp = bullet.target.currentHp,
                            currentMaxHp = bullet.target.playerBaseData.hp + bullet.target.addData.addHp,
                        }
                    );
                    await _broadcaster.SendToAsync(bullet.target.id, playerHpMessage);

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
            float dy = target.y - attacker.y;
            float mag = MathF.Sqrt(dx * dx + dy * dy);
            if (mag <= 0.01f) return;

            float dirX = dx / mag;
            float dirY = dy / mag;

            var bulletId = Guid.NewGuid().ToString();

            var bullet = new Bullet(
                bulletId: bulletId,
                attackerId: attacker.id,
                x: attacker.x,
                y: attacker.y,
                dirX: dirX,
                dirY: dirY,
                damage: (int)attacker.currentAttack,
                target: target,
                bulletData: attacker.bulletData
            );
            bullet.bulletData = attacker.bulletData;

            // 리스트에 추가
            _bullets.Add(bullet);

            // 클라이언트에 브로드캐스트
            var msg = new BulletSpawnMessage(
                bulletId: bulletId,
                enemyId: attacker.id,
                startX: attacker.x,
                startY: attacker.y
            );

            await _broadcaster.BroadcastAsync(msg);
            Console.WriteLine($"[BulletManager] 총알 생성됨: {bulletId}");
        }
    }
}