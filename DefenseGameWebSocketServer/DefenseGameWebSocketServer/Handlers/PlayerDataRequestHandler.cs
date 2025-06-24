using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Models.DataModels;
using DefenseGameWebSocketServer.Model;
using Newtonsoft.Json;

namespace DefenseGameWebSocketServer.Handlers
{
    public class PlayerDataRequestHandler
    {
        public async Task HandleAsync(string playerId, string rawMessage, IWebSocketBroadcaster broadcaster, PlayerManager playerManager)
        {
            try
            {
                var request = JsonConvert.DeserializeObject<PlayerDataRequest>(rawMessage);
                
                if (string.IsNullOrEmpty(request?.playerId))
                {
                    Console.WriteLine("[PlayerDataRequestHandler] 플레이어 ID가 없습니다.");
                    return;
                }

                // 플레이어 찾기
                if (!playerManager.TryGetPlayer(request.playerId, out Player player))
                {
                    Console.WriteLine($"[PlayerDataRequestHandler] 플레이어 {request.playerId}를 찾을 수 없습니다.");
                    return;
                }

                // 플레이어 데이터 테이블에서 직업별 데이터 가져오기
                var playerDataTable = GameDataManager.Instance.GetTable<PlayerData>("player_data");
                PlayerData jobData = null;

                if (playerDataTable != null)
                {
                    // 직업 타입을 기반으로 데이터 찾기
                    jobData = FindPlayerDataByJobType(playerDataTable, player.jobType);
                }

                // 응답 데이터 구성
                var response = new
                {
                    type = "player_data_response",
                    playerData = new
                    {
                        id = jobData?.id ?? GetJobId(player.jobType), // 테이블의 ID 사용
                        job_type = player.jobType,
                        hp = jobData?.hp ?? player.Hp, // 테이블 데이터 우선, 없으면 플레이어 현재 HP
                        ult_gauge = jobData?.ult_gauge ?? 0.1f, // 테이블의 ULT 증가량
                        attack_power = jobData?.attack_power ?? player.AttackPower
                    }
                };

                // 요청한 플레이어에게만 응답 전송
                await broadcaster.SendToAsync(playerId, response);
                
                Console.WriteLine($"[PlayerDataRequestHandler] 플레이어 {playerId}에게 데이터 전송 완료 - 직업: {player.jobType}, ULT 증가량: {jobData?.ult_gauge ?? 0.1f}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PlayerDataRequestHandler] 오류: {ex.Message}");
            }
        }

        private PlayerData FindPlayerDataByJobType(Dictionary<int, PlayerData> playerDataTable, string jobType)
        {
            foreach (var kvp in playerDataTable)
            {
                var data = kvp.Value;
                if (data.job_type == jobType)
                {
                    return data;
                }
            }
            return null;
        }

        private int GetJobId(string jobType)
        {
            // 직업 타입을 테이블 ID로 변환 (CSV 파일 기준)
            return jobType switch
            {
                "tank" => 1,
                "programmer" => 2,
                _ => 1 // 기본값
            };
        }
    }

    // 요청 메시지 모델
    public class PlayerDataRequest : BaseMessage
    {
        public string playerId { get; set; }

        public PlayerDataRequest()
        {
            type = "request_player_data";
        }
    }
}