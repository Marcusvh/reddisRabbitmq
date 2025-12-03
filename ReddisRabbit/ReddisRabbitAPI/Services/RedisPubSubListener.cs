using Microsoft.AspNetCore.SignalR;
using ReddisRabbitGameLogic.Services;
using StackExchange.Redis;
using System.Text.Json;

namespace ReddisRabbitAPI.Services
{
    public class RedisPubSubListener : BackgroundService
    {
        private readonly ISubscriber _sub;
        private readonly IHubContext<GameHub> _hub;

        public RedisPubSubListener(RedisConnection redis, IHubContext<GameHub> hub)
        {
            _sub = redis.Subscriber;
            _hub = hub;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 1. Listen for MATCH CREATED
            _sub.Subscribe("match-created", async (channel, message) =>
            {
                var evt = JsonSerializer.Deserialize<object>(message!);

                await _hub.Clients.All.SendAsync(
                    "MatchCreated",
                    new
                    {
                        type = "match-created",
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        payload = evt
                    }
                );

                Console.WriteLine($"[Gateway] MatchCreated broadcast → {message}");
            });

            // 2. Listen for ALL TURN UPDATES from all matches
            _sub.Subscribe("pubsub:match:*", async (channel, message) =>
            {
                string channelName = channel.ToString();  // e.g. "match:abc123"
                string matchId = channelName.Split(':')[2];

                Console.WriteLine($"[Gateway] TurnUpdate received for match {matchId}: {message}");

                await _hub.Clients.Group(matchId).SendAsync(
                    "TurnUpdate",
                    new
                    {
                        type = "turn-update",
                        matchId = matchId,
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        payload = JsonSerializer.Deserialize<object>(message!)
                    }
                );
            });

            return Task.CompletedTask;
        }
    }
}
