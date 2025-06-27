using DefenseGameWebSocketServer.Models.DataModels;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;

namespace DefenseGameWebSocketServer.Manager
{
    public class CardTableManager
    {
        private static CardTableManager _instance;
        public static CardTableManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CardTableManager();
                }
                return _instance;
            }
        }
        public List<CardData> DrawCards(int drawCount)
        {
            var cardTable = GameDataManager.Instance.GetTable<CardData>("card_data").Values;

            List<CardData> result = new List<CardData>();
            drawCount = Math.Min(drawCount, cardTable.Count);

            for (int i = 0; i < drawCount; i++)
            {
                var availableCards = cardTable.Where(card => !result.Contains(card)).ToList();
                
                int totalPct = availableCards.Sum(card => card.pct);
                if (totalPct == 0) break;
                
                int rand = Random.Shared.Next(1, totalPct + 1);
                int cumulative = 0;

                foreach (var card in availableCards)
                {
                    cumulative += card.pct;
                    if (rand <= cumulative)
                    {
                        result.Add(card);
                        break;
                    }
                }
            }

            Console.WriteLine($"[CardTableManager] 카드 {drawCount}장 뽑기 완료");
            return result;
        }
    }
}
