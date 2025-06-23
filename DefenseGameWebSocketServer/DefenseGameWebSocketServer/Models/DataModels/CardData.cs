namespace DefenseGameWebSocketServer.Models.DataModels
{
    public class CardData
    {
        public int id { get; set; }
        public string title { get; set; }
        public string type { get; set; }
        public string grade { get; set; }
        public float gradeProbability { get; set; }
        public float value { get; set; }
    }
}
