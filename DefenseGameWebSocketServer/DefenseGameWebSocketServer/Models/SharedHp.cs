public class SharedHp
{
    public int currentHp;
    public int maxHp;

    public SharedHp(int maxHp )
    {
        this.currentHp = this.maxHp = maxHp;
    }

    public void Update(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Damage amount cannot be negative.");
        currentHp = Math.Max(0, currentHp - amount);
    }
}