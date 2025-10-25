using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Gaming.Models;
using Proyecto_Gaming.Services; // 👈 Asegúrate de agregar esto


namespace Proyecto_Gaming.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAdminLogService _logService; // 👈 Servicio de logs

        public UsersController(
            UserManager<Usuario> userManager,
            RoleManager<IdentityRole> roleManager,
            IAdminLogService logService) // 👈 Inyectamos el servicio
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logService = logService;
        }

        public async Task<IActionResult> Index()
        {
            var model = new List<UserRowVm>();
            var users = _userManager.Users.ToList(); // 🔥 carga en memoria antes de iterar
            foreach (var u in users)
            {
                var isAdmin = await _userManager.IsInRoleAsync(u, "Admin");
                model.Add(new UserRowVm
                {
                    Id = u.Id,
                    Email = u.Email ?? "",
                    DisplayName = u.DisplayName,
                    Estado = u.Estado,
                    LockoutEnd = u.LockoutEnd,
                    IsAdmin = isAdmin
                });
            }
            return View(model.OrderBy(x => x.Email));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            var locked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;
            user.LockoutEnd = locked ? null : DateTimeOffset.UtcNow.AddYears(50);

            await _userManager.UpdateAsync(user);

            // 🧾 Guardar en el log
            if (locked)
                await _logService.LogAsync("Usuario desbloqueado", user.Email, User.Identity?.Name);
            else
                await _logService.LogAsync("Usuario bloqueado", user.Email, User.Identity?.Name);

            TempData["Ok"] = locked ? "Usuario desbloqueado." : "Usuario bloqueado.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            if (!await _roleManager.RoleExistsAsync("Admin"))
                await _roleManager.CreateAsync(new IdentityRole("Admin"));

            await _userManager.AddToRoleAsync(user, "Admin");

            // 🧾 Log de acción
            await _logService.LogAsync("Rol Admin asignado", user.Email, User.Identity?.Name);

            TempData["Ok"] = "Rol Admin asignado.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAdmin(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null) return NotFound();

            // Evitar que el usuario se quite a sí mismo (opcional)
            if (User.Identity?.Name == user.Email)
            {
                TempData["Error"] = "No puedes quitarte tu propio rol aquí.";
                return RedirectToAction(nameof(Index));
            }

            await _userManager.RemoveFromRoleAsync(user, "Admin");

            // 🧾 Log de acción
            await _logService.LogAsync("Rol Admin retirado", user.Email, User.Identity?.Name);

            TempData["Ok"] = "Rol Admin retirado.";
            return RedirectToAction(nameof(Index));
        }
    }

    public class UserRowVm
    {
        public string Id { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? DisplayName { get; set; }
        public string? Estado { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool IsAdmin { get; set; }
    }
}
