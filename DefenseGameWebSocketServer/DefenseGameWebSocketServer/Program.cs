using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
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

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var socketId = Guid.NewGuid().ToString();
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        sockets.TryAdd(socketId, webSocket);

        var buffer = new byte[1024 * 4];
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    // broadCasting
                    foreach (var pair in sockets)
                    {
                        if (pair.Value.State == WebSocketState.Open)
                        {
                            await pair.Value.SendAsync(
                                Encoding.UTF8.GetBytes(msg),
                                WebSocketMessageType.Text,
                                true,
                                CancellationToken.None
                            );
                        }
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "����", CancellationToken.None);
                }
            }
        }
        finally
        {
            // remove socket
            sockets.TryRemove(socketId, out _);
            webSocket.Dispose();
            Console.WriteLine($"{socketId} ���� ����");
        }
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});
#endregion

app.Run();
