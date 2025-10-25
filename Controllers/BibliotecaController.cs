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
using System.Text.Json; // ✅ AGREGAR ESTE
using System.Text.Json.Serialization; // ✅ AGREGAR ESTE
using Microsoft.Extensions.Caching.Distributed; 

namespace Proyecto_Gaming.Controllers
{
    public class BibliotecaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly IRawgService _rawgService;
        private readonly IGamePriceService _priceService;

        public BibliotecaController(ApplicationDbContext context, 
                                  UserManager<Usuario> userManager,
                                  IRawgService rawgService, 
                                  IGamePriceService priceService)
        {
            _context = context;
            _userManager = userManager;
            _rawgService = rawgService;
            _priceService = priceService;
        }

        // GET: Biblioteca - CON SESIONES Y TRACKING ML
        public async Task<IActionResult> Index(string? search, string? genre, string? platform, int page = 1)
        {
            // ✅ DIAGNÓSTICO COMPLETO
            Console.WriteLine("=== 🚀 DIAGNÓSTICO BIBLIOTECA CONTROLLER ===");
            Console.WriteLine($"📊 Parámetros recibidos - Search: '{search}', Genre: '{genre}', Platform: '{platform}', Page: {page}");
            
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

                // ✅ DEBUG ESPECÍFICO PARA LA LLAMADA PRINCIPAL
                Console.WriteLine($"🔍 ANTES de llamar a RAWG API - Page: {page}");
                Console.WriteLine($"🔍 Parámetros enviados - Search: '{search}', Genre: '{genre}', Platform: '{platform}'");
                
                var gameResponse = await _rawgService.GetGamesAsync(search, genre, platform, page);
                
                // ✅ DEBUG DETALLADO DE LA RESPUESTA
                Console.WriteLine($"📦 RESPUESTA CRUDA - Count: {gameResponse?.Count}, HasResults: {gameResponse?.Results?.Any()}");
                Console.WriteLine($"📦 Next: {gameResponse?.Next ?? "NULL"}, Previous: {gameResponse?.Previous ?? "NULL"}");
                
                if (gameResponse?.Results != null)
                {
                    Console.WriteLine($"🎮 Juegos en respuesta: {gameResponse.Results.Count}");
                    if (gameResponse.Results.Any())
                    {
                        Console.WriteLine("📋 Primeros 3 juegos:");
                        foreach (var game in gameResponse.Results.Take(3))
                        {
                            Console.WriteLine($"   - {game.Name} (ID: {game.Id}, Rating: {game.Rating})");
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ Lista de juegos VACÍA pero existe");
                        // ✅ DEBUG ADICIONAL: Verificar qué está pasando
                        Console.WriteLine("🔍 Verificando respuesta completa...");
                        Console.WriteLine($"🔍 Count: {gameResponse.Count}, Next: {gameResponse.Next}, Previous: {gameResponse.Previous}");
                    }
                }
                else
                {
                    Console.WriteLine("❌ gameResponse o Results es NULL");
                }

                // ✅ OBTENER PRECIOS
                Console.WriteLine("🔄 Obteniendo precios de CheapShark...");
                var prices = new Dictionary<int, decimal?>();
                if (gameResponse?.Results != null && gameResponse.Results.Any())
                {
                    foreach (var game in gameResponse.Results)
                    {
                        if (!string.IsNullOrEmpty(game.Name))
                        {
                            var price = await _priceService.GetGamePriceAsync(game.Name);
                            prices[game.Id] = price;
                            if (price.HasValue)
                            {
                                Console.WriteLine($"   💰 Precio para {game.Name}: ${price.Value}");
                            }
                        }
                    }
                }
                Console.WriteLine($"✅ Precios obtenidos: {prices.Count} juegos con precio");

                // ✅ CREAR VIEWMODEL
                var viewModel = new GameCatalogViewModel
                {
                    Games = gameResponse?.Results ?? new List<Game>(),
                    Genres = availableFilters?.AvailableGenres ?? new List<Genre>(),
                    Platforms = availableFilters?.AvailablePlatforms ?? new List<Platform>(),
                    Search = search,
                    SelectedGenre = genre,
                    SelectedPlatform = platform,
                    CurrentPage = page,
                    TotalPages = Math.Min((int)Math.Ceiling((gameResponse?.Count > 0 ? gameResponse.Count : 1) / 20.0), 5),
                    HasNextPage = !string.IsNullOrEmpty(gameResponse?.Next) && page < 5,
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

                Console.WriteLine($"✅ ViewModel creado exitosamente - Juegos: {viewModel.Games.Count}, Precios: {viewModel.GamePrices.Count}");
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
                    RecentSearchTerms = new List<string>()
                });
            }
        }

        // ✅ TESTS DE DIAGNÓSTICO
        public async Task<IActionResult> TestRawgConnection()
        {
            try
            {
                Console.WriteLine("🧪 ========== TEST RAWG CONNECTION ==========");
                
                // Test 1: Llamada directa igual que en Index
                Console.WriteLine("🧪 Test 1: Llamada sin parámetros (como Index)");
                var testResponse1 = await _rawgService.GetGamesAsync("", "", "", 1);
                Console.WriteLine($"🧪 Test 1 - Count: {testResponse1.Count}, Games: {testResponse1.Results?.Count}");
                
                // Test 2: Con parámetros null
                Console.WriteLine("🧪 Test 2: Llamada con parámetros null");
                var testResponse2 = await _rawgService.GetGamesAsync(null, null, null, 1);
                Console.WriteLine($"🧪 Test 2 - Count: {testResponse2.Count}, Games: {testResponse2.Results?.Count}");
                
                // Test 3: Con búsqueda popular
                Console.WriteLine("🧪 Test 3: Con búsqueda 'the'");
                var testResponse3 = await _rawgService.GetGamesAsync("the", null, null, 1);
                Console.WriteLine($"🧪 Test 3 - Count: {testResponse3.Count}, Games: {testResponse3.Results?.Count}");
                
                // Test 4: Verificar primeros juegos
                if (testResponse3.Results?.Any() == true)
                {
                    Console.WriteLine("🧪 Primeros 3 juegos del Test 3:");
                    foreach (var game in testResponse3.Results.Take(3))
                    {
                        Console.WriteLine($"🧪   - {game.Name} (ID: {game.Id})");
                    }
                }

                return Content($"🧪 Tests completados. Revisa los logs del servidor.<br>" +
                              $"Test 1: {testResponse1.Count} juegos, {testResponse1.Results?.Count} resultados<br>" +
                              $"Test 2: {testResponse2.Count} juegos, {testResponse2.Results?.Count} resultados<br>" +
                              $"Test 3: {testResponse3.Count} juegos, {testResponse3.Results?.Count} resultados");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test Error: {ex.Message}");
                Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                return Content($"❌ ERROR: {ex.Message}");
            }
        }

        // ✅ TEST DE FILTROS
        public async Task<IActionResult> TestFilters()
        {
            try
            {
                Console.WriteLine("🧪 ========== TEST FILTERS ==========");
                
                var filters = await _rawgService.GetAvailableFiltersAsync();
                Console.WriteLine($"🧪 Filtros - Géneros: {filters?.AvailableGenres?.Count}, Plataformas: {filters?.AvailablePlatforms?.Count}");
                
                if (filters?.AvailableGenres?.Any() == true)
                {
                    Console.WriteLine("🧪 Primeros 5 géneros:");
                    foreach (var genre in filters.AvailableGenres.Take(5))
                    {
                        Console.WriteLine($"🧪   - {genre.Name} (ID: {genre.Id}, Slug: {genre.Slug})");
                    }
                }
                
                if (filters?.AvailablePlatforms?.Any() == true)
                {
                    Console.WriteLine("🧪 Primeras 5 plataformas:");
                    foreach (var platform in filters.AvailablePlatforms.Take(5))
                    {
                        Console.WriteLine($"🧪   - {platform.Name} (ID: {platform.Id}, Slug: {platform.Slug})");
                    }
                }

                return Content($"🧪 Test Filtros completado.<br>" +
                              $"Géneros: {filters?.AvailableGenres?.Count}<br>" +
                              $"Plataformas: {filters?.AvailablePlatforms?.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test Filters Error: {ex.Message}");
                return Content($"❌ ERROR: {ex.Message}");
            }
        }

        // ✅ TEST COMPLETO DEL FLUJO
        public async Task<IActionResult> TestCompleteFlow()
        {
            try
            {
                Console.WriteLine("🧪 ========== TEST COMPLETE FLOW ==========");

                if (!User.Identity?.IsAuthenticated ?? false)
                {
                    return Content("❌ No autenticado");
                }

                var usuario = await _userManager.GetUserAsync(User);
                Console.WriteLine($"🧪 Usuario: {usuario?.UserName}");

                // Simular el flujo completo del Index
                var search = "";
                var genre = "";
                var platform = "";
                var page = 1;

                Console.WriteLine("🧪 1. Obteniendo filtros...");
                var filters = await _rawgService.GetAvailableFiltersAsync();
                Console.WriteLine($"🧪    Filtros: {filters?.AvailableGenres?.Count} géneros, {filters?.AvailablePlatforms?.Count} plataformas");

                Console.WriteLine("🧪 2. Obteniendo juegos...");
                var games = await _rawgService.GetGamesAsync(search, genre, platform, page);
                Console.WriteLine($"🧪    Juegos: {games.Count} total, {games.Results?.Count} en página");

                Console.WriteLine("🧪 3. Obteniendo precios...");
                var prices = new Dictionary<int, decimal?>();
                if (games.Results?.Any() == true)
                {
                    foreach (var game in games.Results.Take(3))
                    {
                        var price = await _priceService.GetGamePriceAsync(game.Name);
                        prices[game.Id] = price;
                        Console.WriteLine($"🧪    Precio {game.Name}: {price?.ToString("C") ?? "N/A"}");
                    }
                }

                Console.WriteLine("🧪 4. Creando ViewModel...");
                var viewModel = new GameCatalogViewModel
                {
                    Games = games.Results ?? new List<Game>(),
                    AvailableGenres = filters?.AvailableGenres?.Select(g => new SelectListItem
                    {
                        Value = g.Slug ?? g.Id.ToString(),
                        Text = g.Name ?? "Desconocido"
                    }).ToList() ?? new List<SelectListItem>(),
                    AvailablePlatforms = filters?.AvailablePlatforms?.Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = p.Name ?? "Desconocido"
                    }).ToList() ?? new List<SelectListItem>(),
                    GamePrices = prices
                };

                Console.WriteLine($"🧪    ViewModel: {viewModel.Games.Count} juegos, {viewModel.GamePrices.Count} precios");

                return Content($"🧪 Test Complete Flow EXITOSO<br>" +
                              $"Juegos: {viewModel.Games.Count}<br>" +
                              $"Precios: {viewModel.GamePrices.Count}<br>" +
                              $"Géneros: {viewModel.AvailableGenres.Count}<br>" +
                              $"Plataformas: {viewModel.AvailablePlatforms.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test Complete Flow Error: {ex.Message}");
                Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                return Content($"❌ ERROR: {ex.Message}");
            }
        }
        public async Task<IActionResult> TestDeserialization()
        {
            try
            {
                Console.WriteLine("🧪 ========== TEST DESERIALIZATION ==========");

                // Test directo con HttpClient
                using var httpClient = new HttpClient();
                var url = "https://api.rawg.io/api/games?key=90d320b222334660826f587ddb91e577&page=1&page_size=3&ordering=-rating";

                Console.WriteLine($"🧪 URL: {url}");
                var response = await httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"🧪 Response Status: {response.StatusCode}");
                Console.WriteLine($"🧪 Content Length: {content.Length}");
                Console.WriteLine($"🧪 First 300 chars: {content.Substring(0, Math.Min(300, content.Length))}...");

                // ✅ ESPECIFICAR EL NAMESPACE COMPLETO
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // ✅ USAR: Proyecto_Gaming.Models.Rawg.GameResponse
                var gameResponse = JsonSerializer.Deserialize<Proyecto_Gaming.Models.Rawg.GameResponse>(content, options);

                Console.WriteLine($"🧪 Deserialization - Count: {gameResponse?.Count}, Results: {gameResponse?.Results?.Count}");

                if (gameResponse?.Results?.Any() == true)
                {
                    Console.WriteLine("🧪 Primeros juegos deserializados:");
                    foreach (var game in gameResponse.Results.Take(3))
                    {
                        Console.WriteLine($"🧪   - {game.Name} (ID: {game.Id}, Rating: {game.Rating})");
                        Console.WriteLine($"🧪     Genres: {game.Genres?.Count}, Platforms: {game.Platforms?.Count}");
                    }
                }

                return Content($"🧪 Test Deserialization<br>" +
                            $"Status: {response.StatusCode}<br>" +
                            $"Count: {gameResponse?.Count}<br>" +
                            $"Results: {gameResponse?.Results?.Count}<br>" +
                            $"Content Length: {content.Length}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test Deserialization Error: {ex.Message}");
                Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                return Content($"❌ ERROR: {ex.Message}");
            }
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

        // ✅ NUEVO: Obtener recomendaciones basadas en historial de sesión
        public async Task<IActionResult> Recomendaciones()
        {
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                TempData["Error"] = "Debes iniciar sesión para ver recomendaciones.";
                return RedirectToAction("Login", "Account");
            }

            var usuario = await _userManager.GetUserAsync(User);
            
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

            TempData["Ok"] = "Recomendaciones basadas en tu historial de búsquedas";
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
                var gameDetails = await _rawgService.GetGameExtendedDetailsAsync(id);
                if (gameDetails?.Id == 0)
                    return NotFound();

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