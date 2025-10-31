using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Gaming.Models;
using Proyecto_Gaming.Areas.AdminV2.ViewModels;

namespace Proyecto_Gaming.Areas.AdminV2.Controllers
{
    [Area("AdminV2")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<Usuario> _userManager;

        public UsersController(UserManager<Usuario> userManager)
        {
            _userManager = userManager;
        }

        // GET: /AdminV2/Users
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var list = new List<UserListViewModel>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                list.Add(new UserListViewModel
                {
                    Id = u.Id,
                    Name = u.DisplayName ?? u.UserName ?? "(sin nombre)",
                    Email = u.Email ?? "(sin email)",
                    Role = roles.FirstOrDefault() ?? "Usuario",
                    IsActive = u.LockoutEnd == null && u.Estado != "Bloqueado"
                });
            }

            return View(list);
        }

        // GET: /AdminV2/Users/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            var u = await _userManager.FindByIdAsync(id);
            if (u == null) return NotFound();

            var vm = new UserProfileViewModel
            {
                Id = u.Id,
                Name = u.DisplayName ?? u.UserName ?? "(sin nombre)",
                Email = u.Email ?? "(sin email)",
                RegisteredDate = u.FechaRegistro
            };
            return View(vm);
        }

        // POST: /AdminV2/Users/ToggleLock/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string id)
        {
            var u = await _userManager.FindByIdAsync(id);
            if (u == null) return NotFound();

            if (u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.UtcNow)
            {
                u.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
                u.Estado = "Bloqueado";
            }
            else
            {
                u.LockoutEnd = null;
                u.Estado = "Activo";
            }

            await _userManager.UpdateAsync(u);
            return RedirectToAction(nameof(Index));
        }
    }
}
