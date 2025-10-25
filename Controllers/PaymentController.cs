using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Proyecto_Gaming.Services;
using Proyecto_Gaming.Data;
using Proyecto_Gaming.Models;
using Proyecto_Gaming.Models.Payment;
using Proyecto_Gaming.Models.Rawg;

namespace Proyecto_Gaming.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly IRawgService _rawgService;
         private readonly IGamePriceService _priceService;

        public PaymentController(
            IPaymentService paymentService, 
            ILogger<PaymentController> logger,
            ApplicationDbContext context,
            UserManager<Usuario> userManager,
            IRawgService rawgService,
            IGamePriceService priceService)
        {
            _paymentService = paymentService;
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _rawgService = rawgService;
            _priceService = priceService;
        }

[HttpPost]
public async Task<IActionResult> Checkout(int gameId, decimal price, string gameTitle)
{
    try
    {
        Console.WriteLine($"💰 CHECKOUT INICIADO - GameId: {gameId}, Price: {price}, Title: {gameTitle}");
        
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            TempData["ErrorMessage"] = "Debes iniciar sesión para realizar una compra";
            return RedirectToAction("Login", "Account");
        }

        // ✅ USAR EL PRECIO QUE VIENE DEL FORMULARIO (de CheapShark)
        // En lugar de calcular precio dinámico, usar el que ya tenemos
        var actualPrice = price;
        
        Console.WriteLine($"💰 Usando precio de CheapShark: {actualPrice}");

        var successUrl = Url.Action("Success", "Payment", null, Request.Scheme);
        var cancelUrl = Url.Action("Cancel", "Payment", null, Request.Scheme);
        
        var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(
            gameId, userId, actualPrice, gameTitle, successUrl, cancelUrl);
            
        return Redirect(checkoutUrl);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error al iniciar checkout");
        TempData["ErrorMessage"] = "Error al procesar la compra. Inténtalo de nuevo.";
        return RedirectToAction("Index", "Biblioteca");
    }
}

        // ✅ MÉTODO PARA PRECIOS DINÁMICOS MEJORADO
        private async Task<decimal> GetDynamicPriceAsync(int gameId, string gameTitle, decimal requestedPrice)
        {
            // Si ya viene un precio específico, usarlo
            if (requestedPrice > 0)
            {
                Console.WriteLine($"💰 Usando precio solicitado: {requestedPrice}");
                return requestedPrice;
            }

            try
            {
                // Obtener detalles del juego desde RAWG
                var gameDetails = await _rawgService.GetGameDetailsAsync(gameId);
                
                if (gameDetails != null)
                {
                    Console.WriteLine($"🎮 Datos del juego - Rating: {gameDetails.Rating}, Released: {gameDetails.Released}, Genres: {gameDetails.Genres?.Count ?? 0}");

                    // ✅ ESTRATEGIA 1: Basado en RATING
                    var priceByRating = CalculatePriceByRating(gameDetails.Rating, gameDetails.Genres);
                    
                    // ✅ ESTRATEGIA 2: Basado en ANTIGÜEDAD
                    var priceByAge = CalculatePriceByReleaseDate(gameDetails.Released);
                    
                    // ✅ ESTRATEGIA 3: Basado en CATEGORÍA/EDICIÓN
                    var priceByCategory = CalculatePriceByCategory(gameTitle, gameDetails.Genres);
                    
                    // ✅ COMBINAR ESTRATEGIAS (tomar el mayor valor)
                    var finalPrice = Math.Max(priceByRating, Math.Max(priceByAge, priceByCategory));
                    
                    Console.WriteLine($"💰 Precios calculados - Rating: {priceByRating}, Edad: {priceByAge}, Categoría: {priceByCategory} → Final: {finalPrice}");
                    
                    return finalPrice;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error al calcular precio dinámico: {ex.Message}");
                // Continuar con precio por defecto
            }

            // Precio por defecto si hay error
            Console.WriteLine($"💰 Usando precio por defecto: 39.99");
            return 39.99m;
        }

        // ✅ ESTRATEGIA 1: PRECIO POR RATING Y GÉNERO
        private decimal CalculatePriceByRating(double rating, List<Genre> genres)
        {
            var basePrice = rating switch
            {
                >= 4.5 => 79.99m,  // Excelente
                >= 4.0 => 59.99m,  // Muy bueno
                >= 3.5 => 44.99m,  // Bueno
                >= 3.0 => 34.99m,  // Regular
                _ => 24.99m        // Básico
            };

            // Ajustar por género (juegos AAA vs indie)
            var primaryGenre = genres?.FirstOrDefault()?.Name?.ToLower() ?? "";
            if (primaryGenre.Contains("indie") || primaryGenre.Contains("casual"))
            {
                basePrice = Math.Min(basePrice, 29.99m); // Límite máximo para indies
            }

            return basePrice;
        }

        // ✅ ESTRATEGIA 2: PRECIO POR ANTIGÜEDAD
        private decimal CalculatePriceByReleaseDate(string releasedDate)
        {
            if (string.IsNullOrEmpty(releasedDate) || !DateTime.TryParse(releasedDate, out var releaseDate))
            {
                return 39.99m; // Fecha no disponible
            }

            var currentDate = DateTime.Now;
            var yearsSinceRelease = currentDate.Year - releaseDate.Year;

            return yearsSinceRelease switch
            {
                0 => 69.99m,  // Juego del año actual
                1 => 59.99m,  // 1 año de antigüedad
                2 => 49.99m,  // 2 años
                3 => 39.99m,  // 3 años
                4 => 29.99m,  // 4 años
                5 => 24.99m,  // 5 años
                _ => 19.99m   // Más de 5 años
            };
        }

        // ✅ ESTRATEGIA 3: PRECIO POR CATEGORÍA Y EDICIÓN
        private decimal CalculatePriceByCategory(string gameTitle, List<Genre> genres)
        {
            var titleLower = gameTitle.ToLower();
            var primaryGenre = genres?.FirstOrDefault()?.Name?.ToLower() ?? "";

            // Ediciones especiales/premium
            if (titleLower.Contains("deluxe") || 
                titleLower.Contains("collector") || 
                titleLower.Contains("ultimate") ||
                titleLower.Contains("premium"))
            {
                return 89.99m;
            }

            if (titleLower.Contains("edition") || 
                titleLower.Contains("bundle") || 
                titleLower.Contains("pack"))
            {
                return 69.99m;
            }

            // Por género específico
            return primaryGenre switch
            {
                string g when g.Contains("action") || g.Contains("adventure") => 59.99m,
                string g when g.Contains("rpg") || g.Contains("role-playing") => 64.99m,
                string g when g.Contains("strategy") || g.Contains("simulation") => 49.99m,
                string g when g.Contains("sports") || g.Contains("racing") => 54.99m,
                string g when g.Contains("indie") || g.Contains("casual") => 29.99m,
                string g when g.Contains("shooter") => 59.99m,
                _ => 44.99m // Género no especificado
            };
        }

        public async Task<IActionResult> Success(string session_id)
        {
            if (string.IsNullOrEmpty(session_id))
            {
                TempData["ErrorMessage"] = "Sesión de pago no válida";
                return RedirectToAction("Index", "Biblioteca");
            }

            try
            {
                Console.WriteLine($"🔍 SUCCESS llamado con session_id: {session_id}");
                
                var success = await _paymentService.ConfirmPaymentAsync(session_id);
                
                if (success)
                {
                    Console.WriteLine("✅ Pago confirmado, agregando a biblioteca...");
                    
                    // ✅ AGREGAR AUTOMÁTICAMENTE EL JUEGO A LA BIBLIOTECA EN "PENDIENTES"
                    var result = await AddGameToLibraryAfterPurchase(session_id);
                    
                    if (result)
                    {
                        TempData["SuccessMessage"] = "¡Compra realizada exitosamente! El juego ha sido añadido a tu biblioteca en Pendientes.";
                        // ✅ REDIRIGIR DIRECTAMENTE A PENDIENTES
                        return RedirectToAction("Pendientes", "Biblioteca");
                    }
                    else
                    {
                        TempData["WarningMessage"] = "Pago exitoso, pero hubo un problema al agregar el juego a tu biblioteca. Contacta con soporte.";
                        return RedirectToAction("Index", "Biblioteca");
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "No se pudo confirmar el pago. Contacta con soporte.";
                    return RedirectToAction("Index", "Biblioteca");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al confirmar pago");
                TempData["ErrorMessage"] = "Error al confirmar el pago.";
                return RedirectToAction("Index", "Biblioteca");
            }
        }

        public IActionResult Cancel()
        {
            TempData["WarningMessage"] = "La compra fue cancelada. Puedes intentarlo de nuevo cuando lo desees.";
            return RedirectToAction("Index", "Biblioteca");
        }

        // ✅ MÉTODO TEMPORAL PARA PRUEBAS DIRECTAS
        [HttpGet]
        public async Task<IActionResult> TestDynamicPricing(int gameId = 3328, string gameTitle = "The Witcher 3")
        {
            Console.WriteLine("🧪 TEST PRECIOS DINÁMICOS =========================");
            
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            Console.WriteLine($"🧪 UserId: {userId}");
            Console.WriteLine($"🧪 GameId: {gameId}");
            Console.WriteLine($"🧪 GameTitle: {gameTitle}");

            if (string.IsNullOrEmpty(userId))
            {
                return Content("❌ NO AUTENTICADO - Inicia sesión primero");
            }

            try
            {
                // Probar el cálculo de precios
                var dynamicPrice = await GetDynamicPriceAsync(gameId, gameTitle, 0);
                Console.WriteLine($"🧪 Precio dinámico calculado: {dynamicPrice}");

                var successUrl = Url.Action("Success", "Payment", null, Request.Scheme);
                var cancelUrl = Url.Action("Cancel", "Payment", null, Request.Scheme);

                var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(
                    gameId, userId, dynamicPrice, gameTitle, successUrl, cancelUrl);

                Console.WriteLine($"🧪 Checkout URL: {checkoutUrl}");

                if (!string.IsNullOrEmpty(checkoutUrl))
                {
                    return Redirect(checkoutUrl);
                }
                else
                {
                    return Content("❌ No se pudo generar URL de checkout");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR en TestDynamicPricing: {ex.Message}");
                return Content($"❌ ERROR: {ex.Message}");
            }
        }

        // ✅ MÉTODO MEJORADO CON LOGS DETALLADOS
        private async Task<bool> AddGameToLibraryAfterPurchase(string sessionId)
        {
            try
            {
                Console.WriteLine("📚 Agregando juego a la biblioteca después de compra...");
                
                var transaction = await _paymentService.GetTransactionBySessionIdAsync(sessionId);
                if (transaction != null)
                {
                    Console.WriteLine($"📚 Transacción encontrada:");
                    Console.WriteLine($"   - UsuarioId: {transaction.UsuarioId}");
                    Console.WriteLine($"   - GameId: {transaction.GameId}");
                    Console.WriteLine($"   - GameTitle: {transaction.GameTitle}");
                    Console.WriteLine($"   - Estado: {transaction.PaymentStatus}");

                    // ✅ VERIFICAR QUE NO EXISTA YA EN BIBLIOTECA
                    var existingEntry = await _context.BibliotecaUsuario
                        .FirstOrDefaultAsync(b => b.UsuarioId == transaction.UsuarioId && b.RawgGameId == transaction.GameId);
                    
                    if (existingEntry == null)
                    {
                        Console.WriteLine("✅ El juego NO existe en biblioteca, agregando...");
                        
                        // ✅ OBTENER DETALLES ACTUALIZADOS DEL JUEGO
                        var gameDetails = await _rawgService.GetGameDetailsAsync(transaction.GameId);
                        Console.WriteLine($"📱 Detalles del juego obtenidos: {(gameDetails != null ? "SÍ" : "NO")}");
                        
                        var libraryEntry = new BibliotecaUsuario
                        {
                            UsuarioId = transaction.UsuarioId,
                            RawgGameId = transaction.GameId, // ✅ USAR EL MISMO ID
                            Estado = "Pendiente",
                            GameName = gameDetails?.Name ?? transaction.GameTitle,
                            GameImage = gameDetails?.BackgroundImage ?? "https://via.placeholder.com/400x200?text=Comprado"
                        };

                        await _context.BibliotecaUsuario.AddAsync(libraryEntry);
                        await _context.SaveChangesAsync();
                        Console.WriteLine("✅ Juego agregado a la biblioteca en estado 'Pendiente'");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("ℹ️ El juego YA estaba en la biblioteca");
                        return true; // Ya existe, considerar como éxito
                    }
                }
                else
                {
                    Console.WriteLine("❌ No se encontró la transacción para agregar a biblioteca");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al agregar juego a biblioteca: {ex.Message}");
                Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                return false;
            }
        }
    }
}