using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models;
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

GameDataManager.Instance.LoadAllData();

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
        string playerId = null;
        string roomCode = null;
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var buffer = new byte[1024 * 4];
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var rawMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    try
                    {
                        var root = JsonDocument.Parse(rawMessage).RootElement;
                        var typeString = root.GetProperty("type").GetString();
                        var msgType = MessageTypeHelper.Parse(typeString);
                        playerId = root.GetProperty("playerId").GetString();
                        roomCode = root.GetProperty("roomCode").GetString();
                        if (playerId == null || roomCode == null)
                        {
                            Console.WriteLine($"[WebSocket] playerId|roomCode {playerId} | {roomCode} 누락된 요청");
                            await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Missing playerId or roomCode", CancellationToken.None);
                            return;
                        }
                        
                        Room room = RoomManager.Instance.GetRoom(roomCode);
                        
                        if (!RoomManager.Instance.RoomExists(roomCode))
                        {
                            //첫 시작 요청은 방장이하므로 방장이 호스팅임.
                            string nickName = root.GetProperty("nickName").GetString();
                            if (nickName == null)
                            {
                                Console.WriteLine($"[WebSocket] nickName 누락된 요청");
                                await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Missing playerId or roomCode", CancellationToken.None);
                                return;
                            }
                            room = RoomManager.Instance.CreateRoom(roomCode, playerId, nickName);
                        } else
                        {
                            //방이 존재하는 경우 플레이어 추가
                            if (!RoomManager.Instance.ExistPlayer(roomCode, playerId))
                            {
                                string nickName = root.GetProperty("nickName").GetString();
                                if (nickName == null)
                                {
                                    Console.WriteLine($"[WebSocket] nickName 누락된 요청");
                                    await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Missing playerId or roomCode", CancellationToken.None);
                                    return;
                                }
                                RoomManager.Instance.AddPlayer(roomCode, playerId, nickName);
                            }
                        }
                        //브로드캐스터에 등록
                        if (!room.broadCaster.ExistPlayer(playerId))
                        {
                            room.broadCaster.Register(playerId, webSocket);
                        }
                        //메시지 처리핸들러
                        await room._gameManager.ProcessHandler(playerId, msgType, rawMessage);
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
        catch (Exception ex)
        {
            Console.WriteLine($"[WebSocket Error] {ex.Message}");
        }
        finally
        {
            if (playerId != null)
            {
                if (roomCode != null && RoomManager.Instance.RoomExists(roomCode))
                {
                    var room = RoomManager.Instance.GetRoom(roomCode);
                    var roomInfo = room.RoomInfos.Find(x => x.playerId == playerId);
                    if (room != null && roomInfo != null)
                    {
                        room.broadCaster.Unregister(playerId);
                        room.RoomInfos.Remove(roomInfo);
                        await room._gameManager.RemovePlayer(playerId);
                        if (room.RoomInfos.Count <= 0)
                        {
                            RoomManager.Instance.RemoveRoom(roomCode);
                        }
                    }
                }
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
// 서버 종료 시 안전하게 취소
app.Lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("서버 종료 요청됨 - 웨이브 중지");
    // RoomManager의 모든 Room의 Broadcaster를 Dispose
    var rooms = RoomManager.Instance.GetAllRooms();
    var tasks = rooms.Select(room => room.broadCaster.Dispose());
    Task.WhenAll(tasks).GetAwaiter().GetResult();
});
app.Run();
