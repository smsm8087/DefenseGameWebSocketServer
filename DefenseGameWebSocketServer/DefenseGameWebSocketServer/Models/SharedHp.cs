using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Models.DataModels;
using System.Security.Cryptography.X509Certificates;

public class SharedHp
{
    public float currentHp;
    public float maxHp;
    public List<float> pos;
    public float radius;

    public SharedHp(int shared_id)
    {
        SharedData sharedData = GameDataManager.Instance.GetData<SharedData>("shared_data", shared_id);
        this.currentHp = this.maxHp = sharedData.hp;
        this.pos = sharedData.pos;
        this.radius = sharedData.radius;
    }
    public void Reset()
    {
        currentHp = maxHp;
    }
    public void Update(float amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Damage amount cannot be negative.");
        currentHp = Math.Max(0, currentHp - amount);
    }
}