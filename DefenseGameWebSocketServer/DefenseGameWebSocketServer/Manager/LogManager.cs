namespace DefenseGameWebSocketServer.Manager
{
    public static class LogManager
    {
        // [Room:ROOMCODE] [Player:PLAYERID] 
        public static void Info(string message, string roomCode = null, string playerId = null)
        {
            var prefix = "";
            if (!string.IsNullOrEmpty(roomCode))
                prefix += $"[Room:{roomCode}] ";
            if (!string.IsNullOrEmpty(playerId))
                prefix += $"[Player:{playerId}] ";
            Console.WriteLine($"{prefix}{message}");
        }

        // ErrorLog
        public static void Error(string message, string roomCode = null, string playerId = null)
        {
            var prefix = "";
            if (!string.IsNullOrEmpty(roomCode))
                prefix += $"[Room:{roomCode}] ";
            if (!string.IsNullOrEmpty(playerId))
                prefix += $"[Player:{playerId}] ";
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{prefix}{message}");
            Console.ResetColor();
        }
    }
}