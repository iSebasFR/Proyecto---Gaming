using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Proyecto_Gaming.Data;
using Proyecto_Gaming.Models;
using Proyecto_Gaming.Services;
using Proyecto_Gaming.ViewModels;
using Proyecto_Gaming.Models.Rawg;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering; // ← AGREGAR ESTA DIRECTIVA


namespace Proyecto_Gaming.Controllers
{
    public class BibliotecaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly IRawgService _rawgService;

        public BibliotecaController(ApplicationDbContext context, 
                                  UserManager<Usuario> userManager,
                                  IRawgService rawgService)
        {
            _context = context;
            _userManager = userManager;
            _rawgService = rawgService;
        }

        // GET: Biblioteca - ACTUALIZADO CON FILTROS DINÁMICOS
        public async Task<IActionResult> Index(string search, string genre, string platform, int page = 1)
        {
            // Verificar si el usuario está autenticado
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Debes iniciar sesión para acceder al catálogo.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // OBTENER FILTROS DESDE CACHÉ (MUCHO MÁS RÁPIDO)
                var availableFilters = await _rawgService.GetAvailableFiltersAsync();

                // Obtener juegos con filtros aplicados
                var gamesResponse = await _rawgService.GetGamesAsync(search, genre, platform, page);

                var viewModel = new GameCatalogViewModel
                {
                    Games = gamesResponse.Results,
                    Genres = availableFilters.AvailableGenres,
                    Platforms = availableFilters.AvailablePlatforms,
                    Search = search,
                    SelectedGenre = genre,
                    SelectedPlatform = platform,
                    CurrentPage = page,
                    TotalPages = Math.Min((int)Math.Ceiling(gamesResponse.Count / 20.0), 5),
                    HasNextPage = !string.IsNullOrEmpty(gamesResponse.Next) && page < 5,
                    HasPreviousPage = !string.IsNullOrEmpty(gamesResponse.Previous) && page > 1,
                    // Asignar los SelectListItem para los dropdowns
                    AvailableGenres = availableFilters.AvailableGenres
                        .Select(g => new SelectListItem 
                        { 
                            Value = g.Slug, 
                            Text = g.Name,
                            Selected = g.Slug == genre
                        })
                        .ToList(),
                    AvailablePlatforms = availableFilters.AvailablePlatforms
                        .Select(p => new SelectListItem 
                        { 
                            Value = p.Id.ToString(), 
                            Text = p.Name,
                            Selected = p.Id.ToString() == platform
                        })
                        .ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                TempData["Error"] = $"Error al cargar los juegos: {ex.Message}";
                
                return View(new GameCatalogViewModel { 
                    Games = new List<Game>(),
                    Genres = new List<Genre>(),
                    Platforms = new List<Platform>(),
                    AvailableGenres = new List<SelectListItem>(),
                    AvailablePlatforms = new List<SelectListItem>()
                });
            }
        }

        // AddToLibrary - MODIFICADO para trabajar con RAWG IDs
        public async Task<IActionResult> AddToLibrary(int id)  // id ahora es de RAWG
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Debes iniciar sesión para añadir juegos a tu biblioteca.";
                return RedirectToAction("Login", "Account");
            }

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                TempData["Error"] = "No se pudo identificar al usuario.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Obtener detalles del juego desde RAWG
                var gameDetails = await _rawgService.GetGameDetailsAsync(id);
                
                if (gameDetails == null)
                {
                    TempData["Error"] = "El juego no existe en RAWG API.";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar si ya existe en la biblioteca
                var existente = await _context.BibliotecaUsuario
                    .FirstOrDefaultAsync(b => b.IdUsuario == usuario.Id && b.RawgGameId == id);

                if (existente == null)
                {
                    _context.BibliotecaUsuario.Add(new BibliotecaUsuario
                    {
                        IdUsuario = usuario.Id,
                        RawgGameId = id,  // Usar ID de RAWG
                        Estado = "Pendiente",
                        GameName = gameDetails.Name,
                        GameImage = gameDetails.BackgroundImage
                    });

                    await _context.SaveChangesAsync();
                    TempData["Ok"] = $"{gameDetails.Name} añadido a Pendientes.";
                }
                else
                {
                    if (!string.Equals(existente.Estado, "Pendiente"))
                    {
                        existente.Estado = "Pendiente";
                        _context.Update(existente);
                        await _context.SaveChangesAsync();
                        TempData["Ok"] = $"{gameDetails.Name} ahora está en Pendientes.";
                    }
                    else
                    {
                        TempData["Ok"] = $"{gameDetails.Name} ya está en tus Pendientes.";
                    }
                }

                return RedirectToAction(nameof(Pendientes));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al agregar el juego a la biblioteca.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Biblioteca/Pendientes - MODIFICADO para RAWG
        public async Task<IActionResult> Pendientes()
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Debes iniciar sesión para ver tus juegos pendientes.";
                return RedirectToAction("Login", "Account");
            }

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                TempData["Error"] = "No se pudo identificar al usuario.";
                return RedirectToAction("Login", "Account");
            }

            // Obtener juegos pendientes de la base de datos local
            var juegosPendientes = await _context.BibliotecaUsuario
                .Where(bu => bu.IdUsuario == usuario.Id && bu.Estado == "Pendiente")
                .ToListAsync();

            return View(juegosPendientes);
        }

        // GET: Biblioteca/Detalles - AHORA CON RAWG API
        public async Task<IActionResult> Detalles(int id)  // id de RAWG
        {
            try
            {
                var gameDetails = await _rawgService.GetGameDetailsAsync(id);
                
                if (gameDetails == null)
                    return NotFound();

                return View(gameDetails);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar los detalles del juego.";
                return RedirectToAction(nameof(Index));
            }
        }

        // MarkAsPlaying - MANTENIDO igual
        public async Task<IActionResult> MarkAsPlaying(int id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Debes iniciar sesión.";
                return RedirectToAction(nameof(Pendientes));
            }

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                TempData["Error"] = "No se pudo identificar al usuario.";
                return RedirectToAction(nameof(Pendientes));
            }

            var biblioteca = await _context.BibliotecaUsuario
                .FirstOrDefaultAsync(b => b.IdUsuario == usuario.Id &&
                                          b.RawgGameId == id &&  // Cambiado a RawgGameId
                                          b.Estado == "Pendiente");

            if (biblioteca == null)
            {
                TempData["Error"] = "No se encontró el juego en Pendientes.";
                return RedirectToAction(nameof(Pendientes));
            }

            biblioteca.Estado = "Jugando";
            _context.Update(biblioteca);
            await _context.SaveChangesAsync();

            TempData["Ok"] = "¡Disfruta! Marcado como 'Jugando'.";
            return RedirectToAction(nameof(Pendientes));
        }

        // GET: Biblioteca/MiBiblioteca - MODIFICADO para RAWG
        public async Task<IActionResult> MiBiblioteca()
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "Debes iniciar sesión para ver tu biblioteca.";
                return RedirectToAction("Login", "Account");
            }

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                TempData["Error"] = "No se pudo identificar al usuario.";
                return RedirectToAction("Login", "Account");
            }

            var miBiblioteca = await _context.BibliotecaUsuario
                .Where(bu => bu.IdUsuario == usuario.Id)
                .ToListAsync();

            return View(miBiblioteca);
        }
    }
}