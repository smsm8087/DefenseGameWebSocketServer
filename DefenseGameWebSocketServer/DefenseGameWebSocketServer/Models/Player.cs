using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Models.DataModels;
using System;

public class PlayerInfo
{
    public string id { get; set; }
    public string nickname { get; set; }
    public string job_type { get; set; }
    public int currentHp { get; set; }
    public float currentUlt { get; set; }
    public int currentMaxHp { get; set; }
    public float currentUltGauge { get; set; }
    public float currentMoveSpeed { get; set; }
    public float currentAttackSpeed { get; set; }
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
    public float addAttackSpeed { get; set; }
    public int addCriPct { get; set; }
    public int addCriDmg { get; set; }
    public float addMoveSpeed { get; set; }
}
public class Player
{
    public string id;
    public string nickname;
    public string jobType;
    public float x;
    public float y;
    
    public int currentHp { get; private set; }
    public int currentMaxHp { get; private set; }
    public float currentUlt { get; private set; }
    public float currentMoveSpeed { get; private set; }
    public float currentAttackSpeed { get; private set; }
    public PlayerAddData addData { get; private set; }
    public PlayerData playerBaseData { get; private set; }
    public List<int> CardIds { get; private set; } = new List<int>();
    
    public bool IsDead { get; private set; } = false;
    public DateTime? DeathTime { get; private set; } = null;
    public float DeathPositionX { get; private set; } = 0f;
    public float DeathPositionY { get; private set; } = 0f;
    public bool IsBeingRevived { get; set; } = false;
    public string RevivedBy { get; set; } = null;
    public DateTime? ReviveStartTime { get; set; } = null;
    public bool IsInvulnerable { get; set; } = false;
    public DateTime? InvulnerabilityEndTime { get; set; } = null;
    public RevivalManager? RevivalManagerReference { get; set; }
    public Dictionary<int, DateTime> SkillCooldowns { get; private set; } = new();
    private float _damageReductionPct = 0f;
    private DateTime? _damageReductionEnd = null;

    
    public Player(string id, string nickName, float x, float y, string job_type)
    {
        this.id = id;
        this.nickname = nickName;
        this.x = x;
        this.y = y;
        this.jobType = job_type;

        PlayerData? playerData = GameDataManager.Instance.GetTable<PlayerData>("player_data").Values.FirstOrDefault(x => x.job_type == this.jobType);
        this.playerBaseData = playerData;
        
        this.currentHp = currentMaxHp = playerBaseData.hp; // 기본 HP 설정
        this.currentMoveSpeed = playerBaseData.move_speed; // 기본 이동 속도 설정
        this.currentAttackSpeed = playerBaseData.attack_speed / 10.0f; // 기본 공격 속도 설정
        this.currentUlt = 0; // 기본 ULT 게이지 설정
        this.addData = new PlayerAddData
        {
            addHp = 0,
            addUlt = 0,
            addAttackPower = 0,
            addCriPct = 0,
            addCriDmg = 0,
            addMoveSpeed = 0,
            addAttackSpeed = 0
        };
    }
    public void addCardId(int cardId)
    {
        CardIds.Add(cardId);
        Console.WriteLine($"[Player] {id} 카드 추가: {cardId}");
    
        // 카드 효과 적용
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
                    if (cardTable.need_percent == 1)
                        addData.addAttackPower += (int)(playerBaseData.attack_power * (cardTable.value / 100.0f));
                    else
                        addData.addAttackPower += cardTable.value;
                    break;
                    
                case "add_movespeed":
                    if (cardTable.need_percent == 1)
                        addData.addMoveSpeed += playerBaseData.move_speed * (cardTable.value / 100.0f);
                    else
                        addData.addMoveSpeed += cardTable.value;
                    currentMoveSpeed = playerBaseData.move_speed + addData.addMoveSpeed;
                    break;
                    
                case "add_criticaldmg":
                    if (cardTable.need_percent == 1)
                        addData.addCriDmg += (int)(playerBaseData.critical_dmg * (cardTable.value / 100.0f));
                    else
                        addData.addCriDmg += cardTable.value;
                    break;
                    
                case "add_criticalpct":
                    if (cardTable.need_percent == 1)
                        addData.addCriPct += (int)(playerBaseData.critical_pct * (cardTable.value / 100.0f));
                    else
                        addData.addCriPct += cardTable.value;
                    break;
                    
                case "add_ultgauge":
                    if (cardTable.need_percent == 1)
                        addData.addUlt += (int)(playerBaseData.ult_gauge * (cardTable.value / 100.0f));
                    else
                        addData.addUlt += cardTable.value;
                    break;
                    
                case "add_hp":
                    if (cardTable.need_percent == 1)
                        addData.addHp += (int)(playerBaseData.hp * (cardTable.value / 100.0f));
                    else
                        addData.addHp += cardTable.value;
                    currentMaxHp = playerBaseData.hp + addData.addHp;
                    break;
                case "add_attackspeed":
                    float baseSpeed = playerBaseData.attack_speed / 10.0f;
                    if (cardTable.need_percent == 1)
                        addData.addAttackSpeed += baseSpeed * (cardTable.value / 100.0f);
                    else
                        addData.addAttackSpeed += cardTable.value / 10.0f;
                    currentAttackSpeed = baseSpeed + addData.addAttackSpeed;
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
        int minDamage = (int)(baseDamage * 0.8f); // 최소 공격력 80% 적용
        int maxDamage = (int)(baseDamage * 1.2f); // 최대 공격력 150% 적용

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
    private void UpdateDamageReduction()
    {
        if (_damageReductionEnd.HasValue && DateTime.UtcNow >= _damageReductionEnd.Value)
        {
            _damageReductionEnd = null;
            _damageReductionPct = 0f;
        }
    }
    public void TakeDamage(int damage)
    {
        // 피해감소 만료 체크
        UpdateDamageReduction();

        if (IsInvulnerable)
        {
            Console.WriteLine($"[Player] {id} 무적 상태로 데미지 무시: {damage}");
            return;
        }

        // 피해감소 적용
        if (_damageReductionPct > 0f && damage > 0)
        {
            damage = (int)MathF.Max(0, damage * (1f - _damageReductionPct));
        }

        this.currentHp -= damage;
        if (this.currentHp <= 0)
        {
            this.currentHp = 0;
            if (!IsDead) Die();
        }

        if (RevivalManagerReference != null)
        {
            var revivalsToCancel = RevivalManagerReference.GetRevivalTargetsByReviver(id);
            foreach (var targetId in revivalsToCancel)
            {
                _ = RevivalManagerReference.CancelRevival(targetId, "reviver_damaged");
            }
        }
    }
    
    public void Die()
    {
        if (IsDead) return;
        
        IsDead = true;
        DeathTime = DateTime.Now;
        DeathPositionX = x;
        DeathPositionY = y;
        currentHp = 0;
        
        ClearRevivalState();
        
        Console.WriteLine($"[Player] {id} 사망 - 위치: ({DeathPositionX}, {DeathPositionY})");
    }

    public void Revive(bool withFullHp = false)
    {
        if (!IsDead) return;

        IsDead = false;
        DeathTime = null;
        IsBeingRevived = false;
        RevivedBy = null;
        ReviveStartTime = null;
        
        // 부활 위치로 이동
        x = DeathPositionX;
        y = DeathPositionY;
        
        // 체력 회복
        currentHp = withFullHp ? currentMaxHp : Math.Max(1, currentMaxHp / 2);
        
        // 무적 상태 시작
        IsInvulnerable = true;
        InvulnerabilityEndTime = DateTime.Now.AddSeconds(3.0f);
        
        Console.WriteLine($"[Player] {id} 부활 완료 - HP: {currentHp}/{currentMaxHp}");
    }

    public void ClearRevivalState()
    {
        IsBeingRevived = false;
        RevivedBy = null;
        ReviveStartTime = null;
    }

    public void UpdateInvulnerability()
    {
        if (IsInvulnerable && InvulnerabilityEndTime.HasValue && DateTime.Now >= InvulnerabilityEndTime.Value)
        {
            IsInvulnerable = false;
            InvulnerabilityEndTime = null;
            Console.WriteLine($"[Player] {id} 무적 상태 해제");
        }
    }

    public float GetDistanceTo(float targetX, float targetY)
    {
        float dx = x - targetX;
        float dy = y - targetY;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }

    public float GetDistanceToDeathPosition(float targetX, float targetY)
    {
        if (!IsDead) return float.MaxValue;
        
        float dx = DeathPositionX - targetX;
        float dy = DeathPositionY - targetY;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }
    public bool CanUseSkill(int skillId, float cooldown)
    {
        if (!SkillCooldowns.ContainsKey(skillId)) return true;
        return DateTime.UtcNow >= SkillCooldowns[skillId].AddSeconds(cooldown);
    }
    public void SetSkillCooldown(int skillId)
    {
        SkillCooldowns[skillId] = DateTime.UtcNow;
    }
    public void ApplyDamageReduction(float pct, float durationSeconds)
    {
        if (pct <= 0f || durationSeconds <= 0f) return;
        _damageReductionPct = Math.Clamp(pct, 0f, 0.9f);
        _damageReductionEnd = DateTime.UtcNow.AddSeconds(durationSeconds);
    }
}