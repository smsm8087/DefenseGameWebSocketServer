using System;
using System.Collections.Concurrent;

namespace DefenseGameWebSocketServer.Managers
{
    public class ConfirmationContext
    {
        public string PlayerId { get; set; }
        public Action OnApproved { get; set; }
        public Action OnRejected { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public static class ConfirmationManager
    {
        // requestId → 컨텍스트
        private static ConcurrentDictionary<string, ConfirmationContext> _pending 
            = new ConcurrentDictionary<string, ConfirmationContext>();

        public static string Add(ConfirmationContext ctx)
        {
            var id = Guid.NewGuid().ToString();
            ctx.PlayerId = ctx.PlayerId;
            _pending[id] = ctx;
            return id;
        }

        public static bool TryRemove(string requestId, out ConfirmationContext ctx)
        {
            return _pending.TryRemove(requestId, out ctx);
        }
    }
}