namespace DefenseGameWebSocketServer.Models
{
    // 서버 → 클라이언트 : 알림(확인 버튼만)
    public class NoticeMessage
    {
        public string type { get; } = "notice";
        public string message { get; set; }
        public NoticeMessage(string msg) { message = msg; }
    }

    // 서버 → 클라이언트 : 예/아니오 물어보기
    public class ConfirmMessage
    {
        public string type { get; } = "confirm";
        public string message { get; set; }
        public string requestId { get; set; }
        public ConfirmMessage(string msg, string id)
        {
            message = msg;
            requestId = id;
        }
    }

    // 클라이언트 → 서버 : 예/아니오 응답
    public class ConfirmResponse
    {
        public string type { get; set; }
        public string requestId { get; set; }
        public bool approved { get; set; }
    }
}