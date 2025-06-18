namespace DefenseGameWebSocketServer.Model
{
    public class CountDownMesasge : BaseMessage
    {
        public int countDown { get; set; }
        public string message { get; set; }
        public CountDownMesasge(
            int countDown = -1,
            string message = ""
        )
        {
            type = "countdown";
            this.countDown = countDown;
            this.message = message;
        }
    }
}
