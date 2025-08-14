using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Models.DataModels;
using DefenseGameWebSocketServer.Model;
using System.Text.Json;
using System.Linq;
using System;
using DefenseGameWebSocketServer.MessageModel;

public class UseSkillMessage : BaseMessage
{
    public int skillId { get; set; }
    public float dirX { get; set; }
    public float dirY { get; set; }
}

public class UseSkillHandler
{
    private readonly PlayerManager _playerManager;
    private readonly EnemyManager _enemyManager;
    private readonly IWebSocketBroadcaster _broadcaster;

    public UseSkillHandler(PlayerManager playerManager, EnemyManager enemyManager, IWebSocketBroadcaster broadcaster)
    {
        _playerManager = playerManager;
        _enemyManager = enemyManager;
        _broadcaster = broadcaster;
    }

    public async Task HandleAsync(string playerId, string rawMessage)
    {
        var msg = JsonSerializer.Deserialize<UseSkillMessage>(rawMessage);
        if (msg == null) return;

        var skillData = GameDataManager.Instance.GetData<SkillData>("skill_data", msg.skillId);
        if (skillData == null) return;

        if (!_playerManager.TryGetPlayer(playerId, out var player)) return;
        if (player.jobType != skillData.job) return;

        // 쿨타임 체크
        if (!player.CanUseSkill(skillData.id, skillData.cooldown)) return;
        player.SetSkillCooldown(skillData.id);

        switch (skillData.skill_type)
        {
            case "TAUNT":
                await HandleTaunt(player, skillData);
                break;

            case "DASH_ATTACK":
                await HandleDashAttack(player, skillData, msg.dirX, msg.dirY);
                break;
        }

        await _broadcaster.BroadcastAsync(new
        {
            type = "skill_used",
            playerId = player.id,
            skillId = skillData.id,
            x = player.x,
            y = player.y,
            dirX = msg.dirX,
            dirY = msg.dirY
        });
    }

    private async Task HandleTaunt(Player player, SkillData skill)
    {
        var enemies = _enemyManager.GetEnemiesSnapshot()
            .Where(e => e.IsAlive && player.GetDistanceTo(e.x, e.y) <= skill.aoe_radius)
            .ToList();

        lock (_enemyManager._enemies)
        {
            foreach (var enemy in enemies)
                enemy.ApplyTaunt(player, skill.taunt_duration);
        }
    }

    private async Task HandleDashAttack(Player player, SkillData skill, float dirX, float dirY)
    {
        // 방향 정규화
        var len = MathF.Sqrt(dirX * dirX + dirY * dirY);
        if (len < 1e-4f) { dirX = 1; dirY = 0; } else { dirX /= len; dirY /= len; }

        // 기본 파라미터
        float distance = MathF.Max(0f, skill.dash_distance);
        float speed    = (skill.dash_speed > 0f) ? skill.dash_speed : 18f; // fallback
        float duration = distance > 0f ? distance / speed : 0.3f;
        float radius   = 1.5f; // 충돌 반경(기존과 동일/원하면 CSV로)

        // 피해감소 시작
        if (skill.damage_reduction > 0f)
            player.ApplyDamageReduction(skill.damage_reduction, duration);

        // 대시 시작점
        float startX = player.x;
        float startY = player.y;

        // 이번 대시 동안 이미 맞은 적은 중복 타격 방지
        var hitEnemiesOnce = new HashSet<string>();

        // 이동 루프
        float elapsed = 0f;
        var tickMs = 50; // 20fps
        while (elapsed < duration)
        {
            float step = MathF.Min((tickMs / 1000f), duration - elapsed);
            float move = speed * step;

            float prevX = player.x;
            float prevY = player.y;

            // 위치 갱신
            player.PositionUpdate(player.x + dirX * move, player.y + dirY * move);

            // 히트 스윕
            var enemies = _enemyManager.GetEnemiesSnapshot();

            // 이번 틱에서 맞은 적들의 데미지 정보를 수집해 클라로 전송
            var dmgMsg = new EnemyDamagedMessage { damagedEnemies = new List<EnemyDamageInfo>() };
            bool anyHitThisTick = false;

            lock (_enemyManager._enemies)
            {
                foreach (var e in enemies)
                {
                    if (!e.IsAlive) continue;
                    if (hitEnemiesOnce.Contains(e.id)) continue; // 이미 맞았던 적은 스킵

                    if (DistancePointToSegment(e.x, e.y, prevX, prevY, player.x, player.y) <= radius)
                    {
                        (int baseDamage, bool isCritical) = player.getDamage();
                        int finalDamage = (int)(baseDamage * skill.damage_multiplier);

                        e.TakeDamage(finalDamage, player.id);
                        e.ApplyKnockback(dirX, dirY, skill.knockback_distance);
                        if (skill.stun_duration > 0f) e.ApplyStun(skill.stun_duration);

                        hitEnemiesOnce.Add(e.id);
                        anyHitThisTick = true;

                        // 데미지 텍스트용 정보 수집
                        dmgMsg.damagedEnemies.Add(new EnemyDamageInfo
                        {
                            enemyId   = e.id,
                            currentHp = e.currentHp,
                            maxHp     = e.maxHp,
                            damage    = finalDamage,
                            isCritical= isCritical
                        });
                    }
                }
            }

            // 데미지 텍스트는 공격자에게 바로 보내고, 넉백 반영을 위해 적 동기화 1회 브로드캐스트
            if (dmgMsg.damagedEnemies.Count > 0)
            {
                // 공격자에게 데미지 표시 전송
                await _broadcaster.SendToAsync(player.id, dmgMsg);

                // 넉백/위치 변화가 바로 보이도록 적 동기화 1회
                var syncList = _enemyManager.SyncEnemy();
                if (syncList.Count > 0)
                    await _broadcaster.BroadcastAsync(new EnemySyncMessage(syncList));
            }

            // 클라에 위치 브로드캐스트
            await _broadcaster.BroadcastAsync(new {
                type = "move",
                playerId = player.id,
                x = player.x,
                y = player.y
            });

            await Task.Delay(tickMs);
            elapsed += step;
        }

        // 종료 시 최종 정렬
        player.PositionUpdate(startX + dirX * distance, startY + dirY * distance);
    }


    // 선분-점 최소거리
    private static float DistancePointToSegment(float px, float py, float x1, float y1, float x2, float y2)
    {
        float vx = x2 - x1, vy = y2 - y1;
        float wx = px - x1, wy = py - y1;
        float c1 = vx * wx + vy * wy;
        if (c1 <= 0) return MathF.Sqrt(wx * wx + wy * wy);
        float c2 = vx * vx + vy * vy;
        if (c2 <= c1) return MathF.Sqrt((px - x2) * (px - y2) + (py - y2) * (py - y2));
        float b = c1 / c2;
        float bx = x1 + b * vx, by = y1 + b * vy;
        float dx = px - bx, dy = py - by;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
}
