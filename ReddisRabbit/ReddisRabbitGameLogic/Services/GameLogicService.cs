using Microsoft.Extensions.Hosting;
using ReddisRabbitGameLogic.Services;
using ReddisRabbitModel.DTO;
using ReddisRabbitModel.Redis;
using StackExchange.Redis;
using System.Text.Json;

public class GameLogicService : BackgroundService
{
    private readonly RedisConnection _redis;
    private IDatabase _db;
    private IServer _server;

    public GameLogicService(RedisConnection redis)
    {
        _redis = redis;
        _db = redis.Database;
        _server = redis.Connection.GetServer("localhost", 6379);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // 1. Get list of matches (simplified: scan keys)
            var matchKeys = _server.Keys(pattern: "match:*:players");

            foreach (var matchKey in matchKeys)
            {
                var matchId = matchKey.ToString().Split(':')[1];

                // 2. Get current turn
                var turnIndex = (int)await _db.StringGetAsync(RedisKeys.MatchTurn(matchId));
                var players = await _db.ListRangeAsync(RedisKeys.MatchPlayers(matchId));
                if (players.Length == 0) continue;

                var currentPlayerId = players[turnIndex % players.Length];

                // 3. Check for queued actions
                var actionJson = await _db.ListLeftPopAsync(RedisKeys.MatchActions(matchId));
                if (actionJson.IsNullOrEmpty) continue;

                var action = JsonSerializer.Deserialize<PlayerActionDTO>(actionJson);

                // 4. Validate turn
                if (action.PlayerId != currentPlayerId)
                {
                    Console.WriteLine($"Ignoring action from {action.PlayerId}, not their turn");
                    continue;
                }

                // 5. Process action
                await ProcessAction(matchId, action);

                // 6. Increment turn
                await _db.StringIncrementAsync(RedisKeys.MatchTurn(matchId));

                // 7. Publish update
                await _redis.Subscriber.PublishAsync(
                    RedisKeys.MatchPubSub(matchId),
                    JsonSerializer.Serialize(new { matchId, turnPlayer = players[(turnIndex + 1) % players.Length] })
                );
            }

            await Task.Delay(100, stoppingToken);
        }
    }

    private async Task ProcessAction(string matchId, PlayerActionDTO action)
    {
        var playerStateKey = RedisKeys.PlayerState(action.PlayerId);
        var playerHash = await _db.HashGetAllAsync(playerStateKey);
        int hp = (int)playerHash.FirstOrDefault(x => x.Name == "hp").Value;
        int x = (int)playerHash.FirstOrDefault(x => x.Name == "x").Value;
        int y = (int)playerHash.FirstOrDefault(x => x.Name == "y").Value;

        switch (action.Type)
        {
            case ActionType.Move:
                if (action.Direction == Direction.N) y--;
                if (action.Direction == Direction.S) y++;
                if (action.Direction == Direction.E) x++;
                if (action.Direction == Direction.W) x--;
                break;

            case ActionType.Attack:
                // Simplified: reduce HP of any adjacent player by 10
                var players = await _db.ListRangeAsync(RedisKeys.MatchPlayers(matchId));
                foreach (var p in players)
                {
                    if (p == action.PlayerId) continue;

                    var other = await _db.HashGetAllAsync(RedisKeys.PlayerState(p));
                    int ox = (int)other.FirstOrDefault(f => f.Name == "x").Value;
                    int oy = (int)other.FirstOrDefault(f => f.Name == "y").Value;

                    if (Math.Abs(ox - x) + Math.Abs(oy - y) == 1)
                    {
                        int ohp = (int)other.FirstOrDefault(f => f.Name == "hp").Value;
                        ohp -= 10;
                        await _db.HashSetAsync(RedisKeys.PlayerState(p), "hp", ohp);
                    }
                }
                break;

            case ActionType.Endturn:
                break;
        }

        // Save updated player state
        await _db.HashSetAsync(playerStateKey,
        [
            new("hp", hp),
            new("x", x),
            new("y", y)
        ]);
    }
}
