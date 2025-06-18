using System.Collections.Concurrent;
using System.Numerics;

namespace DefenseGameWebSocketServer.Manager
{

    public class PlayerManager
    {
        public ConcurrentDictionary<string, Player> _playersDict = new ConcurrentDictionary<string, Player>();
        public void AddOrUpdatePlayer(Player player)
        {
            _playersDict[player.id] = player;
        }
        public bool TryGetPlayer(string playerId, out Player player)
        {
            return _playersDict.TryGetValue(playerId, out player);
        }

        public void RemovePlayer(string playerId)
        {
            _playersDict.TryRemove(playerId, out _);
        }

        public IEnumerable<Player> GetAllPlayers()
        {
            return _playersDict.Values;
        }
        public IEnumerable<string> GetAllPlayerIds()
        {
            return _playersDict.Keys;
        }
        public void setPlayerPosition (string playerId, float x, float y)
        {
            if(TryGetPlayer(playerId, out Player player))
            {
                player.PositionUpdate(x, y);
            }
        }
    }
}
