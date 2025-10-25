using Proyecto_Gaming.Models.Rawg;

namespace Proyecto_Gaming.Services
{
    public interface IRawgService
    {
        Task<GameResponse> GetGamesAsync(string? search = null, string? genres = null, string? platforms = null, int page = 1);
        Task<Game> GetGameDetailsAsync(int id);
        Task<List<Genre>> GetGenresAsync();
        Task<List<Platform>> GetPlatformsAsync();
        
        // MÉTODOS OPTIMIZADOS
        Task<FilterData> GetAvailableFiltersAsync();
        Task PreloadFirst100GamesAsync();
        Task<Game> GetGameExtendedDetailsAsync(int id);
        Task<List<GameScreenshot>> GetGameScreenshotsAsync(int id);
        Task<List<GameTrailer>> GetGameTrailersAsync(int id);
        Task<SystemRequirements> GetGameRequirementsAsync(int id);

        // ✅ NUEVO: Método para tracking de búsquedas (ML)
        Task TrackUserSearchAsync(string userId, string searchTerm, string genre, string platform);
        
        // ❌❌❌ ELIMINAR COMPLETAMENTE ESTA LÍNEA:

    }

    public class FilterData
    {
        public List<Genre> AvailableGenres { get; set; } = new List<Genre>();
        public List<Platform> AvailablePlatforms { get; set; } = new List<Platform>();
    }

}