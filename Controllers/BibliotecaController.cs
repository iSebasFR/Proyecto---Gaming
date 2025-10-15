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
public async Task<IActionResult> Index(string search, string genre, string platform, int page = 1)
{
    if (!User.Identity.IsAuthenticated)
    {
        TempData["Error"] = "Debes iniciar sesión para acceder al catálogo.";
        return RedirectToAction("Login", "Account");
    }

    var usuario = await _userManager.GetUserAsync(User);

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
        await _rawgService.TrackUserSearchAsync(usuario.Id, search, genre, platform);
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
        if (!User.Identity.IsAuthenticated)
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
        if (User.Identity.IsAuthenticated)
        {
            HttpContext.Session.SetUserPreferences(preferences);
            TempData["Ok"] = "Preferencias guardadas correctamente.";
         }
    
        // ✅ REDIRIGIR al Index
        return RedirectToAction(nameof(Index));
        }

        // ✅ NUEVO: Obtener recomendaciones basadas en historial de sesión
       // ✅ CORREGIDO: Recomendaciones también debe redirigir al Index
public async Task<IActionResult> Recomendaciones()
{
    if (!User.Identity.IsAuthenticated)
    {
        TempData["Error"] = "Debes iniciar sesión para ver recomendaciones.";
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
                var gameDetails = await _rawgService.GetGameDetailsAsync(id);
                
                if (gameDetails == null)
                {
                    TempData["Error"] = "El juego no existe en RAWG API.";
                    return RedirectToAction(nameof(Index));
                }

                // SQL DIRECTO
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();
                
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO ""BibliotecaUsuario"" 
                        (""UsuarioId"", ""RawgGameId"", ""Estado"", ""GameName"", ""GameImage"", ""Resena"", ""Calificacion"")
                        VALUES (@UsuarioId, @rawgId, 'Pendiente', @gameName, @gameImage, '', 0)";
                    
                    command.Parameters.Add(new Npgsql.NpgsqlParameter("UsuarioId", usuario.Id));
                    command.Parameters.Add(new Npgsql.NpgsqlParameter("rawgId", id));
                    command.Parameters.Add(new Npgsql.NpgsqlParameter("gameName", gameDetails.Name));
                    command.Parameters.Add(new Npgsql.NpgsqlParameter("gameImage", gameDetails.BackgroundImage ?? "https://via.placeholder.com/400x200?text=Imagen+No+Disponible"));
                    
                    await command.ExecuteNonQueryAsync();
                }
                
                await connection.CloseAsync();
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

            // SQL DIRECTO
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();
            
            var juegosPendientes = new List<BibliotecaUsuario>();
            
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT * FROM ""BibliotecaUsuario"" WHERE ""UsuarioId"" = @userId AND ""Estado"" = 'Pendiente'";
                
                var parameter = command.CreateParameter();
                parameter.ParameterName = "userId";
                parameter.Value = usuario.Id;
                command.Parameters.Add(parameter);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        juegosPendientes.Add(new BibliotecaUsuario
                        {
                            Id = reader.GetInt32(0),
                            UsuarioId = reader.GetString(1),
                            RawgGameId = reader.GetInt32(2),
                            Estado = reader.GetString(3),
                            GameName = reader.GetString(4),
                            GameImage = reader.GetString(5),
                            Resena = reader.GetString(6),
                            Calificacion = reader.GetInt32(7),
                            FechaCompletado = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                            FechaResena = reader.IsDBNull(9) ? null : reader.GetDateTime(9)
                        });
                    }
                }
            }
            
            await connection.CloseAsync();
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
            catch (Exception ex)
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
            if (!User.Identity.IsAuthenticated)
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

            // SQL DIRECTO
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();
            
            var juegosJugando = new List<BibliotecaUsuario>();
            
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT * FROM ""BibliotecaUsuario"" WHERE ""UsuarioId"" = @userId AND ""Estado"" = 'Jugando'";
                
                var parameter = command.CreateParameter();
                parameter.ParameterName = "userId";
                parameter.Value = usuario.Id;
                command.Parameters.Add(parameter);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        juegosJugando.Add(new BibliotecaUsuario
                        {
                            Id = reader.GetInt32(0),
                            UsuarioId = reader.GetString(1),
                            RawgGameId = reader.GetInt32(2),
                            Estado = reader.GetString(3),
                            GameName = reader.GetString(4),
                            GameImage = reader.GetString(5),
                            Resena = reader.GetString(6),
                            Calificacion = reader.GetInt32(7),
                            FechaCompletado = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                            FechaResena = reader.IsDBNull(9) ? null : reader.GetDateTime(9)
                        });
                    }
                }
            }
            
            await connection.CloseAsync();
            return View(juegosJugando);
        }

        // GET: Biblioteca/Completados - CON SQL DIRECTO
        public async Task<IActionResult> Completados()
        {
            if (!User.Identity.IsAuthenticated)
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

            // SQL DIRECTO
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();
            
            var juegosCompletados = new List<BibliotecaUsuario>();
            
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT * FROM ""BibliotecaUsuario"" WHERE ""UsuarioId"" = @userId AND ""Estado"" = 'Completado'";
                
                var parameter = command.CreateParameter();
                parameter.ParameterName = "userId";
                parameter.Value = usuario.Id;
                command.Parameters.Add(parameter);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        juegosCompletados.Add(new BibliotecaUsuario
                        {
                            Id = reader.GetInt32(0),
                            UsuarioId = reader.GetString(1),
                            RawgGameId = reader.GetInt32(2),
                            Estado = reader.GetString(3),
                            GameName = reader.GetString(4),
                            GameImage = reader.GetString(5),
                            Resena = reader.GetString(6),
                            Calificacion = reader.GetInt32(7),
                            FechaCompletado = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                            FechaResena = reader.IsDBNull(9) ? null : reader.GetDateTime(9)
                        });
                    }
                }
            }
            
            await connection.CloseAsync();
            return View(juegosCompletados);
        }

        // MarkAsPlaying - CON SQL DIRECTO
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

            // SQL DIRECTO
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();
            
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"UPDATE ""BibliotecaUsuario"" SET ""Estado"" = 'Jugando' WHERE ""UsuarioId"" = @userId AND ""RawgGameId"" = @gameId AND ""Estado"" = 'Pendiente'";
                
                command.Parameters.Add(new Npgsql.NpgsqlParameter("userId", usuario.Id));
                command.Parameters.Add(new Npgsql.NpgsqlParameter("gameId", id));
                
                var rowsAffected = await command.ExecuteNonQueryAsync();
                
                if (rowsAffected == 0)
                {
                    TempData["Error"] = "No se encontró el juego en Pendientes.";
                }
                else
                {
                    TempData["Ok"] = "¡Disfruta! Marcado como 'Jugando'.";
                }
            }
            
            await connection.CloseAsync();
            return RedirectToAction(nameof(Pendientes));
        }

        // MarkAsCompleted - CON SQL DIRECTO
        public async Task<IActionResult> MarkAsCompleted(int id)
        {
            if (!User.Identity.IsAuthenticated)
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

            // SQL DIRECTO
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();
            
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"UPDATE ""BibliotecaUsuario"" SET ""Estado"" = 'Completado', ""FechaCompletado"" = @fecha WHERE ""UsuarioId"" = @userId AND ""RawgGameId"" = @gameId AND ""Estado"" = 'Jugando'";
                
                command.Parameters.Add(new Npgsql.NpgsqlParameter("userId", usuario.Id));
                command.Parameters.Add(new Npgsql.NpgsqlParameter("gameId", id));
                command.Parameters.Add(new Npgsql.NpgsqlParameter("fecha", DateTime.UtcNow));
                
                var rowsAffected = await command.ExecuteNonQueryAsync();
                
                if (rowsAffected == 0)
                {
                    TempData["Error"] = "No se encontró el juego en Jugando.";
                }
                else
                {
                    var game = await _rawgService.GetGameDetailsAsync(id);
                    var gameName = game?.Name ?? "el juego";
                    TempData["Ok"] = $"¡Felicidades! {gameName} marcado como 'Completado'.";
                }
            }
            
            await connection.CloseAsync();
            return RedirectToAction(nameof(Completados));
        }

        // AddReview - CON SQL DIRECTO
        [HttpPost]
        public async Task<IActionResult> AddReview(int id, string resena, int calificacion)
        {
            if (!User.Identity.IsAuthenticated)
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
                // SQL DIRECTO
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();
                
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"UPDATE ""BibliotecaUsuario"" SET ""Resena"" = @resena, ""Calificacion"" = @calificacion, ""FechaResena"" = @fecha WHERE ""UsuarioId"" = @userId AND ""RawgGameId"" = @gameId AND ""Estado"" = 'Completado'";
                    
                    command.Parameters.Add(new Npgsql.NpgsqlParameter("userId", usuario.Id));
                    command.Parameters.Add(new Npgsql.NpgsqlParameter("gameId", id));
                    command.Parameters.Add(new Npgsql.NpgsqlParameter("resena", resena ?? ""));
                    command.Parameters.Add(new Npgsql.NpgsqlParameter("calificacion", calificacion));
                    command.Parameters.Add(new Npgsql.NpgsqlParameter("fecha", DateTime.UtcNow));
                    
                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    
                    if (rowsAffected == 0)
                    {
                        return Json(new { success = false, message = "Juego no encontrado." });
                    }
                }
                
                await connection.CloseAsync();
                return Json(new { success = true, message = "Reseña publicada correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al guardar la reseña." });
            }
        }

        // GET: Biblioteca/MiBiblioteca - CON SQL DIRECTO
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

            // SQL DIRECTO
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();
            
            var miBiblioteca = new List<BibliotecaUsuario>();
            
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"SELECT * FROM ""BibliotecaUsuario"" WHERE ""UsuarioId"" = @userId";
                
                var parameter = command.CreateParameter();
                parameter.ParameterName = "userId";
                parameter.Value = usuario.Id;
                command.Parameters.Add(parameter);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        miBiblioteca.Add(new BibliotecaUsuario
                        {
                            Id = reader.GetInt32(0),
                            UsuarioId = reader.GetString(1),
                            RawgGameId = reader.GetInt32(2),
                            Estado = reader.GetString(3),
                            GameName = reader.GetString(4),
                            GameImage = reader.GetString(5),
                            Resena = reader.GetString(6),
                            Calificacion = reader.GetInt32(7),
                            FechaCompletado = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                            FechaResena = reader.IsDBNull(9) ? null : reader.GetDateTime(9)
                        });
                    }
                }
            }
            
            await connection.CloseAsync();
            return View(miBiblioteca);
        }
    }
}