using ReddisRabbitModel.Redis;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReddisRabbitGameLogic.Services
{
    public class MatchService
    {
        private readonly IDatabase _db;
        public MatchService(RedisConnection redis)
        {
            _db = redis.Database;
        }
        public async Task<string> CreateMatchAsync(string player1, string player2)
        {
            // 1. Create match ID
            var matchId = Guid.NewGuid().ToString("N");

            // 2. Save players in match
            await _db.ListRightPushAsync(
                RedisKeys.MatchPlayers(matchId),
                [player1, player2]
            );

            // 3. Turn pointer (0 = player1)
            await _db.StringSetAsync(RedisKeys.MatchTurn(matchId), 0);

            // 4. Initialize player states (simple 5x5 map)
            await _db.HashSetAsync(RedisKeys.PlayerState(player1),
            [
                new("hp", 100),
                new("x", 1),
                new("y", 1)
            ]);

            await _db.HashSetAsync(RedisKeys.PlayerState(player2),
            [
                new("hp", 100),
                new("x", 3),
                new("y", 3)
            ]);

            // 5. Initialize empty actions list
            // (No-op in Redis: creating the key happens when first push occurs)

            // 6. Assign each player → match
            await _db.StringSetAsync(RedisKeys.PlayerMatch(player1), matchId);
            await _db.StringSetAsync(RedisKeys.PlayerMatch(player2), matchId);

            return matchId;
        }
    }
}
