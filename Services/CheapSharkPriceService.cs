// NUEVO ARCHIVO: Proyecto_Gaming/Services/CheapSharkPriceService.cs
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json.Serialization;
using Proyecto_Gaming.ViewModels;
namespace Proyecto_Gaming.Services
{
    public interface IGamePriceService
    {
        Task<decimal?> GetGamePriceAsync(string gameName);
        Task<Dictionary<string, decimal?>> GetBulkPricesAsync(List<string> gameNames);
        Task<List<StoreOffer>> GetGameDealsAsync(string gameName);
    }

    public class CheapSharkPriceService : IGamePriceService
    {
        private readonly HttpClient _httpClient;
        private readonly IDistributedCache _cache;
        private readonly ILogger<CheapSharkPriceService> _logger;
        
         // ✅ CONFIGURACIÓN DE LÍMITES
        private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(4);
        private readonly TimeSpan _requestTimeout = TimeSpan.FromSeconds(10);


        public CheapSharkPriceService(HttpClient httpClient, IDistributedCache cache, ILogger<CheapSharkPriceService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
             _httpClient.BaseAddress = new Uri("https://www.cheapshark.com/api/1.0/");
            _httpClient.Timeout = _requestTimeout;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "GamingApp/1.0");
        }

        public async Task<decimal?> GetGamePriceAsync(string gameName)
        {
            if (string.IsNullOrEmpty(gameName))
                return null;

            var cacheKey = $"price_{gameName.ToLower().Replace(" ", "_")}";
            
            try
            {
                // ✅ CACHE PRIMERO
                var cachedPrice = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedPrice) && decimal.TryParse(cachedPrice, out var price))
                {
                    return price;
                }

                // ✅ LIMPIAR NOMBRE PARA MEJOR BÚSQUEDA
                var cleanName = CleanGameName(gameName);
                
                // ✅ BUSCAR EN CHEAPSHARK CON TIMEOUT
                using var cts = new CancellationTokenSource(_requestTimeout);
                var response = await _httpClient.GetAsync($"games?title={Uri.EscapeDataString(cleanName)}&limit=1", cts.Token);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var games = JsonSerializer.Deserialize<List<CheapSharkGame>>(content);
                    
