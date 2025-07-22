using DefenseGameWebSocketServer.Models;

namespace DefenseGameWebSocketServer.Manager
{
    public class RoomManager
    {
        private readonly Dictionary<string, Room> _rooms = new();
        private static RoomManager _instance;
        public static RoomManager Instance => _instance ??= new RoomManager();
        public Room CreateRoom(string roomCode, string hostId, WebSocketBroadcaster broadcaster)
        {
            Room room = new Room(broadcaster) {
                RoomCode = roomCode,
                HostId = hostId
            };
            _rooms[roomCode] = room;
            AddPlayer(roomCode, hostId, true);
            return room;
        }
        public void RemoveRoom(string roomCode)
        {
            if (_rooms.ContainsKey(roomCode))
            {
                _rooms.Remove(roomCode);
            }
        }

        public bool RoomExists(string roomCode) => _rooms.ContainsKey(roomCode);

        public Room GetRoom(string roomCode) =>
            _rooms.TryGetValue(roomCode, out var room) ? room : null;

        public void AddPlayer(string roomCode, string playerId, bool isReady = false)
        {
            if (_rooms.TryGetValue(roomCode, out var room))
            {
                room.playerIds.Add(playerId);
                if (!room.PlayerReadyStatus.ContainsKey(playerId))
                    room.PlayerReadyStatus[playerId] = isReady;
                if (!room.PlayerLoadingStatus.ContainsKey(playerId))
                    room.PlayerLoadingStatus[playerId] = false;
            }
        }
    }
}
