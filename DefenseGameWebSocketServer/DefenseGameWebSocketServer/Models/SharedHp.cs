public class SharedHp
{
    public float currentHp;
    public float maxHp;

    public SharedHp(float maxHp )
    {
        this.currentHp = this.maxHp = maxHp;
    }

    public void Update(float amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Damage amount cannot be negative.");
        currentHp = Math.Max(0, currentHp - amount);
    }
}