using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Proyecto_Gaming.Services
{
    public class CacheMaintenanceService : BackgroundService
    {
        private readonly IDistributedCache _redisCache;
        private readonly ILogger<CacheMaintenanceService> _logger;
        private readonly TimeSpan _maintenanceInterval = TimeSpan.FromHours(6);

        public CacheMaintenanceService(IDistributedCache redisCache, ILogger<CacheMaintenanceService> logger)
        {
            _redisCache = redisCache;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🛠️ Servicio de mantenimiento de caché iniciado");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformMaintenanceAsync();
                    await Task.Delay(_maintenanceInterval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Error en mantenimiento de caché");
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // shutdown requested - swallow and exit loop gracefully
                        break;
                    }
                }
            }
        }

        private async Task PerformMaintenanceAsync()
        {
            _logger.LogInformation("🔧 Ejecutando mantenimiento automático de caché...");
            
            try
            {
                // Verificar salud de Redis
                var testKey = "health_check_" + DateTime.Now.ToString("yyyyMMdd_HHmm");
                await _redisCache.SetStringAsync(testKey, "healthy", new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });
                
                var health = await _redisCache.GetStringAsync(testKey);
                
                if (health == "healthy")
                {
                    _logger.LogInformation("✅ Redis saludable - mantenimiento completado");
                }
                else
                {
                    _logger.LogWarning("⚠️ Redis no responde correctamente");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en verificación de salud de Redis");
            }
        }
    }
}