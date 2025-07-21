namespace DefenseGameWebSocketServer.Models
{
    public class Room
    {
        public string RoomCode { get; set; }
        public string HostId { get; set; }
        public Dictionary<string, bool> PlayerReadyStatus { get; set; } = new();
        public bool IsGameStarted { get; set; } = false;

        public bool AllPlayersReady()
        {
            return PlayerReadyStatus.Values.All(ready => ready);
        }
        public int GetPlayerCount()
        {
            return PlayerReadyStatus.Count;
        }   
    }
}
