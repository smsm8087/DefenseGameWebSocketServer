namespace DefenseGameWebSocketServer.Model
{
    public class CountDownMesasge : BaseMessage
    {
        public int countDown { get; set; }
        public string startMessage { get; set; }
        public CountDownMesasge(
            string type,
            int countDown = -1,
            string startMessage = ""
        )
        {
            this.type = type;
            this.countDown = countDown;
            this.startMessage = startMessage;
        }
    }
}
