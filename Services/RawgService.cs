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
            _httpClient.BaseAddress = new Uri("https://api.rawg.io/api/");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<GameResponse> GetGamesAsync(string? search = null, string? genres = null, string? platforms = null, int page = 1)
        {
            // ✅ CACHE EN REDIS
            var cacheKey = $"games_{search}_{genres}_{platforms}_{page}";
            var cachedData = await _redisCache.GetStringAsync(cacheKey);
            
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<GameResponse>(cachedData) ?? new GameResponse { Results = new List<Game>(), Count = 0, Next = null, Previous = null };
            }

            var currentYear = DateTime.Now.Year;
            var startYear = currentYear - 3;
            
            if (page > 5) 
            {
                return new GameResponse { 
                    Results = new List<Game>(),
                    Count = 0,
                    Next = null,
                    Previous = null
                };
            }
            
            var url = $"games?key={_apiKey}&page={page}&page_size=20&dates={startYear}-01-01,{currentYear}-12-31&ordering=-rating";
            
            if (!string.IsNullOrEmpty(search))
                url += $"&search={Uri.EscapeDataString(search)}";
            if (!string.IsNullOrEmpty(genres))
                url += $"&genres={genres}";
            if (!string.IsNullOrEmpty(platforms))
                url += $"&platforms={platforms}";

            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var gamesResponse = JsonSerializer.Deserialize<GameResponse>(content);

                // ✅ GUARDAR EN REDIS (30 minutos)
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                };
                await _redisCache.SetStringAsync(cacheKey, content, cacheOptions);

                return gamesResponse ?? new GameResponse { Results = new List<Game>(), Count = 0, Next = null, Previous = null };
            }
            catch (Exception)
            {
                return new GameResponse { 
                    Results = new List<Game>(),
                    Count = 0,
                    Next = null,
                    Previous = null
                };
            }
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
            
            // ✅ PRIMERO REDIS
            var cachedData = await _redisCache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
                return JsonSerializer.Deserialize<FilterData>(cachedData) ?? new FilterData();

            // ✅ LUEGO MEMORY CACHE (doble capa)
            if (_memoryCache.TryGetValue(cacheKey, out FilterData? cachedFilters) && cachedFilters != null)
                return cachedFilters ?? new FilterData();

            await _semaphore.WaitAsync();
            try
            {
                // Doble verificación después de adquirir el semáforo
                cachedData = await _redisCache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                    return JsonSerializer.Deserialize<FilterData>(cachedData) ?? new FilterData();

                if (_memoryCache.TryGetValue(cacheKey, out cachedFilters))
                    return cachedFilters ?? new FilterData();

                var gamesResponse = await GetGamesAsync(null, null, null, 1);
                var games = gamesResponse.Results;

                if (!games.Any())
                    return new FilterData();

                var filterData = new FilterData();

                var uniqueGenreSlugs = games
                    .SelectMany(g => g.Genres)
                    .Where(g => g != null && !string.IsNullOrEmpty(g.Slug))
                    .Select(g => g.Slug)
                    .Distinct()
                    .ToList();

                var uniquePlatformIds = games
                    .SelectMany(g => g.Platforms)
                    .Where(p => p?.Platform != null)
                    .Select(p => p.Platform != null ? p.Platform.Id : 0)
                    .Where(id => id != 0)
                    .Distinct()
                    .ToList();

                var allGenres = await GetGenresAsync();
                var allPlatforms = await GetPlatformsAsync();

                filterData.AvailableGenres = allGenres
                    .Where(g => uniqueGenreSlugs.Contains(g.Slug))
                    .OrderBy(g => g.Name)
                    .ToList();

                filterData.AvailablePlatforms = allPlatforms
                    .Where(p => uniquePlatformIds.Contains(p.Id))
                    .OrderBy(p => p.Name)
                    .ToList();

                // ✅ CACHE EN AMBOS: REDIS Y MEMORY
                var redisCacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                };
                await _redisCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(filterData), redisCacheOptions);

                _memoryCache.Set(cacheKey, filterData, TimeSpan.FromHours(1));
                
                return filterData;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task PreloadFirst100GamesAsync()
        {
            await Task.Run(async () =>
            {
                try
                {
                    var allGames = new List<Game>();
                    for (int page = 1; page <= 5; page++)
                    {
                        var response = await GetGamesAsync(null, null, null, page);
                        if (response?.Results != null)
                            allGames.AddRange(response.Results);
                    }
                    
                    // ✅ CACHE EN REDIS
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                    };
                    await _redisCache.SetStringAsync("PreloadedGames", JsonSerializer.Serialize(allGames), cacheOptions);
                    
                    Console.WriteLine($"Precargados {allGames.Count} juegos en Redis");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en precarga: {ex.Message}");
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
}