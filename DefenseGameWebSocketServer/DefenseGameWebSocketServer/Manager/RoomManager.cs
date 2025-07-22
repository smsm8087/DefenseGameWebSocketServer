using DefenseGameWebSocketServer.Models;

namespace DefenseGameWebSocketServer.Manager
{
    public class RoomManager
    {
        private readonly Dictionary<string, Room> _rooms = new();

        public Room CreateRoom(string roomCode, string hostId)
        {
            Room room = new Room() {
                RoomCode = roomCode,
                HostId = hostId
            };
            _rooms[roomCode] = room;
            AddPlayer(roomCode, hostId, true);
            return room;
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
                if (!room.PlayerLoadingStatus.ContainsKey(playerId))
                    room.PlayerLoadingStatus[playerId] = false;
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
        public void SetLoadingComplete(string roomCode, string playerId)
        {
            if (_rooms.TryGetValue(roomCode, out var room))
            {
                if (room.PlayerLoadingStatus.ContainsKey(playerId))
                    room.PlayerLoadingStatus[playerId] = true;
            }
        }
    }
}
