using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Models.DataModels;
using System;

public class PlayerInfo
{
    public string id { get; set; }
    public string job_type { get; set; }
    public int currentHp { get; set; }
    public float currentUlt { get; set; }
    public int currentMaxHp { get; set; }
    public float currentUltGauge { get; set; }
    public float currentMoveSpeed { get; set; }
    public int currentCriPct { get; set; }
    public int currentCriDmg { get; set; }
    public float currentAttack { get; set; }
    public List<int> cardIds { get; set; } = new List<int>();

    public PlayerData playerBaseData { get; set; }
}

public class PlayerAddData
{
    public int addHp { get; set; }
    public int addUlt { get; set; }
    public int addAttackPower { get; set; }
    public int addCriPct { get; set; }
    public int addCriDmg { get; set; }
    public float addMoveSpeed { get; set; }
}
public class Player
{
    public string id;
    public string jobType;
    public float x;
    public float y;
    
    public int currentHp { get; private set; }
    public float currentUlt { get; private set; }
    public float currentMoveSpeed { get; private set; }
    public PlayerAddData addData { get; private set; }
    public PlayerData playerBaseData { get; private set; }
    public List<int> CardIds { get; private set; } = new List<int>();
    public Player(string id, float x, float y, string job_type)
    {
        this.id = id;
        this.x = x;
        this.y = y;
        this.jobType = job_type;

        PlayerData? playerData = GameDataManager.Instance.GetTable<PlayerData>("player_data").Values.FirstOrDefault(x => x.job_type == this.jobType);
        this.playerBaseData = playerData;
        
        this.currentHp = playerBaseData.hp; // 기본 HP 설정
        this.currentMoveSpeed = playerBaseData.move_speed; // 기본 이동 속도 설정
        this.currentUlt = 0; // 기본 ULT 게이지 설정
        this.addData = new PlayerAddData
        {
            addHp = 0,
            addUlt = 0,
            addAttackPower = 0,
            addCriPct = 0,
            addCriDmg = 0,
            addMoveSpeed = 0
        };
    }
    public void addCardId(int cardId)
    {
        if (!CardIds.Contains(cardId))
        {
            CardIds.Add(cardId);
            Console.WriteLine($"[Player] {id} 카드 추가: {cardId}");
            
            //카드 효과 적용
        }
        else
        {
            Console.WriteLine($"[PlayerERR] {id} 이미 카드 {cardId}를 가지고 있습니다.");
            return;
        }
        applyCardToPlayerAddData(cardId);
    }
    void applyCardToPlayerAddData(int cardId)
    {
        var cardTable = GameDataManager.Instance.GetData<CardData>("card_data", cardId);
        if (cardTable != null)
        {
            switch(cardTable.type)
            {
                case "add_attack":
                    addData.addAttackPower += cardTable.value;
                    break;
                case "add_movespeed":
                    addData.addMoveSpeed += cardTable.value * 0.01f;
                    currentMoveSpeed = playerBaseData.move_speed + addData.addMoveSpeed; // 이동 속도 증가
                    break;
                case "add_criticaldmg":
                    addData.addCriDmg += cardTable.value;
                    break;
                case "add_criticalpct":
                    addData.addCriPct += cardTable.value;
                    break;
                case "add_ultgauge":
                    addData.addUlt += cardTable.value;
                    break;
                case "add_hp":
                    addData.addHp += cardTable.value;
                    currentHp = playerBaseData.hp + addData.addHp; // 이동 속도 증가
                    break;
            }
        }
    }
    public void PositionUpdate(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
    public (int , bool) getDamage()
    {
        //최소공격력 최대공격력 적용
        int baseDamage = playerBaseData.attack_power + addData.addAttackPower;
        int minDamage = (int)(baseDamage * 0.5f); // 최소 공격력 50% 적용
        int maxDamage = baseDamage; // 최대 공격력 150% 적용

        baseDamage = Random.Shared.Next(minDamage, maxDamage);

        int critChance = Math.Min(playerBaseData.critical_pct + addData.addCriPct, 100);
        int critDamage = playerBaseData.critical_dmg + addData.addCriDmg;
        // 크리티컬 확률 계산
        if (Random.Shared.Next(0, 100) < critChance)
        {
            return ((int)(baseDamage * (1 + critDamage / 100.0f)), true); // 크리티컬 데미지 적용
        }
        return (baseDamage, false); // 일반 데미지
    }
    public void addUltGauge()
    {
        float addUlt = addData.addUlt + playerBaseData.ult_gauge;
        this.currentUlt += addUlt;
        if (this.currentUlt > 100) this.currentUlt = 100; // 최대 ULT 게이지는 100
    }
}