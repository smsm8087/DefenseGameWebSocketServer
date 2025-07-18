namespace DefenseGameWebSocketServer.Model
{
    public class GameResultMessage : BaseMessage
    {
        
        public string result_type { get; set; }
        //아이템 데이터 추가되면 그때 추가
        //public string result_items { get; set; } 
        //public float account_exp { get; set; }

        public GameResultMessage(
            string result_type
        )
        {
            type = "game_result";
            this.result_type = result_type;
        }
    }
}
