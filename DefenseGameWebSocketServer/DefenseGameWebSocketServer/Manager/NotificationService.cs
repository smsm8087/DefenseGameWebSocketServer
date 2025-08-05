using DefenseGameWebSocketServer.Managers;

namespace DefenseGameWebSocketServer.Manager
{
    using DefenseGameWebSocketServer.Models;
    using System.Threading.Tasks;

    public class NotificationService
    {
        private readonly IWebSocketBroadcaster _broadcaster;

        public NotificationService(IWebSocketBroadcaster broadcaster)
        {
            _broadcaster = broadcaster;
        }

        public Task SendNoticeAsync(string playerId, string message)
        {
            LogManager.Info($"[Debug–Server] SendNoticeAsync 호출 → to:{playerId}, msg:\"{message}\"");
            var notice = new NoticeMessage(message);
            LogManager.Info($"[Notice] to {playerId}: {message}", playerId: playerId);
            return _broadcaster.SendToAsync(playerId, notice);
        }
        public Task BroadCastNoticeAsync(string message)
        {
            var notice = new NoticeMessage(message);
            return _broadcaster.BroadcastAsync(notice);
        }

        public Task SendConfirmAsync(string playerId, string question, System.Action onOk, System.Action onCancel)
        {
            var ctx = new ConfirmationContext
            {
                PlayerId   = playerId,
                OnApproved = onOk,
                OnRejected = onCancel
            };
            var requestId = ConfirmationManager.Add(ctx);
            var confirmMsg = new ConfirmMessage(question, requestId);
            LogManager.Info($"[Confirm] to {playerId}: {question} (ReqId={requestId})", playerId: playerId);
            return _broadcaster.SendToAsync(playerId, confirmMsg);
        }
    }
}