using DefenseGameWebSocketServer.Models;

namespace DefenseGameWebSocketServer.Manager
{
    public class RoomManager
    {
        private readonly Dictionary<string, Room> _rooms = new();

        public void CreateRoom(string roomCode, string hostId)
        {
            _rooms[roomCode] = new Room
            {
                RoomCode = roomCode,
                HostId = hostId
            };
            AddPlayer(roomCode, hostId, true);
        }

        public bool RoomExists(string roomCode) => _rooms.ContainsKey(roomCode);

        public Room GetRoom(string roomCode) =>
            _rooms.TryGetValue(roomCode, out var room) ? room : null;

        public void AddPlayer(string roomCode, string playerId, bool isReady = false)
        {
            if (_rooms.TryGetValue(roomCode, out var room))
            {
                if (!room.PlayerReadyStatus.ContainsKey(playerId))
                    room.PlayerReadyStatus[playerId] = isReady;
            }
        }

        public void SetReady(string roomCode, string playerId)
        {
            if (_rooms.TryGetValue(roomCode, out var room))
            {
                if (room.PlayerReadyStatus.ContainsKey(playerId))
                    room.PlayerReadyStatus[playerId] = true;
            }
        }
    }
}
