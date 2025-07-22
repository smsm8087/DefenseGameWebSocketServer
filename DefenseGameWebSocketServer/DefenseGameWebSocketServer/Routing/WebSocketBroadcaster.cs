using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
public interface IWebSocketBroadcaster
{
    Task BroadcastAsync(object message);
    Task SendToAsync(string playerId, object message);
    void Register(string playerId, WebSocket socket);
    void Unregister(string playerId);
}
public class WebSocketBroadcaster : IWebSocketBroadcaster
{
    private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();
    public int ConnectedCount => _sockets.Count;
    public bool HasPlayers() => _sockets.Count > 0;
    public int GetPlayerCount() => _sockets.Count;

    public async Task Dispose()
    {
        foreach (var socket in _sockets.Values)
        {
            if (socket.State == WebSocketState.Open)
            {
                try
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None);
                }
                catch
                {
                    // Ignore errors during shutdown
                }
            }
            socket.Dispose();
        }
        _sockets.Clear();
    }
    public void Register(string playerId, WebSocket socket)
    {
        _sockets[playerId] = socket;
    }

    public void Unregister(string playerId)
    {
        _sockets.TryRemove(playerId, out _);
    }
    public bool ExistPlayer(string playerId)
    {
        return _sockets.ContainsKey(playerId);
    }

    public async Task BroadcastAsync(object message)
    {
        string json = JsonConvert.SerializeObject(message);
        var buffer = Encoding.UTF8.GetBytes(json);

        foreach (var kvp in _sockets)
        {
            var socket = kvp.Value;
            if (socket.State == WebSocketState.Open)
            {
                try
                {
                    await socket.SendAsync(
                        new ArraySegment<byte>(buffer),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );
                }
                catch
                {
                    // 오류나면 제거
                    Unregister(kvp.Key);
                }
            }
        }
    }

    public async Task SendToAsync(string playerId, object message)
    {
        if (_sockets.TryGetValue(playerId, out var socket) && socket.State == WebSocketState.Open)
        {
            string json = JsonConvert.SerializeObject(message);
            var buffer = Encoding.UTF8.GetBytes(json);
            await socket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }
    }
    public string[] GetPlayerIds()
    {
        return _sockets.Keys.ToArray();
    }
}