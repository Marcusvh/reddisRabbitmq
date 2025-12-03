using Microsoft.AspNetCore.SignalR;
using ReddisRabbitGameLogic.Services;
using ReddisRabbitModel.DTO;
using ReddisRabbitModel.Redis;
using StackExchange.Redis;
using System.Text.Json;

namespace ReddisRabbitAPI
{
    public class GameHub : Hub
    {
        private readonly RedisConnection _redis;
        public GameHub(RedisConnection redis)
        {
            _redis = redis;
        }

        // Existing methods
        public async Task JoinMatch(string matchId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, matchId);
            Console.WriteLine($"Client {Context.ConnectionId} joined match {matchId}");
        }

        public async Task LeaveMatch(string matchId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, matchId);
            Console.WriteLine($"Client {Context.ConnectionId} left match {matchId}");
        }

        public async Task MatchCreated(string matchId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, matchId);
            Console.WriteLine($"Client {Context.ConnectionId} joined newly created match {matchId}");
            await Clients.Caller.SendAsync("MatchJoined", matchId);
        }

        // --- New Lobby Methods ---
        public async Task JoinLobby(string playerId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "lobby");
            Console.WriteLine($"{playerId} joined the lobby");

            // Notify all clients in the lobby
            await Clients.Group("lobby").SendAsync("LobbyUpdate", new { playerId, action = "joined" });
        }

        public async Task LeaveLobby(string playerId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "lobby");
            Console.WriteLine($"{playerId} left the lobby");

            await Clients.Group("lobby").SendAsync("LobbyUpdate", new { playerId, action = "left" });
        }
        // --- Send player action ---
        public async Task SendAction(string matchId, PlayerActionDTO action)
        {
            // Push action to Redis list
            await _redis.Database.ListRightPushAsync(RedisKeys.MatchActions(matchId), JsonSerializer.Serialize(action));
        }

        // --- Optional: notify match updates ---
        public async Task BroadcastMatchUpdate(string matchId, object update)
        {
            await Clients.Group(matchId).SendAsync("TurnUpdate", update);
        }
        // --- Existing OnConnectedAsync ---
        public override async Task OnConnectedAsync()
        {
            string? playerId = Context.GetHttpContext()?.Request.Query["playerId"];

            if (!string.IsNullOrEmpty(playerId))
            {
                // auto-join match if player has one
                string matchId = await _redis.Database.StringGetAsync(RedisKeys.PlayerMatch(playerId));
                if (!string.IsNullOrEmpty(matchId))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, matchId);
                    Console.WriteLine($"Player {playerId} auto-joined match {matchId}");
                }

                // optionally auto-join lobby
                await Groups.AddToGroupAsync(Context.ConnectionId, "lobby");
                await Clients.Group("lobby").SendAsync("LobbyUpdate", new { playerId, action = "joined" });
            }

            await base.OnConnectedAsync();
        }
    }
}
