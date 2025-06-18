public class Player
{
    public string id;
    public string jobType;
    public float x;
    public float y;
    //나중에 데이터로 빼야할 목록
    public int Hp { get; private set; } = 100;
    public int AttackPower { get; private set; } = 40;

    public Player(string id, float x, float y)
    {
        this.id = id;
        this.x = x;
        this.y = y;
    }

    public void PositionUpdate(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
    public void TakeDamage(int dmg)
    {
        Hp -= dmg;
        if (Hp < 0) Hp = 0;
    }
}