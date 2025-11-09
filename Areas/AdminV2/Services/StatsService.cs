using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Proyecto_Gaming.Areas.AdminV2.ViewModels;
using Proyecto_Gaming.Data;

namespace Proyecto_Gaming.Areas.AdminV2.Services
{
    public interface IStatsService
    {
        // Mantengo la firma original para NO romper llamadas existentes
        Task<StatsViewModel> GetAsync(DateTime? from, DateTime? to);
    }

    public class StatsService : IStatsService
    {
        private readonly ApplicationDbContext _db;
        public StatsService(ApplicationDbContext db) => _db = db;

        public async Task<StatsViewModel> GetAsync(DateTime? from, DateTime? to)
        {
            // =========================
            // 1) RANGO DE FECHAS (UTC)
            // =========================
            // - from: inicio del día (00:00)
            // - to:   fin del día (23:59:59.9999999) => inclusivo
            // - por defecto, últimos 30 días

            static DateTime ToUtcStartOfDay(DateTime? d, DateTime fallbackUtcToday)
            {
                if (d is null) return fallbackUtcToday.AddDays(-30); // default: 30 días antes del to
                var local = d.Value.Date;
                // si vino Unspecified/Local, lo fijamos como UTC de forma segura
                return DateTime.SpecifyKind(local, DateTimeKind.Utc);
            }
            static DateTime ToUtcEndOfDay(DateTime? d, DateTime fallbackUtcToday)
            {
                var baseDate = d is null ? fallbackUtcToday : DateTime.SpecifyKind(d.Value.Date, DateTimeKind.Utc);
                return baseDate.AddDays(1).AddTicks(-1); // fin de día inclusivo
            }

            var todayUtc         = DateTime.UtcNow.Date;
            var toUtcInclusive   = ToUtcEndOfDay(to, todayUtc);
            var fromUtcInclusive = ToUtcStartOfDay(from, toUtcInclusive.Date);

            // Para el ViewModel (usa DateTime)
            var uiFrom = fromUtcInclusive;
            var uiTo   = toUtcInclusive;

            // ==================================
            // 2) USUARIOS POR ROL (TODOS, incluye "Sin rol")
            // ==================================
            List<UsersByGroupItem> usersByGroup;
            try
            {
                var query =
                    from u in _db.Users.AsNoTracking()
                    join ur in _db.UserRoles.AsNoTracking()
                        on u.Id equals ur.UserId into urj
                    from ur in urj.DefaultIfEmpty()
                    join r in _db.Roles.AsNoTracking()
                        on ur.RoleId equals r.Id into rj
                    from r in rj.DefaultIfEmpty()
                    group u by (r != null ? r.Name : "Sin rol") into g
                    select new UsersByGroupItem
                    {
                        GroupName = g.Key!,
                        Count = g.Select(x => x.Id).Distinct().Count()
                    };

                usersByGroup = await query
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();
            }
            catch
            {
                usersByGroup = new List<UsersByGroupItem>();
            }

            // ==================================
            // 3) TOP JUEGOS MÁS COMPRADOS — TODOS
            // ==================================
            // Dejo tres ejemplos listos; activa el que calce con tu entidad real.
            var topGames = new List<TopGameItem>();

            // // EJEMPLO A: entidad Purchase con fecha OccurredAtUtc y navegación Game.Title
            // try
            // {
            //     topGames = await _db.Set<Purchase>().AsNoTracking()
            //         .Where(p => p.OccurredAtUtc >= fromUtcInclusive && p.OccurredAtUtc <= toUtcInclusive)
            //         .GroupBy(p => p.Game.Title)
            //         .Select(g => new TopGameItem { Game = g.Key ?? "N/D", Purchases = g.Count() })
            //         .OrderByDescending(x => x.Purchases)
            //         .Take(10)
            //         .ToListAsync();
            // }
            // catch { topGames = new List<TopGameItem>(); }

            // // EJEMPLO B: entidad Order con fecha CreatedAt y campo GameName
            // try
            // {
            //     topGames = await _db.Set<Order>().AsNoTracking()
            //         .Where(o => o.CreatedAt >= fromUtcInclusive && o.CreatedAt <= toUtcInclusive)
            //         .GroupBy(o => o.GameName)
            //         .Select(g => new TopGameItem { Game = g.Key ?? "N/D", Purchases = g.Count() })
            //         .OrderByDescending(x => x.Purchases)
            //         .Take(10)
            //         .ToListAsync();
            // }
            // catch { topGames = new List<TopGameItem>(); }

            // // EJEMPLO C: entidad GameSale con fecha CreatedAt y navegación Game.Title
            // try
            // {
            //     topGames = await _db.Set<GameSale>().AsNoTracking()
            //         .Where(s => s.CreatedAt >= fromUtcInclusive && s.CreatedAt <= toUtcInclusive)
            //         .GroupBy(s => s.Game.Title)
            //         .Select(g => new TopGameItem { Game = g.Key ?? "N/D", Purchases = g.Count() })
            //         .OrderByDescending(x => x.Purchases)
            //         .Take(10)
            //         .ToListAsync();
            // }
            // catch { topGames = new List<TopGameItem>(); }

            // =========================
            // 4) RETORNO AL VIEWMODEL
            // =========================
            return new StatsViewModel
            {
                From = uiFrom, // 00:00 UTC
                To   = uiTo,   // 23:59:59.9999999 UTC (inclusivo)
                UsersByGroup = usersByGroup,
                TopGames     = topGames
            };
        }
    }
}
