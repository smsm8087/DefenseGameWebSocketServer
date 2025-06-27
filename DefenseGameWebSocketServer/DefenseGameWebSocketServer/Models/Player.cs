using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Models.DataModels;
using System;

public class PlayerInfo
{
    public string id { get; set; }
    public string job_type { get; set; }
    public int currentHp { get; set; }
    public float currentUlt { get; set; }
    public PlayerData playerBaseData { get; set; }
}

public class PlayerAddData
{
    public int addHp { get; set; }
    public int addUlt { get; set; }
    public int addAttackPower { get; set; }
    public int addCriPct { get; set; }
    public int addCriDmg { get; set; }
    public int addMoveSpeed { get; set; }
}
public class Player
{
    public string id;
    public string jobType;
    public float x;
    public float y;
    
    public int currentHp { get; private set; }
    public float currentUlt { get; private set; }
    public PlayerAddData addData { get; private set; }
    public PlayerData playerBaseData { get; private set; }
    public List<int> CardIds { get; private set; } = new List<int>();
    public Player(string id, float x, float y, string job_type)
    {
        this.id = id;
        this.x = x;
        this.y = y;
        this.currentHp = 100; // 기본 HP 설정
        this.currentUlt = 0; // 기본 ULT 게이지 설정
        this.jobType = job_type;
        this.addData = new PlayerAddData
        {
            addHp = 0,
            addUlt = 0,
            addAttackPower = 0,
            addCriPct = 0,
            addCriDmg = 0,
            addMoveSpeed = 0
        };
        PlayerData? playerData = GameDataManager.Instance.GetTable<PlayerData>("player_data").Values.FirstOrDefault(x => x.job_type == this.jobType);
        this.playerBaseData = playerData;
    }
    public void addCardId(int cardId)
    {
        if (!CardIds.Contains(cardId))
        {
            CardIds.Add(cardId);
            Console.WriteLine($"[Player] {id} 카드 추가: {cardId}");
        }
        else
        {
            Console.WriteLine($"[PlayerERR] {id} 이미 카드 {cardId}를 가지고 있습니다.");
        }
    }

    public void PositionUpdate(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
    public int getDamage()
    {
        int baseDamage = playerBaseData.attack_power + addData.addAttackPower;
        int critChance = addData.addCriPct;
        int critDamage = addData.addCriDmg;
        // 크리티컬 확률 계산
        if (Random.Shared.Next(0, 100) < critChance)
        {
            return (int)(baseDamage * (1 + critDamage / 100.0f)); // 크리티컬 데미지 적용
        }
        return baseDamage; // 일반 데미지
    }
    public void addUltGauge()
    {
        float addUlt = addData.addUlt + playerBaseData.ult_gauge;
        this.currentUlt += addUlt;
        if (this.currentUlt > 100) this.currentUlt = 100; // 최대 ULT 게이지는 100
    }
}