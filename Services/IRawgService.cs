using Proyecto_Gaming.Models.Rawg;

namespace Proyecto_Gaming.Services
{
    public interface IRawgService
    {
        Task<GameResponse> GetGamesAsync(string search = null, string genres = null, string platforms = null, int page = 1);
        Task<Game> GetGameDetailsAsync(int id);
        Task<List<Genre>> GetGenresAsync();
        Task<List<Platform>> GetPlatformsAsync();
        
        // MÉTODOS OPTIMIZADOS
        Task<FilterData> GetAvailableFiltersAsync(); // Solo una llamada
        Task PreloadFirst100GamesAsync(); // Precarga en segundo plano
    }

    public class FilterData
    {
        public List<Genre> AvailableGenres { get; set; } = new List<Genre>();
        public List<Platform> AvailablePlatforms { get; set; } = new List<Platform>();
    }
}