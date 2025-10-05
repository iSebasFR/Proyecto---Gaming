using System.Text.Json;
using System.Text.Json.Serialization;
using Proyecto_Gaming.Models.Rawg;
using Microsoft.Extensions.Caching.Memory;
using System.Threading;

namespace Proyecto_Gaming.Services
{
    public class RawgService : IRawgService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly IMemoryCache _cache;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public RawgService(HttpClient httpClient, IConfiguration configuration, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Rawg:ApiKey"];
            _cache = cache;
            _httpClient.BaseAddress = new Uri("https://api.rawg.io/api/");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<GameResponse> GetGamesAsync(string search = null, string genres = null, string platforms = null, int page = 1)
        {
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
                return JsonSerializer.Deserialize<GameResponse>(content);
            }
            catch (Exception ex)
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
            try
            {
                var response = await _httpClient.GetAsync($"games/{id}?key={_apiKey}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Game>(content);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<List<Genre>> GetGenresAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"genres?key={_apiKey}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var genresResponse = JsonSerializer.Deserialize<GenreResponse>(content);
                return genresResponse?.Results ?? new List<Genre>();
            }
            catch (Exception ex)
            {
                return new List<Genre>();
            }
        }

        public async Task<List<Platform>> GetPlatformsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"platforms?key={_apiKey}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var platformsResponse = JsonSerializer.Deserialize<PlatformResponse>(content);
                return platformsResponse?.Results ?? new List<Platform>();
            }
            catch (Exception ex)
            {
                return new List<Platform>();
            }
        }

        // MÉTODO OPTIMIZADO - Solo obtiene 20 juegos para filtros
        public async Task<FilterData> GetAvailableFiltersAsync()
        {
            const string cacheKey = "AvailableFilters";
            
            if (_cache.TryGetValue(cacheKey, out FilterData cachedFilters))
                return cachedFilters;

            await _semaphore.WaitAsync();
            try
            {
                // Doble verificación después de adquirir el semáforo
                if (_cache.TryGetValue(cacheKey, out cachedFilters))
                    return cachedFilters;

                // Solo obtener una página de juegos para extraer filtros
                var gamesResponse = await GetGamesAsync(null, null, null, 1);
                var games = gamesResponse.Results;

                if (!games.Any())
                    return new FilterData();

                var filterData = new FilterData();

                // Extraer géneros únicos
                var uniqueGenreSlugs = games
                    .SelectMany(g => g.Genres)
                    .Where(g => g != null && !string.IsNullOrEmpty(g.Slug))
                    .Select(g => g.Slug)
                    .Distinct()
                    .ToList();

                // Extraer plataformas únicas
                var uniquePlatformIds = games
                    .SelectMany(g => g.Platforms)
                    .Where(p => p?.Platform != null)
                    .Select(p => p.Platform.Id)
                    .Distinct()
                    .ToList();

                // Obtener géneros y plataformas (con caché individual)
                var allGenres = await GetCachedGenresAsync();
                var allPlatforms = await GetCachedPlatformsAsync();

                filterData.AvailableGenres = allGenres
                    .Where(g => uniqueGenreSlugs.Contains(g.Slug))
                    .OrderBy(g => g.Name)
                    .ToList();

                filterData.AvailablePlatforms = allPlatforms
                    .Where(p => uniquePlatformIds.Contains(p.Id))
                    .OrderBy(p => p.Name)
                    .ToList();

                // Cachear por 1 hora
                _cache.Set(cacheKey, filterData, TimeSpan.FromHours(1));
                
                return filterData;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        // MÉTODOS CON CACHÉ PARA GÉNEROS Y PLATAFORMAS
        private async Task<List<Genre>> GetCachedGenresAsync()
        {
            const string cacheKey = "RawgGenres";
            
            if (_cache.TryGetValue(cacheKey, out List<Genre> cachedGenres))
                return cachedGenres;

            var genres = await GetGenresAsync();
            _cache.Set(cacheKey, genres, TimeSpan.FromHours(24));
            return genres;
        }

        private async Task<List<Platform>> GetCachedPlatformsAsync()
        {
            const string cacheKey = "RawgPlatforms";
            
            if (_cache.TryGetValue(cacheKey, out List<Platform> cachedPlatforms))
                return cachedPlatforms;

            var platforms = await GetPlatformsAsync();
            _cache.Set(cacheKey, platforms, TimeSpan.FromHours(24));
            return platforms;
        }

        // MÉTODO PARA PRECARGA EN SEGUNDO PLANO (opcional)
        public async Task PreloadFirst100GamesAsync()
        {
            // Esto se ejecuta en segundo plano
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
                    
                    // Cachear los juegos precargados
                    _cache.Set("PreloadedGames", allGames, TimeSpan.FromMinutes(30));
                    Console.WriteLine($"Precargados {allGames.Count} juegos");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en precarga: {ex.Message}");
                }
            });
        }
    }

    public class GenreResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("results")]
        public List<Genre> Results { get; set; }
    }

    public class PlatformResponse
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("results")]
        public List<Platform> Results { get; set; }
    }
}