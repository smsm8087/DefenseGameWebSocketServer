using System.Collections.Concurrent;

namespace DefenseGameWebSocketServer.Manager
{
    public class SharedHpManager
    {
        private int maxHp = 100;
        private SharedHp sharedHp;

        public SharedHpManager()
        {
            sharedHp = new SharedHp(maxHp);
        }
        public void Reset()
        {
            sharedHp.currentHp = sharedHp.maxHp = maxHp;
        }
        public void TakeDamage(float damageAmount)
        {
            lock (sharedHp)
            {
                if (sharedHp.currentHp > 0)
                {
                    sharedHp.Update(damageAmount);
                }
            }
        }
        public (float, float) getHpStatus()
        {
            return (sharedHp.currentHp, sharedHp.maxHp);
        }
        public bool isGameOver()
        {
            return sharedHp.currentHp <= 0;
        }
    }
}
