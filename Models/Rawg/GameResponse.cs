using System.Text.Json.Serialization;

namespace Proyecto_Gaming.Models.Rawg
{
    public class GameResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("next")]
        public string Next { get; set; }

        [JsonPropertyName("previous")]
        public string Previous { get; set; }

        [JsonPropertyName("results")]
        public List<Game> Results { get; set; } = new List<Game>();
    }
}