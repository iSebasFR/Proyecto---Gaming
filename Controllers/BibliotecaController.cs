using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Proyecto_Gaming.Data;
using Proyecto_Gaming.Models;
using Proyecto_Gaming.Services;
using Proyecto_Gaming.ViewModels;
using Proyecto_Gaming.Models.Rawg;
using Proyecto_Gaming.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

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

        // GET: Biblioteca - CON SESIONES Y TRACKING ML
        public async Task<IActionResult> Index(string? search, string? genre, string? platform, int page = 1)
        {
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                TempData["Error"] = "Debes iniciar sesión para acceder al catálogo.";
                return RedirectToAction("Login", "Account");
            }

            var usuario = await _userManager.GetUserAsync(User);
            
            // CORREGIDO: Verificar que usuario no sea null
            if (usuario == null)
            {
                TempData["Error"] = "No se pudo identificar al usuario.";
                return RedirectToAction("Login", "Account");
            }

            // ✅ TRACKING PARA ML - Guardar búsqueda actual
            if (!string.IsNullOrEmpty(search) || !string.IsNullOrEmpty(genre) || !string.IsNullOrEmpty(platform))
            {
                var userSearch = new UserSearch
                {
                    SearchTerm = search,
                    Genre = genre,
                    Platform = platform,
                    Timestamp = DateTime.UtcNow
                };

                // Guardar en sesión para ML
                HttpContext.Session.AddSearchToHistory(userSearch);

                // Guardar término de búsqueda reciente
                if (!string.IsNullOrEmpty(search))
                {
                    HttpContext.Session.AddRecentSearch(search);
                }

                // Guardar en Redis para análisis ML
                await _rawgService.TrackUserSearchAsync(usuario.Id, search??"", genre??"", platform??"");
            }

            // ✅ GUARDAR FILTROS EN SESIÓN para persistencia
            if (!string.IsNullOrEmpty(search) || !string.IsNullOrEmpty(genre) || !string.IsNullOrEmpty(platform))
            {
                HttpContext.Session.SetString("LastSearch", search ?? "");
                HttpContext.Session.SetString("LastGenre", genre ?? "");
                HttpContext.Session.SetString("LastPlatform", platform ?? "");
                HttpContext.Session.SetInt32("LastPage", page);
            }

            // ✅ CARGAR PREFERENCIAS DE USUARIO DESDE SESIÓN
            var userPreferences = HttpContext.Session.GetUserPreferences();

            try
            {
                var availableFilters = await _rawgService.GetAvailableFiltersAsync();
                var gamesResponse = await _rawgService.GetGamesAsync(search, genre, platform, page);

                var viewModel = new GameCatalogViewModel
                {
                    Games = gamesResponse.Results ?? new List<Game>(),
                    Genres = availableFilters.AvailableGenres ?? new List<Genre>(),
                    Platforms = availableFilters.AvailablePlatforms ?? new List<Platform>(),
                    Search = search,
                    SelectedGenre = genre,
                    SelectedPlatform = platform,
                    CurrentPage = page,
                    TotalPages = Math.Min((int)Math.Ceiling((gamesResponse.Count > 0 ? gamesResponse.Count : 1) / 20.0), 5),
                    HasNextPage = !string.IsNullOrEmpty(gamesResponse.Next) && page < 5,
                    HasPreviousPage = !string.IsNullOrEmpty(gamesResponse.Previous) && page > 1,
                    AvailableGenres = (availableFilters.AvailableGenres ?? new List<Genre>())
                        .Select(g => new SelectListItem 
                        { 
                            Value = g.Slug ?? "", 
                            Text = g.Name ?? "Sin nombre",
                            Selected = g.Slug == genre
                        })
                        .ToList(),
                    AvailablePlatforms = (availableFilters.AvailablePlatforms ?? new List<Platform>())
                        .Select(p => new SelectListItem 
                        { 
                            Value = p.Id.ToString(), 
                            Text = p.Name ?? "Sin nombre",
                            Selected = p.Id.ToString() == platform
                        })
                        .ToList(),
                    // ✅ NUEVO: Datos de sesión para la vista
                    UserPreferences = userPreferences,
                    RecentSearches = HttpContext.Session.GetSearchHistory().Take(5).ToList(),
                    RecentSearchTerms = HttpContext.Session.GetRecentSearches().Take(5).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar los juegos: {ex.Message}";
                return View(new GameCatalogViewModel { 
                    Games = new List<Game>(),
                    AvailableGenres = new List<SelectListItem>(),
                    AvailablePlatforms = new List<SelectListItem>(),
                    UserPreferences = userPreferences
                });
            }
        }

        // ✅ NUEVO: Restaurar última búsqueda desde sesión
        public async Task<IActionResult> LastSearch()
        {
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                return RedirectToAction("Login", "Account");
            }

            var search = HttpContext.Session.GetString("LastSearch") ?? "";
            var genre = HttpContext.Session.GetString("LastGenre") ?? "";
            var platform = HttpContext.Session.GetString("LastPlatform") ?? "";
            var page = HttpContext.Session.GetInt32("LastPage") ?? 1;
    
            // ✅ REDIRIGIR al Index con los parámetros, no retornar una vista
            TempData["Info"] = "Última búsqueda restaurada desde sesión";
            return RedirectToAction(nameof(Index), new {
                search,
                genre = genre, 
                platform = platform, 
                page = page 
            });
        }

        // ✅ NUEVO: Guardar preferencias de usuario
        [HttpPost]
        public IActionResult SavePreferences(UserPreferences preferences)
        {
            // CORREGIDO: Verificación de autenticación mejorada
            if (User.Identity?.IsAuthenticated ?? false)
            {
                HttpContext.Session.SetUserPreferences(preferences);
                TempData["Ok"] = "Preferencias guardadas correctamente.";
            }
    
            // ✅ REDIRIGIR al Index
            return RedirectToAction(nameof(Index));
        }

        // ✅ NUEVO: Obtener recomendaciones basadas en historial de sesión
        public async Task<IActionResult> Recomendaciones()
        {
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                TempData["Error"] = "Debes iniciar sesión para ver recomendaciones.";
                return RedirectToAction("Login", "Account");
            }

            var usuario = await _userManager.GetUserAsync(User);
            
            // CORREGIDO: Verificar que usuario no sea null
            if (usuario == null)
            {
                TempData["Error"] = "No se pudo identificar al usuario.";
                return RedirectToAction("Login", "Account");
            }

            var searchHistory = HttpContext.Session.GetSearchHistory();
    
            if (!searchHistory.Any())
            {
                TempData["Info"] = "Realiza algunas búsquedas para obtener recomendaciones personalizadas.";
                return RedirectToAction(nameof(Index));
            }

            // ✅ LÓGICA DE RECOMENDACIÓN BASADA EN SESIONES
            var mostSearchedGenre = searchHistory
                .Where(s => !string.IsNullOrEmpty(s.Genre))
                .GroupBy(s => s.Genre)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();

            var mostSearchedPlatform = searchHistory
                .Where(s => !string.IsNullOrEmpty(s.Platform))
                .GroupBy(s => s.Platform)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();

            // ✅ REDIRIGIR al Index con los parámetros de recomendación
            TempData["Ok"] = "Recomendaciones basadas en tu historial de búsquedas";
            return RedirectToAction(nameof(Index), new { 
                genre = mostSearchedGenre, 
                platform = mostSearchedPlatform 
            });
        }

        // AddToLibrary - CON SQL DIRECTO (MANTENIDO)
        public async Task<IActionResult> AddToLibrary(int id)
        {
            if (!User.Identity?.IsAuthenticated ?? false)
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
                var gameDetails = await _rawgService.GetGameDetailsAsync(id);
                
                if (gameDetails == null)
                {
                    TempData["Error"] = "El juego no existe en RAWG API.";
                    return RedirectToAction(nameof(Index));
                }

                // Usar EF Core para añadir la entrada en la biblioteca
                var entry = new BibliotecaUsuario
                {
                    UsuarioId = usuario.Id,
                    RawgGameId = id,
                    Estado = "Pendiente",
                    GameName = gameDetails.Name ?? "información desconocida",
                    GameImage = gameDetails.BackgroundImage ?? "https://via.placeholder.com/400x200?text=Imagen+No+Disponible",
                };

                _context.BibliotecaUsuario.Add(entry);
                await _context.SaveChangesAsync();
                TempData["Ok"] = $"{gameDetails.Name} añadido a Pendientes.";
                return RedirectToAction(nameof(Pendientes));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Biblioteca/Pendientes - CON SQL DIRECTO (MANTENIDO)
        public async Task<IActionResult> Pendientes()
        {
            if (!User.Identity?.IsAuthenticated ?? false)
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

            // Usar EF Core para obtener los juegos pendientes (más seguro y menos propenso a errores de mapeo)
            var juegosPendientes = await _context.BibliotecaUsuario
                .Where(b => b.UsuarioId == usuario.Id && b.Estado.ToLower() == "pendiente")
                .ToListAsync();

            return View(juegosPendientes);
        }

        // GET: Biblioteca/Detalles
        public async Task<IActionResult> Detalles(int id)
        {
            try
            {
                var gameDetails = await _rawgService.GetGameDetailsAsync(id);
                if (gameDetails == null)
                    return NotFound();

                return View(gameDetails);
            }
            catch (Exception)
            {
                TempData["Error"] = "Error al cargar los detalles del juego.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Los demás métodos se mantienen igual...
        // MarkAsPlaying, Jugando, Completados, MarkAsCompleted, AddReview, MiBiblioteca
        // ... (mantener el código existente para estos métodos)

        // GET: Biblioteca/Jugando - CON SQL DIRECTO
        public async Task<IActionResult> Jugando()
        {
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                TempData["Error"] = "Debes iniciar sesión para ver tus juegos en progreso.";
                return RedirectToAction("Login", "Account");
            }

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                TempData["Error"] = "No se pudo identificar al usuario.";
                return RedirectToAction("Login", "Account");
            }

            // Usar EF Core para obtener los juegos en progreso
            var juegosJugando = await _context.BibliotecaUsuario
                .Where(b => b.UsuarioId == usuario.Id && b.Estado.ToLower() == "jugando")
                .ToListAsync();

            return View(juegosJugando);
        }

        // GET: Biblioteca/Completados - CON SQL DIRECTO
        public async Task<IActionResult> Completados()
        {
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                TempData["Error"] = "Debes iniciar sesión para ver tus juegos completados.";
                return RedirectToAction("Login", "Account");
            }

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                TempData["Error"] = "No se pudo identificar al usuario.";
                return RedirectToAction("Login", "Account");
            }

            // Usar EF Core para obtener los juegos completados (más seguro)
            var juegosCompletados = await _context.BibliotecaUsuario
                .Where(b => b.UsuarioId == usuario.Id && b.Estado.ToLower() == "completado")
                .ToListAsync();

            return View(juegosCompletados);
        }

        // MarkAsPlaying - CON SQL DIRECTO
        public async Task<IActionResult> MarkAsPlaying(int id)
        {
            if (!User.Identity?.IsAuthenticated ?? false)
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

            // Usar EF Core para actualizar el estado a 'Jugando'
            var itemToPlay = await _context.BibliotecaUsuario
                .FirstOrDefaultAsync(b => b.UsuarioId == usuario.Id && b.RawgGameId == id && b.Estado.ToLower() == "pendiente");

            if (itemToPlay == null)
            {
                TempData["Error"] = "No se encontró el juego en Pendientes.";
            }
            else
            {
                itemToPlay.Estado = "Jugando";
                _context.BibliotecaUsuario.Update(itemToPlay);
                await _context.SaveChangesAsync();
                TempData["Ok"] = "¡Disfruta! Marcado como 'Jugando'.";
            }
            return RedirectToAction(nameof(Pendientes));
        }

        // MarkAsCompleted - CON SQL DIRECTO
        public async Task<IActionResult> MarkAsCompleted(int id)
        {
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                TempData["Error"] = "Debes iniciar sesión.";
                return RedirectToAction(nameof(Jugando));
            }

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                TempData["Error"] = "No se pudo identificar al usuario.";
                return RedirectToAction(nameof(Jugando));
            }

            // Usar EF Core para actualizar a 'Completado'
            var itemToComplete = await _context.BibliotecaUsuario
                .FirstOrDefaultAsync(b => b.UsuarioId == usuario.Id && b.RawgGameId == id && b.Estado.ToLower() == "jugando");

            if (itemToComplete == null)
            {
                TempData["Error"] = "No se encontró el juego en Jugando.";
            }
            else
            {
                itemToComplete.Estado = "Completado";
                itemToComplete.FechaCompletado = DateTime.UtcNow;
                _context.BibliotecaUsuario.Update(itemToComplete);
                await _context.SaveChangesAsync();

                var game = await _rawgService.GetGameDetailsAsync(id);
                var gameName = game?.Name ?? "el juego";
                TempData["Ok"] = $"¡Felicidades! {gameName} marcado como 'Completado'.";
            }
            return RedirectToAction(nameof(Completados));
        }

        // AddReview - CON SQL DIRECTO
        [HttpPost]
        public async Task<IActionResult> AddReview(int id, string resena, int calificacion)
        {
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                return Json(new { success = false, message = "Debes iniciar sesión." });
            }

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                return Json(new { success = false, message = "No se pudo identificar al usuario." });
            }

            try
            {
                // Usar EF Core para actualizar reseña/calificación (si existen columnas)
                var reviewItem = await _context.BibliotecaUsuario
                    .FirstOrDefaultAsync(b => b.UsuarioId == usuario.Id && b.RawgGameId == id && b.Estado.ToLower() == "completado");

                if (reviewItem == null)
                {
                    return Json(new { success = false, message = "Juego no encontrado." });
                }

                // Algunas migraciones podrían no tener columnas resena/calificacion; actualizar condicionalmente
                try
                {
                    reviewItem.Resena = resena ?? "";
                    reviewItem.Calificacion = calificacion;
                    reviewItem.FechaResena = DateTime.UtcNow;
                    _context.BibliotecaUsuario.Update(reviewItem);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Reseña publicada correctamente." });
                }
                catch
                {
                    return Json(new { success = false, message = "Error al guardar la reseña." });
                }
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error al guardar la reseña." });
            }
        }

        // GET: Biblioteca/MiBiblioteca - CON SQL DIRECTO
        public async Task<IActionResult> MiBiblioteca()
        {
            if (!User.Identity?.IsAuthenticated ?? false)
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

            // Usar EF Core para obtener la biblioteca completa
            var miBiblioteca = await _context.BibliotecaUsuario
                .Where(b => b.UsuarioId == usuario.Id)
                .ToListAsync();

            return View(miBiblioteca);
        }
    }
}