using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Models.DataModels;
using System;

public class Player
{
    public string id;
    public string jobType;
    public float x;
    public float y;
    
    public int Hp { get; private set; }
    public int AttackPower { get; private set; }
    public List<int> CardIds { get; private set; } = new List<int>();
    public Player(string id, float x, float y)
    {
        this.id = id;
        this.x = x;
        this.y = y;
        
        // 기본값 설정 (직업이 할당되기 전)
        this.Hp = 100;
        this.AttackPower = 40;
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

    // 직업이 할당될 때 테이블 데이터로 스탯 업데이트
    public void SetJobType(string jobType)
    {
        this.jobType = jobType;
        LoadStatsFromTable();
    }

    private void LoadStatsFromTable()
    {
        if (string.IsNullOrEmpty(jobType)) return;

        var playerDataTable = GameDataManager.Instance.GetTable<PlayerData>("player_data");
        if (playerDataTable != null)
        {
            // 직업 타입으로 데이터 찾기
            foreach (var kvp in playerDataTable)
            {
                var data = kvp.Value;
                if (data.job_type == jobType)
                {
                    this.Hp = data.hp;
                    this.AttackPower = data.attack_power;
                    Console.WriteLine($"[Player] {id} 직업 {jobType} 스탯 로드: HP={Hp}, 공격력={AttackPower}");
                    return;
                }
            }
        }
        
        Console.WriteLine($"[Player] {jobType} 직업 데이터를 찾을 수 없습니다. 기본값 사용");
    }

    public void PositionUpdate(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
    
    public void TakeDamage(int dmg)
    {
        Hp -= dmg;
        if (Hp < 0) Hp = 0;
    }

    // 최대 HP 복구 (게임 재시작 등에서 사용)
    public void RestoreFullHp()
    {
        LoadStatsFromTable(); // 테이블에서 다시 로드하여 최대 HP 복구
    }
}