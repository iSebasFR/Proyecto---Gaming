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
        Task<StatsViewModel> GetAsync(DateTime? from, DateTime? to);
    }

    public class StatsService : IStatsService
    {
        private readonly ApplicationDbContext _db;
        public StatsService(ApplicationDbContext db) => _db = db;

        public async Task<StatsViewModel> GetAsync(DateTime? from, DateTime? to)
        {
            // Rango por defecto: últimos 30 días (UTC)
            var toDt = (to ?? DateTime.UtcNow.Date).Date.AddDays(1).AddTicks(-1);
            var fromDt = (from ?? toDt.Date.AddDays(-30)).Date;

            // ------ Usuarios por rol (Identity) ------
            List<UsersByGroupItem> usersByGroup;
            try
            {
                var userRoles = _db.Set<IdentityUserRole<string>>();
                var roles     = _db.Set<IdentityRole>();

                usersByGroup = await userRoles
                    .GroupBy(ur => ur.RoleId)
                    .Join(roles, g => g.Key, r => r.Id, (g, r) => new UsersByGroupItem
                    {
                        GroupName = r.Name ?? "Sin grupo",
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();
            }
            catch
            {
                // Si no tienes Identity configurado, deja la lista vacía
                usersByGroup = new List<UsersByGroupItem>();
            }

            // ------ Top juegos más comprados en el rango ------
            // Dejado vacío a propósito para compilar sin tu entidad de compras.
            // Cuando me confirmes la entidad/namespace y campos reales, activo el query.
            var topGames = new List<TopGameItem>();

            // EJEMPLO (actívalo cuando tengas la entidad real):
            // try
            // {
            //     var purchases = _db.Set<Proyecto_Gaming.Models.Purchase>(); // <-- CAMBIA al tipo real
            //     topGames = await purchases
            //         .AsNoTracking()
            //         .Where(p => p.OccurredAtUtc >= fromDt && p.OccurredAtUtc <= toDt) // <-- CAMBIA campos si difieren
            //         .Select(p => p.Sku) // <-- CAMBIA a GameName/GameCode/ProductName si corresponde
            //         .GroupBy(sku => sku)
            //         .Select(g => new TopGameItem { Game = g.Key ?? "N/D", Purchases = g.Count() })
            //         .OrderByDescending(x => x.Purchases)
            //         .Take(10)
            //         .ToListAsync();
            // }
            // catch { /* la vista soporta vacío */ }

            return new StatsViewModel
            {
                From = fromDt.Date,
                To   = toDt.Date,
                UsersByGroup = usersByGroup,
                TopGames     = topGames
            };
        }
    }
}
