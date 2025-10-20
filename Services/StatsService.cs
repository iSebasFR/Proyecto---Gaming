using Microsoft.EntityFrameworkCore;
using Proyecto_Gaming.Data;
using Proyecto_Gaming.ViewModels;

namespace Proyecto_Gaming.Services
{
    public class StatsService : IStatsService
    {
        private readonly ApplicationDbContext _db;

        public StatsService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<UserStatsDto> GetUserStatsAsync(string userId)
        {
            // Juegos en la biblioteca
            var games = await _db.BibliotecaUsuario
                .Where(b => b.UsuarioId == userId)
                .ToListAsync();

            var totalGames = games.Count;
            var completedGames = games.Count(g => g.Estado != null && g.Estado.Equals("Completado", StringComparison.OrdinalIgnoreCase));

            // Tiempo total jugado: si no hay un campo explícito, intentaremos sumar Calificacion como placeholder
            // Si más adelante agregas un campo TimePlayed en BibliotecaUsuario, cambia esta suma
            var totalHours = games.Sum(g => g.Calificacion); // placeholder: revisar

            // Amigos
            var friends = await _db.Amigos
                .Where(a => a.UsuarioId == userId && a.Estado == "Aceptado")
                .CountAsync();
            // Grupos (miembría)
            var groups = await _db.MiembrosGrupo
                .Where(m => m.UsuarioId == userId)
                .CountAsync();

            // Juegos en estado 'Jugando'
            var jugandoCount = await _db.BibliotecaUsuario
                .Where(b => b.UsuarioId == userId && b.Estado != null && b.Estado.ToLower() == "jugando")
                .CountAsync();

            // Juegos en estado 'Pendiente'
            var pendientesCount = await _db.BibliotecaUsuario
                .Where(b => b.UsuarioId == userId && b.Estado != null && b.Estado.ToLower() == "pendiente")
                .CountAsync();

            var dto = new UserStatsDto
            {
                UserId = userId,
                TotalGames = totalGames,
                CompletedGames = completedGames,
                TotalHours = totalHours,
                FriendsCount = friends,
                JuegosJugando = jugandoCount,
                GroupsCount = groups
            };

            dto.PendingGames = pendientesCount;
            // Calcular top juegos finalizados por nombre y conteo
            dto.TopCompletedGames = games
                .Where(g => g.Estado != null && g.Estado.Equals("Completado", StringComparison.OrdinalIgnoreCase))
                .GroupBy(g => g.GameName)
                .Select(grp => new { Name = grp.Key, Count = grp.Count() })
                .OrderByDescending(x => x.Count)
                .ToDictionary(x => x.Name, x => x.Count);

            // Generar datos mensuales simples: distribuir totalHours en los últimos 6 meses
            const int monthsCount = 6;
            try
            {
                var baseVal = totalHours / monthsCount;
                var remainder = totalHours % monthsCount;
                var list = new List<int>();
                for (int i = 0; i < monthsCount; i++)
                {
                    list.Add(baseVal + (i == 0 ? remainder : 0));
                }
                dto.MonthlyHours = list;
            }
            catch
            {
                dto.MonthlyHours = Enumerable.Repeat(0, monthsCount);
            }
            // Generar datos de reseñas por mes (últimos 6 meses)
            var reviewsTrend = new List<int>();
            var now = DateTime.Now;
            for (int i = 0; i < monthsCount; i++)
            {
                var targetMonth = now.AddMonths(i - (monthsCount - 1));
                var count = games
                    .Where(g => !string.IsNullOrEmpty(g.Resena))
                    .Count(g => {
                        var date = g.FechaResena ?? now;
                        return date.Year == targetMonth.Year && date.Month == targetMonth.Month;
                    });
                reviewsTrend.Add(count);
            }
            dto.MonthlyReviews = reviewsTrend;
            // Calcular conteo total de reseñas realizadas
            dto.ReviewsCount = games.Count(g => !string.IsNullOrEmpty(g.Resena));
            // Generar datos de nuevos amigos por mes (últimos 6 meses)
            var friendsTrend = new List<int>();
            for (int i = 0; i < monthsCount; i++)
            {
                var targetMonth = now.AddMonths(i - (monthsCount - 1));
                var countF = await _db.Amigos
                    .Where(a => a.UsuarioId == userId && a.Estado == "Aceptado" && a.FechaAceptacion.HasValue
                        && a.FechaAceptacion.Value.Year == targetMonth.Year
                        && a.FechaAceptacion.Value.Month == targetMonth.Month)
                    .CountAsync();
                friendsTrend.Add(countF);
            }
            dto.MonthlyFriends = friendsTrend;
            // Generar datos de nuevos grupos por mes (últimos 6 meses)
            var groupsTrend = new List<int>();
            for (int i = 0; i < monthsCount; i++)
            {
                var targetMonth = now.AddMonths(i - (monthsCount - 1));
                var countG = await _db.MiembrosGrupo
                    .Where(m => m.UsuarioId == userId
                        && m.FechaUnion.Year == targetMonth.Year
                        && m.FechaUnion.Month == targetMonth.Month)
                    .CountAsync();
                groupsTrend.Add(countG);
            }
            dto.MonthlyGroups = groupsTrend;

            return dto;
        }
    }
}
