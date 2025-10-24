// Controllers/Api/GoogleAuthController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Proyecto_Gaming.Models;
using Proyecto_Gaming.Services;
using System.Security.Claims;

namespace Proyecto_Gaming.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class GoogleAuthController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly SignInManager<Usuario> _signInManager;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly ILogger<GoogleAuthController> _logger;

        public GoogleAuthController(
            UserManager<Usuario> userManager,
            SignInManager<Usuario> signInManager,
            IGoogleAuthService googleAuthService,
            ILogger<GoogleAuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _googleAuthService = googleAuthService;
            _logger = logger;
        }

        [HttpGet("login")]
        public IActionResult Login(string returnUrl = "/")
        {
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", returnUrl);
            return Challenge(properties, "Google");
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback(string returnUrl = "/", string remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            if (remoteError != null)
            {
                _logger.LogError("Error externo de Google: {Error}", remoteError);
                return RedirectToPage("/Account/Login", new { ReturnUrl = returnUrl, Error = "Error de autenticación con Google" });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                _logger.LogError("No se pudo obtener información del login externo");
                return RedirectToPage("/Account/Login", new { ReturnUrl = returnUrl, Error = "Error al obtener información de Google" });
            }

            try
            {
                var user = await _googleAuthService.CreateOrUpdateUserFromGoogleAsync(info);
                
                if (user == null)
                {
                    // ❌ Usuario no registrado
                    var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                    var name = info.Principal.FindFirstValue(ClaimTypes.Name);
                    
                    _logger.LogWarning("Intento de login con Google para cuenta no registrada: {Email}", email);
                    
                    // Usar query parameters en lugar de TempData
                    return RedirectToPage("/Account/Login", new { 
                        ReturnUrl = returnUrl, 
                        Error = "Cuenta no registrada",
                        GoogleEmail = email,
                        GoogleName = name
                    });
                }

                // ✅ Usuario existe - proceder con login
                await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation("Usuario autenticado exitosamente via Google: {Email}", user.Email);

                return Redirect(returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el callback de Google Auth");
                return RedirectToPage("/Account/Login", new { ReturnUrl = returnUrl, Error = "Error interno del servidor" });
            }
        }

        [HttpGet("userinfo")]
        public async Task<IActionResult> GetUserInfo()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    return Ok(new
                    {
                        user.Id,
                        user.UserName,
                        user.Email,
                        user.DisplayName,
                        user.GoogleId,
                        user.GoogleProfilePicture,
                        user.PlataformaPreferida,
                        IsGoogleUser = !string.IsNullOrEmpty(user.GoogleId)
                    });
                }
            }
            return Unauthorized();
        }
    }
}