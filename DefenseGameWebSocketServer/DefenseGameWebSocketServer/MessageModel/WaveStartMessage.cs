namespace DefenseGameWebSocketServer.Model
{
    public class WaveStartMessage : BaseMessage
    {
        public int wave { get; set; }
        
        public WaveStartMessage(int wave = 1)
        {
            type = "wave_start";
            this.wave = wave;
        }
    }
}