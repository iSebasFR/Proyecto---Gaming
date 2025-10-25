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
using System.Text.Json; 
using System.Text.Json.Serialization; 
using Microsoft.Extensions.Caching.Distributed; 
using Proyecto_Gaming.ML.Services; 
namespace Proyecto_Gaming.Controllers
{
    public class BibliotecaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly IRawgService _rawgService;
        private readonly IGamePriceService _priceService;

       private readonly IBibliotecaMLService _mlService;

        public BibliotecaController(ApplicationDbContext context, 
                                UserManager<Usuario> userManager,
                                IRawgService rawgService, 
                                IGamePriceService priceService,
                                Proyecto_Gaming.ML.Services.IBibliotecaMLService mlService) 
        {
            _context = context;
            _userManager = userManager;
            _rawgService = rawgService;
            _priceService = priceService;
            _mlService = mlService; 
        }

        // GET: Biblioteca - CON SESIONES Y TRACKING ML
        public async Task<IActionResult> Index(string? search, string? genre, string? platform, int page = 1)
        {
            // ✅ CONFIGURACIÓN DE LÍMITES OPTIMIZADA
            const int MAX_PAGES = 5;
            const int PAGE_SIZE = 20;
            const int MAX_GAMES_FOR_PRICES = 30; // ✅ REDUCIDO A 30 PARA MÁS RENDIMIENTO

            // Validar página máxima
            if (page > MAX_PAGES)
            {
                page = MAX_PAGES;
                TempData["Info"] = $"Se ha limitado la búsqueda a {MAX_PAGES} páginas para mejor rendimiento.";
            }

            
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                Console.WriteLine("❌ Usuario no autenticado - Redirigiendo a Login");
                TempData["Error"] = "Debes iniciar sesión para acceder al catálogo.";
                return RedirectToAction("Login", "Account");
            }

            var usuario = await _userManager.GetUserAsync(User);
            Console.WriteLine($"👤 Usuario autenticado: {usuario?.UserName ?? "NULL"}, ID: {usuario?.Id ?? "NULL"}");

            // ✅ TRACKING PARA ML
            if (!string.IsNullOrEmpty(search) || !string.IsNullOrEmpty(genre) || !string.IsNullOrEmpty(platform))
            {
                var userSearch = new UserSearch
                {
                    SearchTerm = search,
                    Genre = genre,
                    Platform = platform,
                    Timestamp = DateTime.UtcNow
                };
                HttpContext.Session.AddSearchToHistory(userSearch);
                if (!string.IsNullOrEmpty(search))
                {
                    HttpContext.Session.AddRecentSearch(search);
                }
                await _rawgService.TrackUserSearchAsync(usuario.Id, search??"", genre??"", platform??"");
            }

            // ✅ GUARDAR FILTROS EN SESIÓN
            if (!string.IsNullOrEmpty(search) || !string.IsNullOrEmpty(genre) || !string.IsNullOrEmpty(platform))
            {
                HttpContext.Session.SetString("LastSearch", search ?? "");
                HttpContext.Session.SetString("LastGenre", genre ?? "");
                HttpContext.Session.SetString("LastPlatform", platform ?? "");
                HttpContext.Session.SetInt32("LastPage", page);
            }

            // ✅ CARGAR PREFERENCIAS DE USUARIO DESDE SESIÓN
            var userPreferences = HttpContext.Session.GetUserPreferences();
            Console.WriteLine($"⚙️ Preferencias de usuario cargadas: {(userPreferences != null ? "SÍ" : "NO")}");

