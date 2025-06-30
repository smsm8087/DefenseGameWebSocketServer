using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DefenseGameWebSocketServer.Manager
{
    public class PartyMemberManager
    {
        private readonly PlayerManager _playerManager;
        private readonly WebSocketBroadcaster _broadcaster;

        public PartyMemberManager(PlayerManager playerManager, WebSocketBroadcaster broadcaster)
        {
            _playerManager = playerManager;
            _broadcaster = broadcaster;
        }

        // 플레이어 체력 브로드캐스트
        public async Task BroadcastPlayerHealth(string playerId)
        {
            if (_playerManager.TryGetPlayer(playerId, out Player player))
            {
                int maxHp = player.playerBaseData.hp + player.addData.addHp;
                
                await _broadcaster.BroadcastAsync(new
                {
                    type = "party_member_health",
                    player_id = playerId,
                    current_health = player.currentHp,
                    max_health = maxHp,
                    job_type = player.jobType
                });

                // 플레이어가 죽었다면 상태도 업데이트
                if (player.currentHp <= 0)
                {
                    await UpdatePlayerStatus(playerId, "dead");
                }
            }
        }

        // 플레이어 궁극기 게이지 브로드캐스트
        public async Task BroadcastPlayerUlt(string playerId)
        {
            if (_playerManager.TryGetPlayer(playerId, out Player player))
            {
                await _broadcaster.BroadcastAsync(new
                {
                    type = "party_member_ult",
                    player_id = playerId,
                    current_ult = player.currentUlt,
                    max_ult = 100f,
                    job_type = player.jobType
                });
            }
        }

        // 플레이어 상태 업데이트
        public async Task UpdatePlayerStatus(string playerId, string status)
        {
            await _broadcaster.BroadcastAsync(new
            {
                type = "party_member_status",
                player_id = playerId,
                status = status // "normal", "poisoned", "buffed", "dead" 등
            });
        }

        // 파티 전체 정보 브로드캐스트
        public async Task BroadcastPartyInfo()
        {
            var partyMembers = _playerManager.GetAllPlayers().Select(p => new
            {
                id = p.id,
                job_type = p.jobType,
                current_health = p.currentHp,
                max_health = p.playerBaseData.hp + p.addData.addHp,
                current_ult = p.currentUlt,
                max_ult = 100f
            }).ToList();

            await _broadcaster.BroadcastAsync(new
            {
                type = "party_info",
                members = partyMembers
            });
        }

        // 플레이어가 데미지를 받았을 때 
        public async Task OnPlayerDamaged(string playerId)
        {
            await BroadcastPlayerHealth(playerId);
        }

        // 플레이어가 힐을 받았을 때 
        public async Task OnPlayerHealed(string playerId)
        {
            await BroadcastPlayerHealth(playerId);
        }

        // 궁극기 게이지 변화 시 
        public async Task OnPlayerUltGaugeChanged(string playerId)
        {
            await BroadcastPlayerUlt(playerId);
        }

        // 플레이어 제거 시
        public async Task OnPlayerLeft(string playerId)
        {
            // 플레이어 퇴장 브로드캐스트
            await _broadcaster.BroadcastAsync(new
            {
                type = "party_member_left",
                player_id = playerId
            });
        }

        // 새 플레이어 참여 시
        public async Task OnPlayerJoined(string playerId)
        {
            // 파티 정보 업데이트
            await BroadcastPartyInfo();
        }

        // 파티원 수 반환
        public int GetPartyMemberCount()
        {
            return _playerManager.GetAllPlayerIds().Count();
        }

        // 특정 플레이어에게만 파티 정보 전송 (새 플레이어 접속 시)
        public async Task SendPartyInfoToPlayer(string playerId)
        {
            var partyMembers = _playerManager.GetAllPlayers().Select(p => new
            {
                id = p.id,
                job_type = p.jobType,
                current_health = p.currentHp,
                max_health = p.playerBaseData.hp + p.addData.addHp,
                current_ult = p.currentUlt,
                max_ult = 100f
            }).ToList();

            await _broadcaster.SendToAsync(playerId, new
            {
                type = "party_info",
                members = partyMembers
            });
        }
    }
}