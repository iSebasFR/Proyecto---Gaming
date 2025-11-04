using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Proyecto_Gaming.Models;
using Proyecto_Gaming.ViewModels;
using Proyecto_Gaming.Data;
using Proyecto_Gaming.Services;

// Para PerfilUsuarioVM.MedallaVM
using Proyecto_Gaming.ViewModels.Perfil;
// Alias para evitar ambigüedad
using MedallaVM = Proyecto_Gaming.ViewModels.Perfil.PerfilUsuarioVM.MedallaVM;

namespace Proyecto_Gaming.Controllers
{
    public class PerfilController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IStatsService _statsService;
        private readonly IRewardService _rewardService;

        public PerfilController(
            UserManager<Usuario> userManager,
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            IStatsService statsService,
            IRewardService rewardService
        )
        {
            _userManager = userManager;
            _context = context;
            _environment = environment;
            _statsService = statsService;
            _rewardService = rewardService;
        }

        // GET: Perfil/Index
        public async Task<IActionResult> Index(string? userId = null)
        {
            Usuario? usuario;

            if (string.IsNullOrEmpty(userId))
            {
                usuario = await _userManager.GetUserAsync(User);
                if (usuario == null)
                {
                    TempData["Error"] = "Debes iniciar sesión para ver tu perfil.";
                    return RedirectToAction("Login", "Account");
                }
            }
            else
            {
                usuario = await _userManager.FindByIdAsync(userId);
                if (usuario == null) return NotFound();
            }

            var viewModel = await ConstruirPerfilViewModel(usuario);

            // =========================
            // MEDALLAS DEL USUARIO
            // =========================
            List<MedallaVM>? medallasDesdeServicio = null;
            try
            {
                var medals = await _rewardService.GetUserMedalsAsync(usuario.Id);
                if (medals != null && medals.Count > 0)
                {
                    medallasDesdeServicio = medals
                        .Select(m => new MedallaVM
                        {
                            Id           = m.Id, // <-- antes: MedalId
                            Nombre       = m.Name ?? "Medalla",
                            IconoUrl     = "/img/medals/" + (string.IsNullOrWhiteSpace(m.Icon) ? "m1.png" : m.Icon),
                            Points       = m.Points,
                            GrantedAtUtc = DateTime.UtcNow
                        })
                        .ToList();
                }
            }
            catch
            {
                // fallback abajo
            }

            if (medallasDesdeServicio == null || medallasDesdeServicio.Count == 0)
            {
                viewModel.Medallas = await _context.UserMedals
                    .Where(um => um.UsuarioId == usuario.Id)
                    .Include(um => um.Medal)
                    .OrderByDescending(um => um.GrantedAtUtc)
                    .Select(um => new MedallaVM
                    {
                        Id           = um.MedalId, // <-- antes: MedalId
                        Nombre       = um.Medal != null ? um.Medal.Name : "Medalla",
                        IconoUrl     = "/img/medals/" + (
                                          um.Medal != null && !string.IsNullOrWhiteSpace(um.Medal.Icon)
                                              ? um.Medal.Icon
                                              : "m1.png"),
                        Points       = um.Medal != null ? um.Medal.Points : 0,
                        GrantedAtUtc = um.GrantedAtUtc
                    })
                    .AsNoTracking()
                    .ToListAsync();
            }
            else
            {
                viewModel.Medallas = medallasDesdeServicio;
            }

            return View(viewModel);
        }

        private async Task<PerfilViewModel> ConstruirPerfilViewModel(Usuario usuario)
        {
            var bibliotecaReciente = await _context.BibliotecaUsuario
                .Where(b => b.UsuarioId == usuario.Id)
                .OrderByDescending(b => b.Id)
                .Take(3)
                .ToListAsync();

            var amigosVisual = new List<UsuarioAmigoViewModel>
            {
                new UsuarioAmigoViewModel { Nombre = "ProGamer99", Estado = "En Línea", Avatar = "PG" },
                new UsuarioAmigoViewModel { Nombre = "NubMaster",  Estado = "Ausente",  Avatar = "NM" },
                new UsuarioAmigoViewModel { Nombre = "GameLover",  Estado = "Jugando",  Avatar = "GL" }
            };

            var stats = await _statsService.GetUserStatsAsync(usuario.Id);
            var groupsCount = await _context.MiembrosGrupo.CountAsync(m => m.UsuarioId == usuario.Id);

            return new PerfilViewModel
            {
                Usuario            = usuario,
                AmigosCount        = stats.FriendsCount,
                TotalJuegos        = stats.TotalGames,
                TotalHoras         = stats.TotalHours,
                JuegosPendientes   = stats.TotalGames - stats.CompletedGames,
                JuegosJugando      = 0,
                JuegosCompletados  = stats.CompletedGames,
                GruposCount        = groupsCount,
                BibliotecaReciente = bibliotecaReciente,
                AmigosVisual       = amigosVisual,
                JuegosDestacados   = bibliotecaReciente.Take(2).ToList(),
                Medallas           = new List<MedallaVM>() // se llena en Index
            };
        }
    }
}
