using CsvHelper.Configuration.Attributes;
using DefenseGameWebSocketServer.Manager;

namespace DefenseGameWebSocketServer.Models
{
    public class Room
    {
        public string RoomCode { get; set; }
        public string HostId { get; set; }
        public WebSocketBroadcaster broadCaster { get; }
        public Dictionary<string, bool> PlayerReadyStatus { get; set; } = new();
        public Dictionary<string, bool> PlayerLoadingStatus { get; set; } = new();
        public bool IsGameStarted { get; set; } = false;
        public List<string> playerIds { get; set; } = new();


        public GameManager _gameManager { get; private set; }
        public WaveScheduler _waveScheduler { get; private set; }
        public EnemyManager _enemyManager { get; private set; }

        private int wave_id = 1;//임시
        public Room()
        {
            broadCaster = new WebSocketBroadcaster();
            _gameManager = new GameManager(this, broadCaster, wave_id);
        }

        public bool AllPlayersReady()
        {
            return PlayerReadyStatus.Values.All(ready => ready);
        }
        public bool AllPlayersLoading()
        {
            return PlayerLoadingStatus.Values.All(loading => loading);
        }
        public int GetPlayerCount()
        {
            return PlayerReadyStatus.Count;
        }   
    }
}
