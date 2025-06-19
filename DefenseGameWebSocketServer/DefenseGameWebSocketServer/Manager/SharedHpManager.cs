using System.Collections.Concurrent;

namespace DefenseGameWebSocketServer.Manager
{
    public class SharedHpManager
    {
        private int maxHp = 100;
        private int damageAmount = 1; // 한 번에 받는 데미지
        private SharedHp sharedHp;

        public SharedHpManager()
        {
            sharedHp = new SharedHp(maxHp);
        }
        public void Reset()
        {
            sharedHp.currentHp = sharedHp.maxHp = maxHp;
        }
        public void TakeDamage()
        {
            lock (sharedHp)
            {
                if (sharedHp.currentHp > 0)
                {
                    sharedHp.Update(damageAmount);
                }
            }
        }
        public (int, int) getHpStatus()
        {
            return (sharedHp.currentHp, sharedHp.maxHp);
        }
        public bool isGameOver()
        {
            return sharedHp.currentHp <= 0;
        }
    }
}
