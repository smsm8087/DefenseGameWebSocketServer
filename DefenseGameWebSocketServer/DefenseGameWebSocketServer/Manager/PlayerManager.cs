using System.Collections.Concurrent;

namespace DefenseGameWebSocketServer.Manager
{

    public class PlayerManager
    {
        public ConcurrentDictionary<string, (float x, float y)> playerPositions = new ConcurrentDictionary<string, (float x, float y)>();

        public void setPlayerPosition (string playerId, float x, float y)
        {
            playerPositions[playerId] = (x, y);
        }
        public void TryRemove(string playerId)
        {
            playerPositions.TryRemove(playerId, out var position);
        }
    }
}
