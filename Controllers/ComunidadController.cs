using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Proyecto_Gaming.Models;
using Proyecto_Gaming.Data;
using Proyecto_Gaming.ViewModels;
using Proyecto_Gaming.Models.Comunidad;
using System.Diagnostics;

namespace Proyecto_Gaming.Controllers
{
    public class ComunidadController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ComunidadController(UserManager<Usuario> userManager, 
                                 ApplicationDbContext context,
                                 IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _context = context;
            _environment = environment;
        }

        // GET: Comunidad/Index - Página principal de comunidad (Feed)
        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Debes iniciar sesión para acceder a la comunidad.";
                return RedirectToAction("Login", "Account");
            }

            var viewModel = new ComunidadViewModel
            {
                // Estadísticas simuladas (puedes reemplazar con datos reales)
                UsuariosConectados = await _context.Users.CountAsync(),
                GruposActivos = await _context.Grupos.CountAsync(),
                PublicacionesMensuales = await _context.PublicacionesGrupo
                    .Where(p => p.FechaPublicacion >= DateTime.UtcNow.AddDays(-30))
                    .CountAsync(),
                UsuariosSatisfechos = new Random().Next(500, 1000)
            };

            return View(viewModel);
        }
    }
}