            try
            {
                Console.WriteLine("🔄 Obteniendo filtros disponibles de RAWG...");
                var availableFilters = await _rawgService.GetAvailableFiltersAsync();
                Console.WriteLine($"✅ Filtros cargados - Géneros: {availableFilters?.AvailableGenres?.Count ?? 0}, Plataformas: {availableFilters?.AvailablePlatforms?.Count ?? 0}");

                Console.WriteLine($"🔍 ANTES de llamar a RAWG API - Page: {page}");
                
                var gameResponse = await _rawgService.GetGamesAsync(search, genre, platform, page);
                
                // ✅ FILTRAR JUEGOS: SOLO LOS QUE TIENEN DATOS COMPLETOS
                var filteredGames = FilterCompleteGames(gameResponse?.Results);
                
                Console.WriteLine($"🎮 Juegos obtenidos: {gameResponse?.Results?.Count ?? 0}");
                Console.WriteLine($"✅ Juegos con datos completos: {filteredGames.Count}");

            // ✅ OBTENER PRECIOS Y FILTRAR JUEGOS SIN PRECIO
            Console.WriteLine("🔄 Obteniendo precios y filtrando juegos...");
            var gamesWithPrices = new List<Game>();
            var prices = new Dictionary<int, decimal?>();

            if (filteredGames.Any())
            {
                // ✅ USAR EL NUEVO MÉTODO QUE FILTRA AUTOMÁTICAMENTE
                var gamesForPricing = filteredGames.Take(MAX_GAMES_FOR_PRICES).ToList();
                Console.WriteLine($"💰 Buscando precios para {gamesForPricing.Count} juegos...");
                
                (gamesWithPrices, prices) = await GetGamesWithPricesAsync(gamesForPricing);
                
                Console.WriteLine($"✅ Encontrados {gamesWithPrices.Count} juegos con precio disponible");
            }
            else
            {
                Console.WriteLine("⚠️ No hay juegos válidos para obtener precios");
            }

                // ✅ CALCULAR PÁGINAS CON LÍMITE
                var totalResults = gameResponse?.Count ?? 0;
                var limitedTotalPages = Math.Min((int)Math.Ceiling(totalResults / (double)PAGE_SIZE), MAX_PAGES);

                // ✅ CREAR VIEWMODEL SOLO CON JUEGOS QUE TIENEN PRECIO
                var viewModel = new GameCatalogViewModel
                {
                    Games = gamesWithPrices, // ✅ SOLO JUEGOS CON PRECIO DISPONIBLE
                    Genres = availableFilters?.AvailableGenres ?? new List<Genre>(),
                    Platforms = availableFilters?.AvailablePlatforms ?? new List<Platform>(),
                    Search = search,
                    SelectedGenre = genre,
                    SelectedPlatform = platform,
                    CurrentPage = page,
                    TotalPages = limitedTotalPages,
                    HasNextPage = !string.IsNullOrEmpty(gameResponse?.Next) && page < MAX_PAGES,
                    HasPreviousPage = !string.IsNullOrEmpty(gameResponse?.Previous) && page > 1,
                    AvailableGenres = (availableFilters?.AvailableGenres ?? new List<Genre>())
                        .Select(g => new SelectListItem 
                        { 
                            Value = g.Slug ?? g.Id.ToString(), 
                            Text = g.Name ?? "Desconocido",
                            Selected = g.Slug == genre || g.Id.ToString() == genre
                        }).ToList(),
                    AvailablePlatforms = (availableFilters?.AvailablePlatforms ?? new List<Platform>())
                        .Select(p => new SelectListItem 
                        { 
                            Value = p.Id.ToString(), 
                            Text = p.Name ?? "Desconocido",
                            Selected = p.Id.ToString() == platform
                        }).ToList(),
                    UserPreferences = userPreferences,
                    RecentSearches = HttpContext.Session.GetSearchHistory(),
                    RecentSearchTerms = HttpContext.Session.GetRecentSearches(),
                    HasPreviousSearch = !string.IsNullOrEmpty(HttpContext.Session.GetString("LastSearch")),
                    GamePrices = prices
                };

                // ✅ INFORMACIÓN DE OPTIMIZACIÓN
                if (filteredGames.Count < (gameResponse?.Results?.Count ?? 0))
                {
                    var filteredOut = (gameResponse?.Results?.Count ?? 0) - filteredGames.Count;
                    TempData["Info"] = $"Mostrando {filteredGames.Count} juegos completos. Se omitieron {filteredOut} juegos incompletos.";
                }
               // ✅ INFORMACIÓN SOBRE PRECIOS
                if (gamesWithPrices.Count < filteredGames.Count)
                {
                    var withoutPrice = filteredGames.Count - gamesWithPrices.Count;
                    if (TempData["Info"] != null)
                    {
                        TempData["Info"] += $" | {withoutPrice} sin precio";
                    }
                    else
                    {
                        TempData["Info"] = $"Mostrando {gamesWithPrices.Count} juegos con precio. {withoutPrice} juegos sin precio omitidos.";
                    }
                } 

                Console.WriteLine($"✅ ViewModel creado - Juegos: {viewModel.Games.Count}, Precios: {viewModel.GamePrices.Count}");
                Console.WriteLine("=== ✅ DIAGNÓSTICO COMPLETADO ===");

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 ERROR CRÍTICO: {ex.Message}");
                Console.WriteLine($"💥 StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"Error al cargar los juegos: {ex.Message}";
                
                return View(new GameCatalogViewModel { 
                    Games = new List<Game>(),
                    AvailableGenres = new List<SelectListItem>(),
                    AvailablePlatforms = new List<SelectListItem>(),
                    UserPreferences = userPreferences,
                    RecentSearches = new List<UserSearch>(),
                    RecentSearchTerms = new List<string>(),
                    TotalPages = MAX_PAGES
                });
            }
        }

