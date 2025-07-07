using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models.DataModels;

public class Bullet
{
    public string bulletId;
    public string attackerId;
    public float x;
    public float y;
    public float dirX;
    public float dirY;
    public int damage;
    public bool isActive = true;
    public Player target;
    public BulletData bulletData;

    public Bullet(string bulletId, string attackerId, float x, float y, float dirX, float dirY, int damage, Player target, BulletData bulletData)
    {
        this.bulletId = bulletId;
        this.attackerId = attackerId;
        this.x = x;
        this.y = y;
        this.dirX = dirX;
        this.dirY = dirY;
        this.damage = damage;
        this.target = target;
        this.bulletData = bulletData;
    }

    public void Update(float deltaTime)
    {
        if (!isActive) return;

        x += dirX * bulletData.speed * deltaTime;
        y += dirY * bulletData.speed * deltaTime;

        // 간단한 충돌 판정 (근접 거리 체크)
        float dx = target.x - x;
        float dy = target.y - y;
        float distSqr = dx * dx + dy * dy;

        if (distSqr < bulletData.range * bulletData.range) // 충돌 거리 허용치
        {
            target.TakeDamage(damage);
            isActive = false;
            Console.WriteLine($"[Bullet] {bulletId} → 플레이어 {target.id} 피격");
        }
    }
}