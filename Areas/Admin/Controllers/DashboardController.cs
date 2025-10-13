using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Gaming.Models;

namespace Proyecto_Gaming.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DashboardController(UserManager<Usuario> userManager,
                                   RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

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
    }

    public class AdminDashboardVm
    {
        public int TotalUsers { get; set; }
        public int AdminUsers { get; set; }
        public int LockedUsers { get; set; }
    }
}