        // ✅ MÉTODO PARA FILTRAR JUEGOS COMPLETOS
        private List<Game> FilterCompleteGames(List<Game>? games)
        {
            if (games == null) return new List<Game>();

            var completeGames = games.Where(game =>
                !string.IsNullOrEmpty(game.Name) &&           // ✅ Tiene nombre
                !string.IsNullOrEmpty(game.BackgroundImage) && // ✅ Tiene imagen
                game.Rating > 0 &&                           // ✅ Tiene rating
                (game.Genres?.Any() == true) &&              // ✅ Tiene géneros
                (game.Platforms?.Any() == true) &&           // ✅ Tiene plataformas
                !string.IsNullOrEmpty(game.Released) &&      // ✅ Tiene fecha de lanzamiento
                DateTime.TryParse(game.Released, out _)      // ✅ Fecha válida
            ).ToList();

            Console.WriteLine($"🔍 Filtrado: {games.Count} → {completeGames.Count} juegos completos");
            return completeGames;
        }

        // ✅ REEMPLAZAR EL MÉTODO EXISTENTE con este NUEVO MÉTODO MEJORADO
        private async Task<(List<Game> gamesWithPrices, Dictionary<int, decimal?> prices)> GetGamesWithPricesAsync(List<Game> games)
        {
            var gamesWithPrices = new List<Game>();
            var prices = new Dictionary<int, decimal?>();
            
            if (!games.Any())
            {
                Console.WriteLine("⚠️ No hay juegos para obtener precios");
                return (gamesWithPrices, prices);
            }

            try
            {
                var semaphore = new SemaphoreSlim(2, 2); // ✅ REDUCIDO A 2 llamadas simultáneas
                var tasks = new List<Task>();
                var processedCount = 0;
                var totalGames = games.Count;
                
                foreach (var game in games)
                {
                    await semaphore.WaitAsync();
                    
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            // ✅ MÁS PAUSA ENTRE LLAMADAS (200ms)
                            if (processedCount > 0)
                            {
                                await Task.Delay(200);
                            }

                            var price = await _priceService.GetGamePriceAsync(game.Name ?? "");
                            
                            lock (prices)
                            {
                                processedCount++;
                                prices[game.Id] = price;
                                
                                // ✅ SOLO AGREGAR A LA LISTA SI TIENE PRECIO
                                if (price.HasValue)
                                {
                                    gamesWithPrices.Add(game);
                                    Console.WriteLine($"   💰 [{processedCount}/{totalGames}] {game.Name}: ${price.Value}");
                                }
                                else
                                {
                                    Console.WriteLine($"   ❌ [{processedCount}/{totalGames}] {game.Name}: Precio no disponible");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            lock (prices)
                            {
                                processedCount++;
                                Console.WriteLine($"   ⚠️ [{processedCount}/{totalGames}] Error en {game.Name}: {ex.Message}");
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }

                Console.WriteLine($"🕐 Esperando {tasks.Count} tareas de precios...");
                await Task.WhenAll(tasks);
                Console.WriteLine($"✅ Tareas completadas: {gamesWithPrices.Count} juegos con precio de {totalGames} total");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en obtención de precios: {ex.Message}");
            }

            return (gamesWithPrices, prices);
        }


    
       // ✅ LIMPIAR CACHÉ DE REDIS
public async Task<IActionResult> ClearCache()
{
    try
    {
        // Obtener el servicio de Redis
        var redisCache = HttpContext.RequestServices.GetService<IDistributedCache>();
        
        if (redisCache != null)
        {
            // Limpiar todas las claves relacionadas con juegos
            await redisCache.RemoveAsync("Games____1");
            await redisCache.RemoveAsync("Games____2");
            await redisCache.RemoveAsync("Games____3");
            await redisCache.RemoveAsync("Games____4");
            await redisCache.RemoveAsync("Games____5");
            await redisCache.RemoveAsync("AvailableFilters");
            await redisCache.RemoveAsync("PreloadedGames");
            
            Console.WriteLine("✅ Caché de Redis limpiado exitosamente");
            TempData["Ok"] = "Caché limpiado. Los juegos se cargarán fresh desde RAWG API.";
        }
        else
        {
            Console.WriteLine("❌ No se pudo obtener el servicio de Redis");
            TempData["Error"] = "No se pudo limpiar el caché.";
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error limpiando caché: {ex.Message}");
        TempData["Error"] = $"Error limpiando caché: {ex.Message}";
    }
    
    return RedirectToAction(nameof(Index));
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

            TempData["Info"] = "Última búsqueda restaurada desde sesión";
            return RedirectToAction(nameof(Index), new
            {
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
            if (User.Identity?.IsAuthenticated ?? false)
            {
                HttpContext.Session.SetUserPreferences(preferences);
                TempData["Ok"] = "Preferencias guardadas correctamente.";
            }
    
            return RedirectToAction(nameof(Index));
        }

       // ✅ NUEVO MÉTODO MEJORADO CON ML
        public async Task<IActionResult> Recomendaciones()
        {
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                TempData["Error"] = "Debes iniciar sesión para ver recomendaciones personalizadas.";
                return RedirectToAction("Login", "Account");
            }

            var usuario = await _userManager.GetUserAsync(User);
            
            if (usuario == null)
            {
                TempData["Error"] = "No se pudo identificar al usuario.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                Console.WriteLine($"🎯 Iniciando sistema ML para {usuario.UserName}...");
                
                // ✅ OBTENER RECOMENDACIONES DEL SERVICIO ML
                var recommendedGames = await _mlService.GetPersonalizedRecommendationsAsync(usuario.Id);
                
                if (!recommendedGames.Any())
                {
                    TempData["Info"] = "Agrega más juegos a tu biblioteca para obtener recomendaciones personalizadas.";
                    return RedirectToAction(nameof(Index));
                }

                // Obtener datos del usuario para mostrar en la vista
                var userLibraryCount = await _mlService.GetUserLibraryCountAsync(usuario.Id);
                var userTopGenre = await _mlService.GetUserTopGenreAsync(usuario.Id);

                // Obtener precios para las recomendaciones
                var gamesWithPrices = new List<Game>();
                var prices = new Dictionary<int, decimal?>();

                if (recommendedGames.Any())
                {
                    (gamesWithPrices, prices) = await GetGamesWithPricesAsync(recommendedGames.Take(15).ToList());
                }

                // ✅ CREAR VIEWMODEL CON INFORMACIÓN ML
                var viewModel = new GameCatalogViewModel
                {
                    Games = gamesWithPrices,
                    GamePrices = prices,
                    Search = "🎯 Recomendaciones ML Personalizadas",
                    CurrentPage = 1,
                    TotalPages = 1,
                    UserPreferences = HttpContext.Session.GetUserPreferences(),
                    RecentSearches = HttpContext.Session.GetSearchHistory()
                };

                // ✅ AGREGAR DATOS ML AL VIEWDATA PARA LA VISTA
                ViewData["IsMLRecommendation"] = true;
                ViewData["UserLibraryCount"] = userLibraryCount;
                ViewData["UserTopGenre"] = userTopGenre;
                ViewData["RecommendationCount"] = recommendedGames.Count;

                // ✅ TRACKING DE USO DE RECOMENDACIONES
                await _mlService.TrackUserInteractionAsync(usuario.Id, 0, "view_ml_recommendations");

                TempData["Ok"] = $"¡{recommendedGames.Count} recomendaciones basadas en tu biblioteca personal!";
                return View("Index", viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en recomendaciones ML: {ex.Message}");
                
                // ✅ FALLBACK A MÉTODO ORIGINAL SI EL ML FALLA
                TempData["Info"] = "Usando recomendaciones básicas por fallo temporal en ML";
                return await FallbackToBasicRecommendations();
            }
        }

        // ✅ MÉTODO FALLBACK (tu método original)
        private async Task<IActionResult> FallbackToBasicRecommendations()
        {
            var searchHistory = HttpContext.Session.GetSearchHistory();

            if (!searchHistory.Any())
            {
                TempData["Info"] = "Realiza algunas búsquedas para obtener recomendaciones personalizadas.";
                return RedirectToAction(nameof(Index));
            }

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

            return RedirectToAction(nameof(Index), new { 
                genre = mostSearchedGenre, 
                platform = mostSearchedPlatform 
            });
        }

        // ✅ LIMPIAR HISTORIAL DE BÚSQUEDAS
        public IActionResult ClearSearchHistory()
        {
            HttpContext.Session.Remove("SearchHistory");
            HttpContext.Session.Remove("RecentSearches");
            TempData["Ok"] = "Historial de búsquedas limpiado.";
            return RedirectToAction(nameof(Index));
        }

        // MÉTODOS EXISTENTES (mantener igual)
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
                bool hasPurchased = await _context.Transactions
                    .AnyAsync(t => t.UsuarioId == usuario.Id && t.GameId == id && t.PaymentStatus == "Completed");
                
                if (!hasPurchased)
                {
                    TempData["ErrorMessage"] = "Debes comprar el juego antes de añadirlo a tu biblioteca.";
                    return RedirectToAction("Detalles", "Biblioteca", new { id = id });
                }

                var gameDetails = await _rawgService.GetGameDetailsAsync(id);
                
                if (gameDetails == null)
                {
                    TempData["Error"] = "El juego no existe en RAWG API.";
                    return RedirectToAction(nameof(Index));
                }

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

            var juegosPendientes = await _context.BibliotecaUsuario
                .Where(b => b.UsuarioId == usuario.Id && b.Estado.ToLower() == "pendiente")
                .ToListAsync();

            return View(juegosPendientes);
        }

       public async Task<IActionResult> Detalles(int id)
        {
            try
            {
                // ✅ TRACKING DE VISUALIZACIÓN DE JUEGO (agregar al inicio del método)
                if (User.Identity?.IsAuthenticated ?? false)
                {
                    var usuario = await _userManager.GetUserAsync(User);
                    await _mlService.TrackUserInteractionAsync(usuario.Id, id, "view_game_details");
                }

                var gameDetails = await _rawgService.GetGameExtendedDetailsAsync(id);
                if (gameDetails?.Id == 0)
                    return NotFound();

                // ✅ OBTENER JUEGOS SIMILARES CON ML
                var similarGames = await _mlService.GetSimilarGamesAsync(id);
                ViewData["SimilarGames"] = similarGames;

                var price = await _priceService.GetGamePriceAsync(gameDetails.Name ?? "");
                var deals = await _priceService.GetGameDealsAsync(gameDetails.Name ?? "");
                var bestDeal = deals.FirstOrDefault();

                ViewData["CurrentPrice"] = price?.ToString("0.00");
                ViewData["BestStore"] = bestDeal?.StoreName ?? "Steam";
                ViewData["IsOnSale"] = bestDeal?.IsOnSale ?? false;
                ViewData["StoreOffers"] = deals;

                return View(gameDetails);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar los detalles del juego: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
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

            var juegosJugando = await _context.BibliotecaUsuario
                .Where(b => b.UsuarioId == usuario.Id && b.Estado.ToLower() == "jugando")
                .ToListAsync();

            return View(juegosJugando);
        }

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

            var juegosCompletados = await _context.BibliotecaUsuario
                .Where(b => b.UsuarioId == usuario.Id && b.Estado.ToLower() == "completado")
                .ToListAsync();

            return View(juegosCompletados);
        }

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
                var reviewItem = await _context.BibliotecaUsuario
                    .FirstOrDefaultAsync(b => b.UsuarioId == usuario.Id && b.RawgGameId == id && b.Estado.ToLower() == "completado");

                if (reviewItem == null)
                {
                    return Json(new { success = false, message = "Juego no encontrado." });
                }

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

            var miBiblioteca = await _context.BibliotecaUsuario
                .Where(b => b.UsuarioId == usuario.Id)
                .ToListAsync();

            return View(miBiblioteca);
        }
    }
}