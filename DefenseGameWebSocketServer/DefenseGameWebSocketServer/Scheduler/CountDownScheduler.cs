using DefenseGameWebSocketServer.Model;

public class CountDownScheduler
{
    private readonly IWebSocketBroadcaster _broadcaster;
    private readonly CancellationTokenSource _cts;
    private readonly Func<bool> _hasPlayerCount;

    public CountDownScheduler(IWebSocketBroadcaster broadcaster, CancellationTokenSource cts, Func<bool> hasPlayerCount)
    {
        _broadcaster = broadcaster;
        _cts = cts;
        _hasPlayerCount = hasPlayerCount;
    }

    public async Task StartAsync()
    {
        for (int i = 0; i < 5; i++)
        {
            // 웨이브 시작 전 잠시 대기
            if (_cts.Token.IsCancellationRequested || !_hasPlayerCount()) return;

            //웨이브 시작전 카운트다운 메시지 전송
            var msg = new CountDownMesasge(5 - i);
            await _broadcaster.BroadcastAsync(msg);

            await Task.Delay(1000, _cts.Token);
            Console.WriteLine($"[CountDownScheduler] 시작 전 카운트다운 중...{i + 1}초");
        }
        //시작 메시지 전송
        var start_msg = new CountDownMesasge(-1, "start!!");
        await _broadcaster.BroadcastAsync(start_msg);
        await Task.Delay(1000, _cts.Token);

        var deActive_msg = new CountDownMesasge(-1, string.Empty);
        await _broadcaster.BroadcastAsync(deActive_msg);
    }
}