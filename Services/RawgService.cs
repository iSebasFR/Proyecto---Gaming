using System.Text.Json;
using System.Text.Json.Serialization;
using Proyecto_Gaming.Models.Rawg;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Threading;

namespace Proyecto_Gaming.Services
{
    public class RawgService : IRawgService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;
        private readonly IDistributedCache _redisCache;
        private readonly IMemoryCache _memoryCache;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public RawgService(HttpClient httpClient, IConfiguration configuration,
                         IDistributedCache redisCache, IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Rawg:ApiKey"];
            _redisCache = redisCache;
            _memoryCache = memoryCache;
        }

    public async Task<GameResponse> GetGamesAsync(string? search = null, string? genres = null, string? platform = null, int page = 1)
    {
        // ✅ CONFIGURACIÓN DE LÍMITES
        const int MAX_PAGES = 5;
        const int PAGE_SIZE = 20;
        
        // Validar límites
        if (page > MAX_PAGES)
        {
            Console.WriteLine($"⚠️ Página {page} excede el límite de {MAX_PAGES}, usando página {MAX_PAGES}");
            page = MAX_PAGES;
        }

        var cacheKey = $"Games_{search}_{genres}_{platform}_{page}";
        
        try
        {
            // ✅ VERIFICAR CACHÉ CON VALIDACIÓN ROBUSTA
            var cachedData = await _redisCache.GetStringAsync(cacheKey);
            
            if (!string.IsNullOrEmpty(cachedData))
            {
                try
                {
                    var cachedResponse = JsonSerializer.Deserialize<GameResponse>(cachedData);
                    
                    if (cachedResponse != null && cachedResponse.Results != null && cachedResponse.Results.Any())
                    {
                        // ✅ LIMITAR RESULTADOS EN CACHÉ TAMBIÉN
                        if (cachedResponse.Results.Count > PAGE_SIZE)
                        {
                            cachedResponse.Results = cachedResponse.Results.Take(PAGE_SIZE).ToList();
                        }
                        
                        Console.WriteLine($"✅ Cache HIT: {cacheKey} - {cachedResponse.Results.Count} juegos");
                        return cachedResponse;
                    }
                }
                catch (JsonException)
                {
                    await _redisCache.RemoveAsync(cacheKey);
                }
            }
        }
        catch (Exception cacheEx)
        {
            Console.WriteLine($"⚠️ Error accediendo a Redis: {cacheEx.Message}");
        }

        // ✅ CONSTRUIR URL CON LÍMITES
        var url = $"games?key={_apiKey}&page={page}&page_size={PAGE_SIZE}&ordering=-rating";
        
        if (!string.IsNullOrEmpty(search))
            url += $"&search={Uri.EscapeDataString(search)}";
        
        if (!string.IsNullOrEmpty(genres))
            url += $"&genres={genres}";
        
        if (!string.IsNullOrEmpty(platform))
            url += $"&platforms={platform}";

        Console.WriteLine($"🔗 Llamando RAWG API: {url}");

        try
        {
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Error RAWG API: {response.StatusCode}");
                return CreateEmptyResponse();
            }
            
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"✅ Respuesta RAWG - Length: {content.Length}");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var gameResponse = JsonSerializer.Deserialize<GameResponse>(content, options);
            
            // ✅ LIMITAR RESULTADOS DIRECTAMENTE
            if (gameResponse?.Results?.Count > PAGE_SIZE)
            {
                Console.WriteLine($"✂️ Limitando resultados de {gameResponse.Results.Count} a {PAGE_SIZE}");
                gameResponse.Results = gameResponse.Results.Take(PAGE_SIZE).ToList();
            }

            Console.WriteLine($"🔍 Deserializado - Count: {gameResponse?.Count}, Results: {gameResponse?.Results?.Count}");

