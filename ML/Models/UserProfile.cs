namespace Proyecto_Gaming.ML.Models
{
    public class UserProfile
    {
        public string UserId { get; set; } = string.Empty;
        public Dictionary<string, int> PreferredGenres { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> PreferredPlatforms { get; set; } = new Dictionary<string, int>();
        public List<int> RatedGames { get; set; } = new List<int>();
        public int TotalGames { get; set; }
        public string TopGenre { get; set; } = "No data";
        
        // Método para obtener el género favorito
        public string GetTopGenre()
        {
            return PreferredGenres.OrderByDescending(x => x.Value)
                                .FirstOrDefault().Key ?? "No data";
        }
    }
}