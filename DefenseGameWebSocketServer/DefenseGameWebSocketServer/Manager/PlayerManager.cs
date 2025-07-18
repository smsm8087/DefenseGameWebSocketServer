using System.Collections.Concurrent;
using System.Numerics;

namespace DefenseGameWebSocketServer.Manager
{

    public class PlayerManager
    {
        private static PlayerManager _instance;
        public ConcurrentDictionary<string, Player> _playersDict;
        public static PlayerManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PlayerManager();
                }
                return _instance;
            }
        }
        public PlayerManager()
        {
            _playersDict = new ConcurrentDictionary<string, Player>();
            _instance = this;
        }
        
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
    }
}
