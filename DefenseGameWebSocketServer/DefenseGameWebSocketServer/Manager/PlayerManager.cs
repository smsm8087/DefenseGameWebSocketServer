using System.Collections.Concurrent;
using System.Numerics;

namespace DefenseGameWebSocketServer.Manager
{

    public class PlayerManager
    {
        private RevivalManager _revivalManager;
        public ConcurrentDictionary<string, Player> _playersDict;
        public PlayerManager()
        {
            _playersDict = new ConcurrentDictionary<string, Player>();
        }

        public PlayerManager(RevivalManager revivalManager) : this()
        {
            _revivalManager = revivalManager;
        }

        public void SetRevivalManager(RevivalManager revivalManager)
        {
            _revivalManager = revivalManager;
        }

        public void AddOrUpdatePlayer(Player player)
        {
            player.RevivalManagerReference = _revivalManager;
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
        public void addCardToPlayer(string playerId, int cardId)
        {
            if (TryGetPlayer(playerId, out Player player))
            {
                player.addCardId(cardId);
            }
        }
        public (int , bool) getPlayerAttackPower(string playerId)
        {
            if (TryGetPlayer(playerId, out Player player))
            {
                return player.getDamage();
            }
            return (0,false);
        }
        public (float, float) addUltGauge(string playerId)
        {
            if (TryGetPlayer(playerId, out Player player))
            {
                player.addUltGauge();
                return (player.currentUlt, 100f);
            }
            return (0f, 100f);
        }
        public Player GetRandomPlayer()
        {
            var alivePlayers = _playersDict.Values.Where(p => !p.IsDead).ToArray();
            if (alivePlayers.Length == 0)
            {
                return null;
            }
            var random = new Random();
            int index = random.Next(alivePlayers.Length);
            return alivePlayers[index];
        }
        
        public bool AreAllPlayersDead()
        {
            if (_playersDict.Count == 0)
                return false;
        
            return _playersDict.Values.All(player => player.IsDead);
        }

        public IEnumerable<Player> GetAlivePlayers()
        {
            return _playersDict.Values.Where(player => !player.IsDead);
        }
        public void Dispose()
        {
            _playersDict.Clear();
        }
    }
}
