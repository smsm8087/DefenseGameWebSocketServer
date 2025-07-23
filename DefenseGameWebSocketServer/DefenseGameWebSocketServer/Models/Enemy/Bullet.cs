using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models.DataModels;

public class Bullet
{
    public string bulletId;
    public string attackerId;
    public Player hitPlayer; // 플레이어가 피격되었을 때, 해당 플레이어를 저장
    public float x;
    public float y;
    public float dirX;
    public float dirY;
    public int damage;
    public bool isActive = true;
    public BulletData bulletData;
    private const float groundHitY = -2.91f;
    private readonly PlayerManager _playerManager;

    public Bullet(string bulletId, string attackerId, float x, float y, float dirX, float dirY, int damage, BulletData bulletData, PlayerManager playerManager)
    {
        this.bulletId = bulletId;
        this.attackerId = attackerId;
        this.x = x;
        this.y = y;
        this.dirX = dirX;
        this.dirY = dirY;
        this.damage = damage;
        this.bulletData = bulletData;
        _playerManager = playerManager;
    }

    public void Update(float deltaTime)
    {
        if (!isActive) return;

        x += dirX * bulletData.speed * deltaTime;
        y += dirY * bulletData.speed * deltaTime;

        //Ground hit 체크
        if(y < groundHitY)
        {
            isActive = false;
            return;
        }
        //플레이어피격 체크
        foreach (var player in _playerManager.GetAllPlayers())
        {
            if (CheckBulletHitsPlayer(this, player))
            {
                player.TakeDamage(damage);
                hitPlayer = player; // 공격자 ID를 플레이어 ID로 설정
                isActive = false;
                Console.WriteLine($"[Bullet] {bulletId} → 플레이어 {attackerId} 피격");
            }
        }
    }
    //총알 중심좌표가 플레이어 히트박스 사각형 안에 들어있는지 검사
    public static bool CheckBulletHitsPlayer(Bullet bullet, Player player)
    {
        float scale = player.playerBaseData.base_scale;
        var offset = player.playerBaseData.hit_offset;
        var size = player.playerBaseData.hit_size;

        // 스케일 적용된 중심 좌표
        float centerX = player.x + offset[0] * scale;
        float centerY = player.y + offset[1] * scale;

        float halfW = size[0] * scale / 2f;
        float halfH = size[1] * scale / 2f;

        float left = centerX - halfW;
        float right = centerX + halfW;
        float bottom = centerY - halfH;
        float top = centerY + halfH;

        float bx = bullet.x;
        float by = bullet.y;

        return bx > left && bx < right && by > bottom && by < top;
    }
}