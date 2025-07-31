using CsvHelper.Configuration.Attributes;
using DefenseGameWebSocketServer.Manager;

namespace DefenseGameWebSocketServer.Models
{
    public class RoomInfo
    {
        public string playerId;
        public string nickName;
        public string jobType;
        public bool isReady = false;
        public bool isLoading = false;
    }
    public class ChatInfo
    {
        public string playerId;
        public string nickName;
        public List<string> message;
        public DateTime time;
    }
    public class Room
    {
        public string RoomCode { get; set; }
        public string HostId { get; set; }
        public WebSocketBroadcaster broadCaster { get; }
        public bool IsGameStarted { get; set; } = false;
        public List<RoomInfo> RoomInfos { get; set; } = new();

        public GameManager _gameManager { get; private set; }
        public WaveScheduler _waveScheduler { get; private set; }
        public EnemyManager _enemyManager { get; private set; }
        public Dictionary<string, ChatInfo> ChatListDict;

        private int wave_id = 1;//임시
        public Room()
        {
            broadCaster = new WebSocketBroadcaster();
            _gameManager = new GameManager(this, broadCaster, wave_id);
            ChatListDict = new Dictionary<string, ChatInfo>();
        }
        public RoomInfo GetRoomInfo(string playerId)
        {
            return RoomInfos.FirstOrDefault(info => info.playerId == playerId);
        }
        public bool RemoveRoomInfo(string playerId)
        {
            var roomInfo = GetRoomInfo(playerId);
            if (roomInfo != null)
            {
                RoomInfos.Remove(roomInfo);
                return true;
            }
            return false;
        }
        public bool AllPlayersReady()
        {
            return RoomInfos.All(x => x.isReady);
        }
        public bool AllPlayersLoading()
        {
            return RoomInfos.All(x => x.isLoading);
        }
        public int GetPlayerCount()
        {
            return RoomInfos.Count;
        }
        public void AddChatLog(string playerId, string message)
        {
            if(!ChatListDict.ContainsKey(playerId))
            {
                ChatListDict[playerId] = new ChatInfo
                {
                    playerId = playerId,
                    nickName = RoomInfos.Find(x => x.playerId == playerId).nickName ?? "",
                    message = new List<string>(),
                    time = DateTime.Now,
                };
            }
            ChatListDict[playerId].message.Add(message);
        }
    }
}
