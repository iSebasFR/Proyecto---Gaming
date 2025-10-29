using Proyecto_Gaming.Models.Rawg;

namespace Proyecto_Gaming.ML.Services
{
    public interface IBibliotecaMLService
    {
        Task<List<Game>> GetPersonalizedRecommendationsAsync(string userId);
        Task<List<Game>> GetSimilarGamesAsync(int gameId);
        Task TrackUserInteractionAsync(string userId, int gameId, string action);
        Task<int> GetUserLibraryCountAsync(string userId);
        Task<string> GetUserTopGenreAsync(string userId);
    }
}