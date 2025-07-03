using DefenseGameWebSocketServer.Models.DataModels;
using System.Collections.Concurrent;

namespace DefenseGameWebSocketServer.Manager
{
    public class SharedHpManager
    {
        private SharedHp sharedHp;

        public SharedHpManager(int wave_id)
        {
            var wave_table = GameDataManager.Instance.GetData<WaveData>("wave_data", wave_id);
            if (wave_table == null)
            {
                throw new ArgumentException($"WaveData with id {wave_id} not found.");
            }
            if (wave_table.shared_hp_id <= 0)
            {
                throw new ArgumentException($"WaveData with id {wave_id} does not have a valid shared_id.");
            }
            sharedHp = new SharedHp(wave_table.shared_hp_id);
        }
        public void Reset()
        {
            sharedHp.Reset();
        }
        public List<float> GetPosition()
        {
            return sharedHp.pos;
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
