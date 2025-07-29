using DefenseGameWebSocketServer.Models;
using Microsoft.Extensions.Hosting;

namespace DefenseGameWebSocketServer.Manager
{
    public class RoomManager
    {
        private readonly Dictionary<string, Room> _rooms = new();
        private static RoomManager _instance;
        public static RoomManager Instance => _instance ??= new RoomManager();
        public Room CreateRoom(string roomCode, string hostId, string nickName)
        {
            Room room = new Room() {
                RoomCode = roomCode,
                HostId = hostId
            };
            Console.WriteLine($"[Room] Created room with code: {roomCode} | hostId : {hostId}");

            _rooms[roomCode] = room;
            AddPlayer(roomCode, hostId, nickName, true);
            return room;
        }
        public Room GetRoomByPlayerId(string playerId)
        {
            if(string.IsNullOrEmpty(playerId))
            {
                LogManager.Error("[RoomManager] GetRoomByPlayerId: playerId is null or empty.");
                return null;
            }
            return _rooms.Values.FirstOrDefault(room => room.RoomInfos.Find(x => x.playerId == playerId) != null);
        }
        public void RemoveRoom(string roomCode)
        {
            if (_rooms.ContainsKey(roomCode))
            {
                _rooms.Remove(roomCode);
            }
        }
        public IEnumerable<Room> GetAllRooms() => _rooms.Values;
        public bool RoomExists(string roomCode) => _rooms.ContainsKey(roomCode);

        public Room GetRoom(string roomCode) =>
            _rooms.TryGetValue(roomCode, out var room) ? room : null;

        public void AddPlayer(string roomCode, string playerId, string nickName, bool isReady = false)
        {
            if (_rooms.TryGetValue(roomCode, out var room))
            {
                if (room.RoomInfos.Find(x => x.playerId == playerId) == null)
                    room.RoomInfos.Add(new RoomInfo { playerId = playerId, nickName = nickName });
                Console.WriteLine($"[Room] addPlayer: {roomCode} | playerId : {playerId}");
            }
        }
        public bool RemovePlayer(string roomCode, string playerId)
        {
            if (_rooms.TryGetValue(roomCode, out var room))
            {
                if (room.RoomInfos.Find(x => x.playerId == playerId) != null)
                    room.RemoveRoomInfo(playerId);
                Console.WriteLine($"[Room] removePlayer: {roomCode} | playerId : {playerId}");
                return true;
            }
            return false;
        }
        public bool ExistPlayer(string roomCode, string playerId)
        {
            if (_rooms.TryGetValue(roomCode, out var room))
            {
                return room.RoomInfos.Find(x => x.playerId == playerId) != null;
            }
            return false;
        }
    }
}
