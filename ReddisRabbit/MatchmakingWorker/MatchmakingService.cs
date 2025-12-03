using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using ReddisRabbitAPI;
using ReddisRabbitGameLogic.Services;
using ReddisRabbitModel.Redis;
using StackExchange.Redis;
using System.Text.Json;

public class MatchmakingService : BackgroundService
{
    private readonly RedisConnection _redis;
    private readonly MatchService _matchService;

    public MatchmakingService(RedisConnection redis, MatchService matchService)
    {
        _redis = redis;
        _matchService = matchService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var db = _redis.Database;
        var pub = _redis.Subscriber;

        while (!stoppingToken.IsCancellationRequested)
        {
            // Pop 2 players from matchmaking queue
            var player1 = await db.ListLeftPopAsync(RedisKeys.MatchmakingQueue);
            var player2 = await db.ListLeftPopAsync(RedisKeys.MatchmakingQueue);

            if (!player1.IsNullOrEmpty && !player2.IsNullOrEmpty)
            {
                // 1. Create match in Redis
                var matchId = await _matchService.CreateMatchAsync(player1, player2);

                Console.WriteLine($"Created match {matchId} with players {player1} & {player2}");

                // 2. Publish MatchCreated event (optional)
                // Example:
                // PublishToRabbitMQ(new MatchCreatedEvent(matchId, new[]{player1,player2}));
                object payload = new
                {
                    matchId = matchId,
                    players = new[] { player1, player2 }
                };

                string json = JsonSerializer.Serialize(payload);
                
                await pub.PublishAsync(RedisChannel.Pattern("match-created"), json);
            }
            else
            {
                // If not enough players, push back popped players
                if (!player1.IsNullOrEmpty) await db.ListRightPushAsync(RedisKeys.MatchmakingQueue, player1);
                if (!player2.IsNullOrEmpty) await db.ListRightPushAsync(RedisKeys.MatchmakingQueue, player2);
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}
