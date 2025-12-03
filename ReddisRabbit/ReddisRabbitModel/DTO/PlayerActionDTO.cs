using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ReddisRabbitModel.DTO
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ActionType { Move, Attack, Endturn}
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Direction { N, E, S, W }
    public class PlayerActionDTO
    {
        public string PlayerId { get; set; }
        public ActionType Type { get; set; }
        public Direction? Direction { get; set; }
    }
}
