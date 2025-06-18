namespace DefenseGameWebSocketServer.Model
{
    public class RestartMessage : BaseMessage
    {
        //치트임. 로그용 플레이어아이디 푸시
        public string playerId { get; set; }
        public RestartMessage(
            string playerId
        )
        {
            type = "restart";
            this.playerId = playerId;
        }
    }
}
