using System.Collections.Concurrent;

namespace DefenseGameWebSocketServer.Manager
{
    public class SharedHpManager
    {
        private int maxHp = 100;
        private int damageAmount = 10; // 한 번에 받는 데미지
        private SharedHp sharedHp;

        public SharedHpManager()
        {
            sharedHp = new SharedHp(maxHp);
        }
        public void TakeDamage()
        {
            if (sharedHp.currentHp > 0)
            {
                sharedHp.Update(damageAmount);
            }
            if(sharedHp.currentHp <= 0)
            {
                Console.WriteLine("[서버] 크리스탈 파괴됨");
            }
        }
        public (int,int) getHpStatus()
        {
            return (sharedHp.currentHp, sharedHp.maxHp);
        }
    }
}
