using System.Text.Json.Serialization;

namespace Proyecto_Gaming.Models.Rawg
{
    public class Game
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("slug")]
        public string? Slug { get; set; }
        
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("released")]
        public string? Released { get; set; }
        
        [JsonPropertyName("background_image")]
        public string? BackgroundImage { get; set; }
        
        [JsonPropertyName("rating")]
        public double Rating { get; set; }
        
        [JsonPropertyName("rating_top")]
        public int RatingTop { get; set; }
        
        [JsonPropertyName("ratings_count")]
        public int RatingsCount { get; set; }
        
        [JsonPropertyName("metacritic")]
        public int? Metacritic { get; set; }
        
        [JsonPropertyName("playtime")]
        public int Playtime { get; set; }
        
        [JsonPropertyName("genres")]
        public List<Genre> Genres { get; set; } = new List<Genre>();
        
        [JsonPropertyName("platforms")]
        public List<PlatformInfo> Platforms { get; set; } = new List<PlatformInfo>();
        
        [JsonPropertyName("short_screenshots")]
        public List<Screenshot> ShortScreenshots { get; set; } = new List<Screenshot>();
        
        // Propiedades adicionales para detalles extendidos
        public string? Description { get; set; }
        public string? DescriptionRaw { get; set; }
        public SystemRequirements? Requirements { get; set; }
        public List<GameScreenshot> Screenshots { get; set; } = new List<GameScreenshot>();
        public List<GameTrailer>? Trailers { get; set; }
    }

    public class Genre
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("slug")]
        public string? Slug { get; set; }
    }

    public class PlatformInfo
    {
        [JsonPropertyName("platform")]
        public Platform? Platform { get; set; }
    }

    public class Platform
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("slug")]
        public string? Slug { get; set; }
    }

    public class Screenshot
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("image")]
        public string? Image { get; set; }
    }

    public class SystemRequirements
    {
        [JsonPropertyName("minimum")]
        public string? Minimum { get; set; }

        [JsonPropertyName("recommended")]
        public string? Recommended { get; set; }
    }

    public class GameScreenshot
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }
    }

    public class GameTrailer
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("preview")]
        public string? Preview { get; set; }

        [JsonPropertyName("data")]
        public Dictionary<string, string>? Data { get; set; }
    }
}