namespace DefenseGameWebSocketServer.Models
{
    public class Room
    {
        public string RoomCode { get; set; }
        public string HostId { get; set; }
        public Dictionary<string, bool> PlayerReadyStatus { get; set; } = new();
        public Dictionary<string, bool> PlayerLoadingStatus { get; set; } = new();
        public bool IsGameStarted { get; set; } = false;

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
