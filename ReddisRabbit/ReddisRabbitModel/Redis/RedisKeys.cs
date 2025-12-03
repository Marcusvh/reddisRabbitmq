using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReddisRabbitModel.Redis
{
    public static class RedisKeys
    {
        // ----- MATCH KEYS -----
        public static string Match(string matchId)
            => $"match:{matchId}";

        public static string MatchPlayers(string matchId)
            => $"match:{matchId}:players";   // LIST or SET of player IDs

        public static string MatchTurn(string matchId)
            => $"match:{matchId}:turn";      // INT: index of current player's turn

        public static string MatchActions(string matchId)
            => $"match:{matchId}:actions";   // LIST or STREAM of actions

        public static string MatchPubSub(string matchId)
            => $"pubsub:match:{matchId}";    // channel for updates


        // ----- PLAYER KEYS -----
        public static string Player(string playerId)
            => $"player:{playerId}";

        public static string PlayerState(string playerId)
            => $"player:{playerId}:state";   // HASH: HP, X, Y, etc.

        // Optional: store which match a player is currently in
        public static string PlayerMatch(string playerId)
            => $"player:{playerId}:match";


        // ----- MATCHMAKING KEYS -----
        public static string MatchmakingQueue
            => "matchmaking:queue";          // LIST of player IDs

        public static string MatchmakingAssignment(string playerId)
            => $"matchmaking:assigned:{playerId}"; // STRING: matchId (TTL)
    }
}

