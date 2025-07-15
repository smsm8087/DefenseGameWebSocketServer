using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models.DataModels;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    var port = Environment.GetEnvironmentVariable("PORT");
    var parsedPort = string.IsNullOrEmpty(port) ? 5215 : int.Parse(port);
    options.ListenAnyIP(parsedPort); 
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var broadcaster = new WebSocketBroadcaster();
builder.Services.AddSingleton<IWebSocketBroadcaster>(broadcaster);

GameDataManager.Instance.LoadAllData();

int wave_id = 1; // 임시로 웨이브 ID 설정, 실제 게임 로직에 따라 변경 필요
var GameManager = new GameManager(broadcaster, wave_id);
// 게임 데이터 초기화

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
        Console.WriteLine($"[WebSocket] 플레이어 접속 시도: {playerId}");
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();

        broadcaster.Register(playerId, webSocket);

        //임시로 wave_id = 1로 설정
        await GameManager.InitializeGame(playerId);

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

                        //메시지 처리핸들러
                        await GameManager.ProcessHandler(playerId, msgType, rawMessage);
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
            GameManager.RemovePlayer(playerId);
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
    GameManager.Dispose();
});
app.Run();