                    var game = games?.FirstOrDefault();
                    if (game != null)
                    {
                        // ✅ OBTENER MEJOR PRECIO
                        var bestPrice = await GetBestCurrentPrice(game.CheapestDealID);
                        if (bestPrice.HasValue)
                        {
                            // ✅ GUARDAR EN CACHE
                            await _cache.SetStringAsync(cacheKey, bestPrice.Value.ToString(), 
                                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _cacheDuration });
                            
                            return bestPrice.Value;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Timeout obteniendo precio para {GameName}", gameName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error obteniendo precio para {GameName}", gameName);
            }

            return null;
        }

        public async Task<Dictionary<string, decimal?>> GetBulkPricesAsync(List<string> gameNames)
        {
            var prices = new Dictionary<string, decimal?>();
            
            // ✅ LIMITAR A 50 JUEGOS MÁXIMO
            var limitedNames = gameNames.Take(50).ToList();
            
            var tasks = limitedNames.Select(async gameName =>
            {
                var price = await GetGamePriceAsync(gameName);
                prices[gameName] = price;
            });

            await Task.WhenAll(tasks);
            return prices;
        }

            public async Task<List<StoreOffer>> GetGameDealsAsync(string gameName)
    {
        try
        {
            // ✅ LIMITAR RESULTADOS
            var cleanName = CleanGameName(gameName);
            var response = await _httpClient.GetAsync($"games?title={Uri.EscapeDataString(cleanName)}&limit=3");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var games = JsonSerializer.Deserialize<List<CheapSharkGame>>(content);
                var deals = new List<StoreOffer>();
                
                foreach (var game in games ?? new List<CheapSharkGame>())
                {
                    // ✅ LIMITAR OFERTAS POR JUEGO
                    foreach (var deal in game.Deals?.Take(3) ?? new List<DealInfo>())
                    {
                        var storeName = await GetStoreName(deal.StoreID);
                        deals.Add(new StoreOffer
                        {
                            StoreName = storeName,
                            Price = decimal.Parse(deal.Price),
                            RetailPrice = decimal.Parse(deal.RetailPrice),
                            Savings = deal.Savings,
                            DealUrl = $"https://www.cheapshark.com/redirect?dealID={deal.DealID}"
                        });
                    }
                }
                
                return deals.OrderBy(d => d.Price).Take(5).ToList(); // ✅ MÁXIMO 5 OFERTAS
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error obteniendo ofertas para {GameName}", gameName);
        }

        return new List<StoreOffer>();
    }

        private async Task<decimal?> GetBestCurrentPrice(string cheapestDealId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"deals?id={cheapestDealId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var deal = JsonSerializer.Deserialize<CheapSharkDeal>(content);

                    if (deal != null && decimal.TryParse(deal.GameInfo.SalePrice, out var price))
                    {
                        return price;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al obtener detalles del deal {DealId}", cheapestDealId);
            }

            return null;
        }

            // ✅ MÉTODO PARA LIMPIAR NOMBRES DE JUEGOS
    private string CleanGameName(string gameName)
    {
        if (string.IsNullOrEmpty(gameName)) return "";
        
        // Remover ediciones especiales, años, etc. para mejor búsqueda
        var clean = gameName
            .Replace("®", "")
            .Replace("™", "")
            .Replace(":", "")
            .Replace("-", " ")
            .Replace("'", "");
            
        // Limitar longitud para API
        return clean.Length > 50 ? clean.Substring(0, 50) : clean;
    }

        


        private async Task<string> GetStoreName(string storeId)
        {
            var cacheKey = $"store_{storeId}";
            var cachedName = await _cache.GetStringAsync(cacheKey);
            
            if (!string.IsNullOrEmpty(cachedName))
                return cachedName;

            try
            {
                var response = await _httpClient.GetAsync("stores");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var stores = JsonSerializer.Deserialize<List<CheapSharkStore>>(content);
                    
                    var store = stores?.FirstOrDefault(s => s.StoreID == storeId);
                    if (store != null)
                    {
                        await _cache.SetStringAsync(cacheKey, store.StoreName, 
                            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7) });
                        return store.StoreName;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al obtener nombre de tienda {StoreId}", storeId);
            }

            return "Tienda Desconocida";
        }
    }

    // MODELOS PARA CHEAPSHARK API
    public class CheapSharkGame
    {
        [JsonPropertyName("gameID")]
        public string GameID { get; set; }

        [JsonPropertyName("steamAppID")]
        public string SteamAppID { get; set; }

        [JsonPropertyName("cheapest")]
        public string Cheapest { get; set; }

        [JsonPropertyName("cheapestDealID")]
        public string CheapestDealID { get; set; }

        [JsonPropertyName("external")]
        public string External { get; set; }

        [JsonPropertyName("internalName")]
        public string InternalName { get; set; }

        [JsonPropertyName("thumb")]
        public string Thumb { get; set; }

        [JsonPropertyName("deals")]
        public List<DealInfo> Deals { get; set; } = new List<DealInfo>();
    }

    public class DealInfo
    {
        [JsonPropertyName("storeID")]
        public string StoreID { get; set; }

        [JsonPropertyName("dealID")]
        public string DealID { get; set; }

        [JsonPropertyName("price")]
        public string Price { get; set; }

        [JsonPropertyName("retailPrice")]
        public string RetailPrice { get; set; }

        [JsonPropertyName("savings")]
        public string Savings { get; set; }
    }

    public class CheapSharkDeal
    {
        [JsonPropertyName("gameInfo")]
        public GameInfo GameInfo { get; set; }
    }

    public class GameInfo
    {
        [JsonPropertyName("storeID")]
        public string StoreID { get; set; }

        [JsonPropertyName("gameID")]
        public string GameID { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("steamAppID")]
        public string SteamAppID { get; set; }

        [JsonPropertyName("salePrice")]
        public string SalePrice { get; set; }

        [JsonPropertyName("retailPrice")]
        public string RetailPrice { get; set; }

        [JsonPropertyName("steamRatingText")]
        public string SteamRatingText { get; set; }

        [JsonPropertyName("steamRatingPercent")]
        public string SteamRatingPercent { get; set; }

        [JsonPropertyName("steamRatingCount")]
        public string SteamRatingCount { get; set; }

        [JsonPropertyName("metacriticScore")]
        public string MetacriticScore { get; set; }

        [JsonPropertyName("metacriticLink")]
        public string MetacriticLink { get; set; }

        [JsonPropertyName("releaseDate")]
        public int ReleaseDate { get; set; }

        [JsonPropertyName("publisher")]
        public string Publisher { get; set; }

        [JsonPropertyName("steamworks")]
        public string Steamworks { get; set; }

        [JsonPropertyName("thumb")]
        public string Thumb { get; set; }
    }

    public class CheapSharkStore
    {
        [JsonPropertyName("storeID")]
        public string StoreID { get; set; }

        [JsonPropertyName("storeName")]
        public string StoreName { get; set; }

        [JsonPropertyName("isActive")]
        public int IsActive { get; set; }
    }


}