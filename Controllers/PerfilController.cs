using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Proyecto_Gaming.Models;
using Proyecto_Gaming.ViewModels;
using Proyecto_Gaming.Data;
using Proyecto_Gaming.Services;
using System.Diagnostics;

namespace Proyecto_Gaming.Controllers
{
    public class PerfilController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IStatsService _statsService;

        public PerfilController(UserManager<Usuario> userManager,
                              ApplicationDbContext context,
                              IWebHostEnvironment environment,
                              IStatsService statsService)
        {
            _userManager = userManager;
            _context = context;
            _environment = environment;
            _statsService = statsService;
        }

        // GET: Perfil/Index
        public async Task<IActionResult> Index(string? userId = null)
        {
            Usuario? usuario;

            if (string.IsNullOrEmpty(userId))
            {
                // Ver perfil propio
                usuario = await _userManager.GetUserAsync(User);
                if (usuario == null)
                {
                    TempData["Error"] = "Debes iniciar sesión para ver tu perfil.";
                    return RedirectToAction("Login", "Account");
                }
            }
            else
            {
                // Ver perfil de otro usuario (para futuro)
                usuario = await _userManager.FindByIdAsync(userId);
                if (usuario == null)
                {
                    return NotFound();
                }
            }

            var viewModel = await ConstruirPerfilViewModel(usuario);
            return View(viewModel);
        }

        private async Task<PerfilViewModel> ConstruirPerfilViewModel(Usuario usuario)
        {
            // Obtener biblioteca reciente (últimos 3 juegos) usando EF Core
            var bibliotecaReciente = await _context.BibliotecaUsuario
                .Where(b => b.UsuarioId == usuario.Id)
                .OrderByDescending(b => b.Id)
                .Take(3)
                .ToListAsync();

            // Datos visuales para amigos (no funcional aún)
            var amigosVisual = new List<UsuarioAmigoViewModel>
            {
                new UsuarioAmigoViewModel { Nombre = "ProGamer99", Estado = "En Línea", Avatar = "PG" },
                new UsuarioAmigoViewModel { Nombre = "NubMaster", Estado = "Ausente", Avatar = "NM" },
                new UsuarioAmigoViewModel { Nombre = "GameLover", Estado = "Jugando", Avatar = "GL" }
            };

            // Obtener estadísticas generales del usuario
            var stats = await _statsService.GetUserStatsAsync(usuario.Id);
            var totalJuegos = stats.TotalGames;

            // Contar grupos del usuario
            var groupsCount = await _context.MiembrosGrupo.CountAsync(m => m.UsuarioId == usuario.Id);

            return new PerfilViewModel
            {
                Usuario = usuario,
                AmigosCount = stats.FriendsCount,
                TotalJuegos = stats.TotalGames,
                TotalHoras = stats.TotalHours,
                JuegosPendientes = stats.TotalGames - stats.CompletedGames,
                JuegosJugando = 0, // pendiente de campo real
                JuegosCompletados = stats.CompletedGames,
                GruposCount = groupsCount,
                BibliotecaReciente = bibliotecaReciente,
                AmigosVisual = amigosVisual,
                JuegosDestacados = bibliotecaReciente.Take(2).ToList()
            };
        }
    }
}