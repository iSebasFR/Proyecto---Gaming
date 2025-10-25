using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Gaming.Models;
using Proyecto_Gaming.Services; // ⬅️ importa el servicio de logs
using System.Linq;

namespace Proyecto_Gaming.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogService _logService; // ⬅️ inyectamos el servicio de logs

        public DashboardController(
            UserManager<Usuario> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogService logService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logService  = logService;
        }

        // Panel principal (métricas)
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();

            var adminUsers = 0;
            foreach (var u in users)
            {
                if (await _userManager.IsInRoleAsync(u, "Admin"))
                    adminUsers++;
            }

            var locked = users.Count(u => u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow);

            var vm = new AdminDashboardVm
            {
                TotalUsers = users.Count,
                AdminUsers = adminUsers,
                LockedUsers = locked
            };

            return View(vm);
        }

        // 🔎 Actividad reciente (JSON para la vista)
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Activity(int take = 15)
        {
            var logs = await _logService.GetRecentLogsAsync(take);

            // Devolvemos un payload limpio para el front
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
    }
}
