using DefenseGameWebSocketServer.Constants;
using DefenseGameWebSocketServer.MessageModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DefenseGameWebSocketServer.Manager
{
    public class RevivalManager
    {
        private readonly PlayerManager _playerManager;
        private readonly WebSocketBroadcaster _broadcaster;
        private readonly Dictionary<string, RevivalData> _activeRevivals = new();
        private readonly object _revivalLock = new object();

        public RevivalManager(PlayerManager playerManager, WebSocketBroadcaster broadcaster)
        {
            _playerManager = playerManager;
            _broadcaster = broadcaster;
        }

        public class RevivalData
        {
            public string ReviverId { get; set; }
            public string TargetId { get; set; }
            public DateTime StartTime { get; set; }
            public float Progress { get; set; }
            public bool IsActive { get; set; }
        }

        public async Task<bool> StartRevival(string reviverId, string targetId)
        {
            Console.WriteLine($"[서버] RevivalManager.StartRevival 호출");
            Console.WriteLine($"[서버] reviverId: {reviverId}, targetId: {targetId}");
    
            lock (_revivalLock)
            {
                if (!CanStartRevival(reviverId, targetId))
                {
                    Console.WriteLine($"[서버] 부활 조건 실패");
                    return false;
                }

                if (!_playerManager.TryGetPlayer(reviverId, out Player reviver) ||
                    !_playerManager.TryGetPlayer(targetId, out Player target))
                {
                    Console.WriteLine($"[서버] 플레이어를 찾을 수 없음");
                    return false;
                }

                Console.WriteLine($"[서버] 부활 데이터 생성 중...");
        
                // 부활 데이터 생성
                var revivalData = new RevivalData
                {
                    ReviverId = reviverId,
                    TargetId = targetId,
                    StartTime = DateTime.Now,
                    Progress = 0f,
                    IsActive = true
                };

                _activeRevivals[targetId] = revivalData;
                target.IsBeingRevived = true;
                target.RevivedBy = reviverId;
                target.ReviveStartTime = DateTime.Now;
            }

            Console.WriteLine($"[서버] 부활 시작 메시지 브로드캐스트");
    
            // 클라이언트에 부활 시작 알림
            var message = new RevivalStartMessage
            {
                reviverId = reviverId,
                targetId = targetId,
                duration = RevivalConstants.REVIVE_DURATION,
                targetX = _playerManager.TryGetPlayer(targetId, out Player deadPlayer) ? deadPlayer.DeathPositionX : 0,
                targetY = deadPlayer?.DeathPositionY ?? 0
            };

            await _broadcaster.BroadcastAsync(message);
            Console.WriteLine($"[서버] 부활 시작 브로드캐스트 완료");
            return true;
        }

        public async Task<bool> UpdateRevival(string reviverId, string targetId, float progress)
        {
            Console.WriteLine($"[서버] UpdateRevival 호출 - reviverId: {reviverId}, targetId: {targetId}, progress: {progress}");
    
            if (!_playerManager.TryGetPlayer(reviverId, out Player reviver) ||
                !_playerManager.TryGetPlayer(targetId,  out Player target))
                return false;

            float dx = reviver.x - target.DeathPositionX;
            float dy = reviver.y - target.DeathPositionY;
            float distance = (float)Math.Sqrt(dx*dx + dy*dy);
            if (distance > RevivalConstants.REVIVE_INTERACTION_RANGE)
            {
                // 범위 벗어나면 즉시 취소
                await CancelRevival(targetId, "distance");
                return false;
            }
            
            lock (_revivalLock)
            {
                if (!_activeRevivals.TryGetValue(targetId, out RevivalData revivalData))
                {
                    Console.WriteLine($"[서버] activeRevivals에서 {targetId}를 찾을 수 없음");
                    return false;
                }
        
                if (revivalData.ReviverId != reviverId)
                {
                    Console.WriteLine($"[서버] 부활자 불일치: 기대값={revivalData.ReviverId}, 실제값={reviverId}");
                    return false;
                }
        
                if (!revivalData.IsActive)
                {
                    Console.WriteLine($"[서버] 부활이 비활성 상태");
                    return false;
                }

                Console.WriteLine($"[서버] 진행률 업데이트: {revivalData.Progress} -> {progress}");
                revivalData.Progress = Math.Clamp(progress, 0f, 100f);

                // 부활 완료 체크
                if (revivalData.Progress >= 100f)
                {
                    Console.WriteLine($"[서버] 부활 완료 조건 달성!");
                    _ = Task.Run(() => CompleteRevival(targetId));
                    return true;
                }
            }

            // 진행상황 브로드캐스트
            var message = new RevivalProgressMessage
            {
                targetId = targetId,
                progress = progress
            };

            await _broadcaster.BroadcastAsync(message);
            Console.WriteLine($"[서버] 진행률 브로드캐스트 완료: {progress}%");
            return true;
        }
        
        public async Task CheckAllRevivalDistancesAsync()
        {
            var toCancel = new List<string>();
            lock (_revivalLock)
            {
                foreach (var kv in _activeRevivals)
                {
                    var data = kv.Value;
                    if (!_playerManager.TryGetPlayer(data.ReviverId, out Player reviver) ||
                        !_playerManager.TryGetPlayer(data.TargetId,  out Player target))
                        continue;

                    float dx = reviver.x - target.DeathPositionX;
                    float dy = reviver.y - target.DeathPositionY;
                    float distance = (float)Math.Sqrt(dx*dx + dy*dy);

                    if (distance > RevivalConstants.REVIVE_INTERACTION_RANGE)
                        toCancel.Add(data.TargetId);
                }
            }

            // 락 해제 후 취소 호출
            foreach (var targetId in toCancel)
                await CancelRevival(targetId, "distance");
        }

        public async Task CompleteRevival(string targetId)
        {
            RevivalData revivalData;
            lock (_revivalLock)
            {
                if (!_activeRevivals.TryGetValue(targetId, out revivalData))
                {
                    return;
                }

                _activeRevivals.Remove(targetId);
            }

            if (!_playerManager.TryGetPlayer(targetId, out Player target))
            {
                return;
            }

            // 플레이어 부활 처리
            target.Revive(true);

            // 클라이언트에 부활 완료 알림
            var message = new RevivalCompletedMessage
            {
                targetId = targetId,
                reviveX = target.DeathPositionX,
                reviveY = target.DeathPositionY,
                currentHp = target.currentHp,
                maxHp = target.currentMaxHp,
                invulnerabilityDuration = RevivalConstants.INVULNERABILITY_DURATION
            };

            await _broadcaster.BroadcastAsync(message);
            Console.WriteLine($"[RevivalManager] {targetId} 부활 완료");
        }

        public async Task CancelRevival(string targetId, string reason = "interrupted")
        {
            RevivalData revivalData;
            lock (_revivalLock)
            {
                if (!_activeRevivals.TryGetValue(targetId, out revivalData))
                {
                    return;
                }

                _activeRevivals.Remove(targetId);
            }

            if (_playerManager.TryGetPlayer(targetId, out Player target))
            {
                target.ClearRevivalState();
            }

            // 클라이언트에 부활 취소 알림
            var message = new RevivalCancelledMessage
            {
                targetId = targetId,
                reviverId  = revivalData.ReviverId,
                reason = reason
            };

            await _broadcaster.BroadcastAsync(message);
            Console.WriteLine($"[RevivalManager] {targetId} 부활 취소: {reason}");
        }

        public bool CanStartRevival(string reviverId, string targetId)
        {
            Console.WriteLine($"[서버] CanStartRevival 확인 시작");
            Console.WriteLine($"[서버] reviverId: {reviverId}, targetId: {targetId}");
    
            if (!_playerManager.TryGetPlayer(reviverId, out Player reviver))
            {
                Console.WriteLine($"[서버] 부활자를 찾을 수 없음: {reviverId}");
                return false;
            }
    
            if (!_playerManager.TryGetPlayer(targetId, out Player target))
            {
                Console.WriteLine($"[서버] 대상을 찾을 수 없음: {targetId}");
                return false;
            }
    
            Console.WriteLine($"[서버] 부활자({reviverId}) 상태: 죽음={reviver.IsDead}");
            Console.WriteLine($"[서버] 대상({targetId}) 상태: 죽음={target.IsDead}, 부활중={target.IsBeingRevived}");
    
            // 거리 계산 수정 - 직접 계산
            float dx = reviver.x - target.DeathPositionX;
            float dy = reviver.y - target.DeathPositionY;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);
    
            Console.WriteLine($"[서버] 부활자 위치: ({reviver.x}, {reviver.y})");
            Console.WriteLine($"[서버] 대상 사망 위치: ({target.DeathPositionX}, {target.DeathPositionY})");
            Console.WriteLine($"[서버] 올바른 거리: {distance} (최대: {RevivalConstants.REVIVE_INTERACTION_RANGE})");
    
            // 각 조건별로 체크
            bool reviverNotDead = !reviver.IsDead;
            bool targetIsDead = target.IsDead;
            bool targetNotBeingRevived = !target.IsBeingRevived;
            bool withinRange = distance <= RevivalConstants.REVIVE_INTERACTION_RANGE;
    
            Console.WriteLine($"[서버] 조건 체크:");
            Console.WriteLine($"[서버] - 부활자가 살아있음: {reviverNotDead}");
            Console.WriteLine($"[서버] - 대상이 죽었음: {targetIsDead}");
            Console.WriteLine($"[서버] - 대상이 부활 중이 아님: {targetNotBeingRevived}");
            Console.WriteLine($"[서버] - 거리 조건: {withinRange}");
    
            bool canStart = reviverNotDead && targetIsDead && targetNotBeingRevived && withinRange;
            Console.WriteLine($"[서버] CanStartRevival 최종 결과: {canStart}");
    
            return canStart;
        }

        public async Task UpdateInvulnerabilities()
        {
            var playersToUpdate = new List<string>();

            foreach (var player in _playerManager.GetAllPlayers())
            {
                if (player.IsInvulnerable)
                {
                    bool wasInvulnerable = player.IsInvulnerable;
                    player.UpdateInvulnerability();
                    
                    if (wasInvulnerable && !player.IsInvulnerable)
                    {
                        playersToUpdate.Add(player.id);
                    }
                }
            }

            // 무적 해제된 플레이어들에게 알림
            foreach (var playerId in playersToUpdate)
            {
                var message = new InvulnerabilityEndedMessage
                {
                    playerId = playerId
                };
                await _broadcaster.BroadcastAsync(message);
            }
        }

        public async Task OnPlayerDeath(string playerId)
        {
            if (!_playerManager.TryGetPlayer(playerId, out Player player))
            {
                return;
            }

            // 만약 이 플레이어가 다른 플레이어를 부활시키고 있었다면 취소
            var revivalsToCancel = new List<string>();
            lock (_revivalLock)
            {
                foreach (var kvp in _activeRevivals)
                {
                    if (kvp.Value.ReviverId == playerId)
                    {
                        revivalsToCancel.Add(kvp.Key);
                    }
                }
            }

            foreach (var targetId in revivalsToCancel)
            {
                await CancelRevival(targetId, "reviver_died");
            }

            // 클라이언트에 죽음 알림
            var message = new PlayerDeadMessage
            {
                playerId = playerId,
                deathX = player.DeathPositionX,
                deathY = player.DeathPositionY
            };

            await _broadcaster.BroadcastAsync(message);
        }

        public void ClearAllRevivals()
        {
            lock (_revivalLock)
            {
                _activeRevivals.Clear();
            }
            Console.WriteLine("[RevivalManager] 모든 부활 상태를 초기화했습니다.");
        }
        
        public List<string> GetRevivalTargetsByReviver(string reviverId)
        {
            var targets = new List<string>();
            lock (_revivalLock)
            {
                foreach (var kvp in _activeRevivals)
                {
                    if (kvp.Value.ReviverId == reviverId)
                    {
                        targets.Add(kvp.Key);
                    }
                }
            }
            return targets;
        }
    }
}