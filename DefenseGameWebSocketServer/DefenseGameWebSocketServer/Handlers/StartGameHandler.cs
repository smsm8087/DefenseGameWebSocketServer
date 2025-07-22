using DefenseGameWebSocketServer.Manager;
using DefenseGameWebSocketServer.Model;
using DefenseGameWebSocketServer.Models;
using System.Text.Json;

public class StartGameHandler
{
    public async Task HandleAsync(string playerId, string rawMessage, IWebSocketBroadcaster broadcaster, RoomManager roomManager, GameManager gameManager)
    {
        var msg = JsonSerializer.Deserialize<StartRoomMessage>(rawMessage);
        if (msg == null)
        {
            Console.WriteLine("[JoinRoomHandler] 잘못된 메시지 수신");
            return;
        }
        string roomCode = msg.roomCode;
        string hostId = msg.playerId;
        List<string> participants = msg.players;

        if(roomManager.RoomExists(roomCode))
        {
            Console.WriteLine($"[{roomCode}] 방이 이미 존재합니다.");
            return;
        }

        //room 초기화 및 참가자 추가
        Room room = roomManager.CreateRoom(roomCode, hostId);
        for(int i = 0; i < participants.Count; i++)
        {
            roomManager.AddPlayer(roomCode, participants[i]);
        }
        //TODO: 게임 시작 로직 추가
        await gameManager.TryConnectGame(room);
        room.IsGameStarted = true;
        //if (room.AllPlayersReady())
        //{
            
        //}
        //else
        //{
        //    Console.WriteLine($"[{roomCode}] 아직 준비 안된 플레이어 있음");
        //}
    }
}
