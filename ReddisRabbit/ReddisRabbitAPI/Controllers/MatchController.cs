using Microsoft.AspNetCore.Mvc;
using ReddisRabbitGameLogic.Services;
using ReddisRabbitModel.DTO;
using ReddisRabbitModel.Redis;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ReddisRabbitAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MatchController : ControllerBase
    {
        private readonly RedisConnection _redis;
        private readonly MatchService _matchService;
        public MatchController(RedisConnection redis)
        {
            _redis = redis;
            _matchService = new(redis);
        }
        // GET: api/<MatchController>
        [HttpGet("{matchId}/state")]
        public async Task<IActionResult> Get(string matchId)
        {
            var turn = await _redis.Database.StringGetAsync($"match:{matchId}:turn");
            var players = await _redis.Database.ListRangeAsync($"match:{matchId}:players");
            return Ok(new
            {
                matchId,
                turn = (int)turn,
                players = players.Select(o => o.ToString()).ToList()
            });
        }
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromQuery] string p1, [FromQuery] string p2)
        {
            var matchId = await _matchService.CreateMatchAsync(p1, p2);
            return Ok(new { matchId });
        }
        // GET api/<MatchController>/5
        [HttpGet("matchmaking/status")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<MatchController>
        [HttpPost("matchmaking/join")]
        public void Post([FromBody] string value)
        {
        }
        [HttpPost("matchmaking/{matchId}/action")]
        public async Task<IActionResult> MatchmakingAction(string matchId, PlayerActionDTO dto)
        {
            string dtoConvert = JsonSerializer.Serialize(dto);
            var action = await _redis.Database.ListRightPushAsync(RedisKeys.MatchActions(matchId), dtoConvert);
            return Ok();
        }
        

        // PUT api/<MatchController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<MatchController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
