using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5215); // HTTP 포트 지정
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

//WEBSOCKET
#region  websocket
app.UseWebSockets();
var sockets = new ConcurrentDictionary<string, WebSocket>();
var playerPositions = new ConcurrentDictionary<string, (float x, float y)>();

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var playerId = Guid.NewGuid().ToString();
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        sockets[playerId] = webSocket;
        playerPositions[playerId] = (0, 0); // 처음 위치 (0,0)으로

        // 입장 알림: 접속자 모두에게
        var joinMsg = Encoding.UTF8.GetBytes($"{{\"type\":\"player_join\",\"playerId\":\"{playerId}\"}}");
        foreach (var sock in sockets.Values)
        {
            if (sock.State == WebSocketState.Open)
                await sock.SendAsync(joinMsg, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        // 접속한 유저에게 전체 플레이어 리스트 전송
        var playerListMsg = JsonSerializer.Serialize(new
        {
            type = "player_list",
            players = sockets.Keys.ToArray()
        });
        await webSocket.SendAsync(Encoding.UTF8.GetBytes(playerListMsg), WebSocketMessageType.Text, true, CancellationToken.None);

        var buffer = new byte[1024 * 4];
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    // (3) 메시지 파싱
                    using var doc = JsonDocument.Parse(msg);
                    var root = doc.RootElement;
                    var type = root.GetProperty("type").GetString();

                    if (type == "move")
                    {
                        // (3-1) 좌표 정보 받기
                        var x = root.GetProperty("x").GetSingle();
                        var y = root.GetProperty("y").GetSingle();
                        playerPositions[playerId] = (x, y);
                        var isJumping = root.GetProperty("isJumping").GetBoolean();
                        var isRunning = root.GetProperty("isRunning").GetBoolean();

                        // (3-2) 모든 플레이어에 위치 정보 브로드캐스트
                        var moveMsg = JsonSerializer.Serialize(new { type = "move", playerId, x, y, isJumping, isRunning });
                        foreach (var pair in sockets)
                        {
                            if (pair.Value.State == WebSocketState.Open)
                                await pair.Value.SendAsync(Encoding.UTF8.GetBytes(moveMsg), WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "닫힘", CancellationToken.None);
                }
            }
        }
        finally
        {
            // remove socket
            sockets.TryRemove(playerId, out _);
            playerPositions.TryRemove(playerId, out _);

            var leaveMsg = JsonSerializer.Serialize(new { type = "player_leave", playerId });
            foreach (var pair in sockets)
            {
                if (pair.Value.State == WebSocketState.Open)
                    await pair.Value.SendAsync(Encoding.UTF8.GetBytes(leaveMsg), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            webSocket.Dispose();
        }
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});
#endregion

app.Run();
