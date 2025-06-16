using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
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
var broadcaster = new WebSocketBroadcaster();
builder.Services.AddSingleton<IWebSocketBroadcaster>(broadcaster);

var cts = new CancellationTokenSource();
var playerManager = new PlayerManager();
var sharedHpManager = new SharedHpManager();
var waveScheduler = new WaveScheduler(broadcaster, cts, ()=> broadcaster.HasPlayers(),sharedHpManager);
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
app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var playerId = Guid.NewGuid().ToString();
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();

        broadcaster.Register(playerId, webSocket);
        // 처음 위치 (0,0)으로
        playerManager.setPlayerPosition(playerId, 0, 0);

        // 전체 브로드캐스트 (join 알림)
        await broadcaster.BroadcastAsync(new { type = "player_join", playerId });

        //한명이상 접속했을때 웨이브 시작
        if (broadcaster.ConnectedCount >= 1)
        {
            waveScheduler.TryStart();
        }

        // 접속자에게 player_list만 개별 전송
        await broadcaster.SendToAsync(playerId, new
        {
            type = "player_list",
            players = broadcaster.GetPlayerIds()
        });

        var buffer = new byte[1024 * 4];
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var rawMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    try
                    {
                        var root = JsonDocument.Parse(rawMessage).RootElement;
                        var typeString = root.GetProperty("type").GetString();
                        var msgType = MessageTypeHelper.Parse(typeString);

                        switch(msgType)
                        {
                            case MessageType.Move:
                                {
                                    var moveHandler = new MoveHandler();
                                    await moveHandler.HandleAsync(playerId, rawMessage, broadcaster, playerManager);
                                }
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[WebSocket] JSON 처리 중 오류: {ex.Message}");
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
            broadcaster.Unregister(playerId);
            playerManager.TryRemove(playerId);
            await broadcaster.BroadcastAsync(new { type = "player_leave", playerId });
            webSocket.Dispose();
        }
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});
#endregion
// 서버 종료 시 안전하게 취소
app.Lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("서버 종료 요청됨 - 웨이브 중지");
    cts.Cancel();
});
app.Run();
