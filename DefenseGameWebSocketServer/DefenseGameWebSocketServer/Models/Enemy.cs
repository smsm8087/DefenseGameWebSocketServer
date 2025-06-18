public class Enemy
{
    public string Id;
    public float X;
    public float Y;
    public float Speed;
    public int Hp = 100;
    public int MaxHp = 100;

    private float targetX;
    private float targetY;

    public Enemy(string id, string type, float startX, float startY, float targetX, float targetY, float speed = 3f)
    {
        Id = id;
        X = startX;
        Y = startY;
        this.targetX = targetX;
        this.targetY = targetY;
        Speed = speed;
        Hp = MaxHp = 100;
    }

    public void Update(float deltaTime)
    {
        float dirX = targetX - X;
        float dirY = targetY - Y;
        float len = MathF.Sqrt(dirX * dirX + dirY * dirY);

        if (len > 0.01f)
        {
            dirX /= len;
            dirY /= len;

            float moveX = dirX * Speed * deltaTime;
            float moveY = dirY * Speed * deltaTime;

            X += moveX;
            Y += moveY;
        }
    }
    public bool CheckArrived(float radius = 2.125f)
    {
        float dx = targetX - X;
        float dy = targetY - Y;
        return dx * dx + dy * dy <= radius * radius;
    }
    public void TakeDamage(int dmg)
    {
        Hp -= dmg;
        if (Hp < 0) Hp = 0;
    }
}