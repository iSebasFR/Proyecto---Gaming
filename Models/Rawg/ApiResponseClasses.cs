using System.Text.Json.Serialization;

namespace Proyecto_Gaming.Models.Rawg
{
    public class GenreResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }
        
        [JsonPropertyName("results")]
        public List<Genre>? Results { get; set; }
    }

    public class PlatformResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }
        
        [JsonPropertyName("results")]
        public List<Platform>? Results { get; set; }
    }

    public class ScreenshotResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }
        
        [JsonPropertyName("results")]
        public List<GameScreenshot> Results { get; set; } = new List<GameScreenshot>();
    }

    public class TrailerResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }
        
        [JsonPropertyName("results")]
        public List<GameTrailer> Results { get; set; } = new List<GameTrailer>();
    }
}