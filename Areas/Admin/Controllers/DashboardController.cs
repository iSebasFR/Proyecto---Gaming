using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Gaming.Data;                // ✅ DbContext
using Proyecto_Gaming.Models;
using Proyecto_Gaming.Services;            // ✅ ILogService
using System.Linq;

namespace Proyecto_Gaming.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogService _logService;
        private readonly ApplicationDbContext _db;

        public DashboardController(
            UserManager<Usuario> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogService logService,
            ApplicationDbContext db) // ✅ inyectamos el DbContext
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logService  = logService;
            _db          = db;
        }

        // Panel principal (métricas)
        public async Task<IActionResult> Index()
        {
            var now = DateTime.UtcNow;
            var weekAgo = now.AddDays(-7);

            // Traemos a memoria para poder usar IsInRoleAsync sin problemas de EF
            var users = await _userManager.Users.AsNoTracking().ToListAsync();

            // Totales
            var totalUsers = users.Count;
            var lockedUsers = users.Count(u => u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow);
            var newUsersThisWeek = users.Count(u => u.FechaRegistro != null && u.FechaRegistro >= weekAgo);

            // Admins
            var adminUsers = 0;
            var adminLocked = 0;
            foreach (var u in users)
            {
                if (await _userManager.IsInRoleAsync(u, "Admin"))
                {
                    adminUsers++;
                    if (u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow)
                        adminLocked++;
                }
            }
            var adminActivos = adminUsers - adminLocked;

            // % Bloqueados
            var lockoutPercent = totalUsers == 0
                ? 0
                : Math.Round((lockedUsers * 100.0) / totalUsers, 1);

            // Eventos (si no tienes la tabla, queda en 0 y no falla)
            int totalEventos = 0;
            try
            {
                // Si tienes una entidad Evento mapeada, esto funciona aunque no tengas DbSet<Evento> en el contexto.
                totalEventos = await _db.Set<Evento>().CountAsync();
            }
            catch
            {
                totalEventos = 0;
            }

            var vm = new AdminDashboardVm
            {
                TotalUsers = totalUsers,
                AdminUsers = adminUsers,
                LockedUsers = lockedUsers,

                // ✅ nuevas métricas
                NewUsersThisWeek = newUsersThisWeek,
                AdminActivos = adminActivos,
                LockoutPercent = lockoutPercent,
                TotalEventos = totalEventos
            };

            return View(vm);
        }

        // 🔎 Actividad reciente (JSON para la vista)
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Activity(int take = 15)
        {
            var logs = await _logService.GetRecentLogsAsync(take);

            var data = logs.Select(l => new
            {
                l.Id,
                l.Action,
                l.TargetUser,
                l.PerformedBy,
                timestamp = l.Timestamp
            });

            return Json(data);
        }
    }

    public class AdminDashboardVm
    {
        public int TotalUsers { get; set; }
        public int AdminUsers { get; set; }
        public int LockedUsers { get; set; }

        // ✅ nuevas métricas que usará la vista
        public int NewUsersThisWeek { get; set; }
        public int AdminActivos { get; set; }
        public double LockoutPercent { get; set; }
        public int TotalEventos { get; set; }
    }

    // ⚠️ Dummy mínimo para poder contar eventos si existe la tabla.
    // Si ya tienes tu propia clase Evento en Models/, puedes borrar esta clase.
    public class Evento { public int Id { get; set; } }
}
