namespace DefenseGameWebSocketServer.Model
{
    public class CountDownMesasge : BaseMessage
    {
        public int countDown { get; set; }
        public string message { get; set; }
        public CountDownMesasge(
            string type,
            int countDown = -1,
            string message = ""
        )
        {
            this.type = type;
            this.countDown = countDown;
            this.message = message;
        }
    }
}