            // ✅ GUARDAR EN CACHÉ SOLO SI ES VÁLIDO
            if (gameResponse?.Results?.Any() == true)
            {
                try
                {
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
                    };
                    
                    await _redisCache.SetStringAsync(cacheKey, content, cacheOptions);
                    Console.WriteLine($"💾 Cache guardado: {cacheKey}");
                }
                catch (Exception saveEx)
                {
                    Console.WriteLine($"⚠️ Error guardando en cache: {saveEx.Message}");
                }
            }

            return gameResponse ?? CreateEmptyResponse();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Excepción en RAWG API: {ex.Message}");
            return CreateEmptyResponse();
        }
    }

    // ✅ MÉTODOS AUXILIARES PARA AUTO-RECUPERACIÓN
    private async Task<GameResponse?> TryGetValidCacheAsync(string cacheKey)
    {
        try
        {
            var cachedData = await _redisCache.GetStringAsync(cacheKey);
            
            if (string.IsNullOrEmpty(cachedData))
            {
                return null; // No hay caché, proceder con API
            }

            var cachedResponse = JsonSerializer.Deserialize<GameResponse>(cachedData);
            
            // ✅ VALIDACIÓN ROBUSTA: debe tener datos reales
            if (cachedResponse != null && 
                cachedResponse.Results != null && 
                cachedResponse.Results.Any() &&
                cachedResponse.Count > 0)
            {
                Console.WriteLine($"✅ Cache VÁLIDO: {cacheKey} - {cachedResponse.Results.Count} juegos");
                return cachedResponse;
            }
            else
            {
                Console.WriteLine($"🔄 Cache INVÁLIDO - auto-limpiando: {cacheKey}");
                await _redisCache.RemoveAsync(cacheKey);
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error validando cache - continuando sin cache: {ex.Message}");
            return null; // Si hay error, proceder con API
        }
    }

    private async Task SaveToCacheAsync(string cacheKey, string content)
    {
        try
        {
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
            };
            
            await _redisCache.SetStringAsync(cacheKey, content, cacheOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ No se pudo guardar en cache (continuando sin cache): {ex.Message}");
            // NO throw - la aplicación puede continuar sin cache
        }
    }

    private string BuildRawgUrl(string? search, string? genres, string? platform, int page)
    {
        var url = $"games?key={_apiKey}&page={page}&page_size=20&ordering=-rating";
        
        if (!string.IsNullOrEmpty(search))
            url += $"&search={Uri.EscapeDataString(search)}";
        
        if (!string.IsNullOrEmpty(genres))
            url += $"&genres={genres}";
        
        if (!string.IsNullOrEmpty(platform))
            url += $"&platforms={platform}";

        return url;
    }

    private GameResponse DeserializeGameResponse(string content)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };
        
        return JsonSerializer.Deserialize<GameResponse>(content, options);
    }

    private GameResponse CreateEmptyResponse()
    {
        return new GameResponse { 
            Results = new List<Game>(), 
            Count = 0, 
            Next = null, 
            Previous = null 
        };
    }   
        public async Task<Game> GetGameDetailsAsync(int id)
        {
            // ✅ CACHE EN REDIS
            var cacheKey = $"game_details_{id}";
            var cachedData = await _redisCache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<Game>(cachedData) ?? new Game();
            }

            try
            {
                var response = await _httpClient.GetAsync($"games/{id}?key={_apiKey}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var gameDetails = JsonSerializer.Deserialize<Game>(content);

                // ✅ GUARDAR EN REDIS (1 hora - datos más estables)
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                };
                await _redisCache.SetStringAsync(cacheKey, content, cacheOptions);

                return gameDetails ?? new Game();
            }
            catch (Exception)
            {
                return new Game();
            }
        }
        
        public async Task<Game> GetGameExtendedDetailsAsync(int id)
        {
            try
            {
                // Obtener detalles básicos del juego
                var game = await GetGameDetailsAsync(id);
                if (game == null) return new Game();

                // Obtener datos extendidos en paralelo para mejor performance
                var screenshotsTask = GetGameScreenshotsAsync(id);
                var trailersTask = GetGameTrailersAsync(id);
                var requirementsTask = GetGameRequirementsAsync(id);
                

                await Task.WhenAll(screenshotsTask, trailersTask, requirementsTask);

                // Asignar los datos extendidos al juego
                game.Screenshots = await screenshotsTask;
                game.Trailers = await trailersTask;
                game.Requirements = await requirementsTask;

                // Los precios los manejaremos separadamente en el ViewModel

                return game;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener detalles extendidos: {ex.Message}");
                return await GetGameDetailsAsync(id); // Fallback a detalles básicos
            }
        }
    


        public async Task<List<GameScreenshot>> GetGameScreenshotsAsync(int id)
        {
            var cacheKey = $"screenshots_{id}";
            
            // Intentar obtener de caché primero
            var cachedData = await _redisCache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<List<GameScreenshot>>(cachedData) ?? new List<GameScreenshot>();
            }

            try
            {
                var response = await _httpClient.GetAsync($"games/{id}/screenshots?key={_apiKey}");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var screenshotResponse = JsonSerializer.Deserialize<ScreenshotResponse>(content);
                
                var screenshots = screenshotResponse?.Results ?? new List<GameScreenshot>();

                // Guardar en caché por 1 hora
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                };
                await _redisCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(screenshots), cacheOptions);

                return screenshots;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener screenshots: {ex.Message}");
                return new List<GameScreenshot>();
            }
        }

        public async Task<List<GameTrailer>> GetGameTrailersAsync(int id)
        {
            var cacheKey = $"trailers_{id}";
            
            // Intentar obtener de caché primero
            var cachedData = await _redisCache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<List<GameTrailer>>(cachedData) ?? new List<GameTrailer>();
            }

            try
            {
                var response = await _httpClient.GetAsync($"games/{id}/movies?key={_apiKey}");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var trailerResponse = JsonSerializer.Deserialize<TrailerResponse>(content);
                
                var trailers = trailerResponse?.Results ?? new List<GameTrailer>();

                // Guardar en caché por 2 horas (los tráilers cambian menos)
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
                };
                await _redisCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(trailers), cacheOptions);

                return trailers;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener trailers: {ex.Message}");
                return new List<GameTrailer>();
            }
        }

        public async Task<SystemRequirements> GetGameRequirementsAsync(int id)
        {
            var cacheKey = $"requirements_{id}";
            
            // Intentar obtener de caché primero
            var cachedData = await _redisCache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<SystemRequirements>(cachedData) ?? new SystemRequirements();
            }

            try
            {
                // Para requisitos necesitamos obtener la plataforma PC específicamente
                var response = await _httpClient.GetAsync($"games/{id}?key={_apiKey}");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var gameDetails = JsonSerializer.Deserialize<Game>(content);
                
                // Los requisitos vienen en la respuesta principal para PC
                // En una implementación real, buscaríamos en platforms[].requirements
                var requirements = new SystemRequirements();

                // Guardar en caché por 1 hora
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                };
                await _redisCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(requirements), cacheOptions);

                return requirements;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener requisitos: {ex.Message}");
                return new SystemRequirements();
            }
        }


        public async Task<List<Genre>> GetGenresAsync()
        {
            // ✅ CACHE EN REDIS (24 horas - datos muy estables)
            var cacheKey = "rawg_genres";
            var cachedData = await _redisCache.GetStringAsync(cacheKey);
            
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<List<Genre>>(cachedData) ?? new List<Genre>();
            }

            try
            {
                var response = await _httpClient.GetAsync($"genres?key={_apiKey}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var genresResponse = JsonSerializer.Deserialize<GenreResponse>(content);
                var genres = genresResponse?.Results ?? new List<Genre>();

                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                };
                await _redisCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(genres), cacheOptions);

                return genres;
            }
            catch (Exception)
            {
                return new List<Genre>();
            }
        }

        public async Task<List<Platform>> GetPlatformsAsync()
        {
            // ✅ CACHE EN REDIS (24 horas)
            var cacheKey = "rawg_platforms";
            var cachedData = await _redisCache.GetStringAsync(cacheKey);
            
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<List<Platform>>(cachedData) ?? new List<Platform>();
            }

            try
            {
                var response = await _httpClient.GetAsync($"platforms?key={_apiKey}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var platformsResponse = JsonSerializer.Deserialize<PlatformResponse>(content);
                var platforms = platformsResponse?.Results ?? new List<Platform>();

                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                };
                await _redisCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(platforms), cacheOptions);

                return platforms;
            }
            catch (Exception)
            {
                return new List<Platform>();
            }
        }

        public async Task<FilterData> GetAvailableFiltersAsync()
        {
            const string cacheKey = "AvailableFilters";
            
            // ✅ 1. INTENTAR CACHÉ PRIMERO
            try
            {
                var cachedData = await _redisCache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    var filters = JsonSerializer.Deserialize<FilterData>(cachedData);
                    if (IsValidFilterData(filters))
                    {
                        Console.WriteLine($"✅ Filtros desde cache válidos");
                        return filters!;
                    }
                    else
                    {
                        Console.WriteLine($"🔄 Filtros en cache inválidos - auto-limpiando");
                        await _redisCache.RemoveAsync(cacheKey);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error leyendo filtros de cache: {ex.Message}");
            }

            // ✅ 2. CARGAR DESDE API SI CACHÉ FALLÓ
            Console.WriteLine("🔄 Cargando filtros desde API (auto-recuperación)");
            
            var filterData = new FilterData();
            
            try
            {
                var genresTask = GetGenresAsync();
                var platformsTask = GetPlatformsAsync();
                
                await Task.WhenAll(genresTask, platformsTask);
                
                filterData.AvailableGenres = genresTask.Result ?? new List<Genre>();
                filterData.AvailablePlatforms = platformsTask.Result ?? new List<Platform>();
                
                // ✅ 3. GUARDAR EN CACHÉ SI SON VÁLIDOS
                if (IsValidFilterData(filterData))
                {
                    await SaveFiltersToCacheAsync(cacheKey, filterData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error cargando filtros: {ex.Message}");
                // Devolver datos vacíos pero no fallar
            }

            return filterData;
        }

        private bool IsValidFilterData(FilterData? filters)
        {
            return filters != null && 
                filters.AvailableGenres?.Any() == true && 
                filters.AvailablePlatforms?.Any() == true;
        }

        private async Task SaveFiltersToCacheAsync(string cacheKey, FilterData filterData)
        {
            try
            {
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                };
                await _redisCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(filterData), cacheOptions);
                Console.WriteLine("💾 Filtros guardados en cache automáticamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ No se pudieron guardar filtros en cache: {ex.Message}");
            }
        }

        public async Task PreloadFirst100GamesAsync()
    {
        await Task.Run(async () =>
        {
            try
            {
                const int MAX_PAGES_TO_PRELOAD = 3; // ✅ Solo precargar 3 páginas (60 juegos)
                const int GAMES_PER_PAGE = 20;
                
                var allGames = new List<Game>();
                Console.WriteLine($"🔄 Precargando máximo {MAX_PAGES_TO_PRELOAD * GAMES_PER_PAGE} juegos...");

                for (int page = 1; page <= MAX_PAGES_TO_PRELOAD; page++)
                {
                    var response = await GetGamesAsync(null, null, null, page);
                    if (response?.Results != null)
                    {
                        allGames.AddRange(response.Results);
                        Console.WriteLine($"📥 Página {page}: {response.Results.Count} juegos");
                        
                        // Pequeña pausa para no saturar la API
                        await Task.Delay(500);
                    }
                    
                    // Si no hay más juegos, salir
                    if (string.IsNullOrEmpty(response?.Next))
                        break;
                }

                // ✅ CACHEAR SOLO SI HAY JUEGOS
                if (allGames.Any())
                {
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                    };
                    
                    // Guardar solo los IDs para ahorrar espacio
                    var gameIds = allGames.Select(g => g.Id).ToList();
                    await _redisCache.SetStringAsync("PreloadedGameIds", 
                        JsonSerializer.Serialize(gameIds), cacheOptions);
                    
                    Console.WriteLine($"✅ Precargados {allGames.Count} juegos en Redis (solo IDs)");
                }
                else
                {
                    Console.WriteLine("⚠️ No se pudieron precargar juegos");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en precarga: {ex.Message}");
            }
        });
    }

        // ✅ NUEVO: Tracking para ML
        public async Task TrackUserSearchAsync(string userId, string searchTerm, string genre, string platform)
        {
            try
            {
                var searchData = new
                {
                    UserId = userId,
                    SearchTerm = searchTerm,
                    Genre = genre,
                    Platform = platform,
                    Timestamp = DateTime.UtcNow
                };

                // Guardar en Redis para ML (30 días de historial)
                var cacheKey = $"user_search_{userId}_{DateTime.UtcNow:yyyyMMdd}";
                var existingSearches = await _redisCache.GetStringAsync(cacheKey);
                var searches = string.IsNullOrEmpty(existingSearches) 
                    ? new List<object>() 
                    : JsonSerializer.Deserialize<List<object>>(existingSearches) ?? new List<object>();

                searches.Add(searchData);

                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
                };
                await _redisCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(searches), cacheOptions);

                Console.WriteLine($"Búsqueda trackeada para ML: {searchTerm}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error tracking search: {ex.Message}");
            }
        }
    }

    public class GenreResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("results")]
        public List<Genre>? Results { get; set; }
    }

    public class PlatformResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("results")]
        public List<Platform>? Results { get; set; }
    }
      public class ScreenshotResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }
        
        [JsonPropertyName("results")]
        public List<GameScreenshot> Results { get; set; } = new List<GameScreenshot>();
    }

public class TrailerResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("results")]
    public List<GameTrailer> Results { get; set; } = new List<GameTrailer>();
}
}
