using Proyecto_Gaming.ViewModels;
namespace Proyecto_Gaming.Services
{
    public interface IStatsService
    {
        /// <summary>
        /// Obtiene estadísticas agregadas para un usuario.
        /// </summary>
        Task<UserStatsDto> GetUserStatsAsync(string userId);
    }
}
