// Services/GoogleAuthService.cs
using Microsoft.AspNetCore.Identity;
using Proyecto_Gaming.Models;
using System.Security.Claims;

namespace Proyecto_Gaming.Services
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly ILogger<GoogleAuthService> _logger;

        public GoogleAuthService(UserManager<Usuario> userManager, ILogger<GoogleAuthService> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<Usuario> CreateOrUpdateUserFromGoogleAsync(ExternalLoginInfo info)
        {
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            
            if (string.IsNullOrEmpty(email))
            {
                throw new Exception("No se pudo obtener el email de Google");
            }

            // Buscar usuario por email
            var user = await _userManager.FindByEmailAsync(email);

            // Obtener claims de Google
            var name = info.Principal.FindFirstValue(ClaimTypes.Name);
            var givenName = info.Principal.FindFirstValue(ClaimTypes.GivenName);
            var surname = info.Principal.FindFirstValue(ClaimTypes.Surname);
            var googleId = info.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Intentar obtener la foto de perfil
            var picture = info.Principal.FindFirstValue("picture") 
                         ?? info.Principal.FindFirstValue("image") 
                         ?? info.Principal.Claims.FirstOrDefault(c => c.Type.Contains("picture"))?.Value;

            if (user == null)
            {
                // ❌ POLÍTICA: No crear usuarios automáticamente
                // Retornar null para indicar que el usuario no existe
                _logger.LogWarning("Usuario no encontrado para email de Google: {Email}", email);
                return null;
            }
            else
            {
                // ✅ Usuario existe - actualizar información de Google
                user.GoogleId = googleId;
                user.GoogleProfilePicture = picture ?? user.GoogleProfilePicture;
                
                // Solo actualizar si está vacío
                if (string.IsNullOrEmpty(user.DisplayName) && !string.IsNullOrEmpty(name))
                    user.DisplayName = name;
                    
                if (string.IsNullOrEmpty(user.NombreReal) && !string.IsNullOrEmpty(givenName))
                    user.NombreReal = $"{givenName} {surname}".Trim();

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    _logger.LogWarning("Error actualizando usuario: {Errors}", 
                        string.Join(", ", updateResult.Errors));
                }
                
                _logger.LogInformation("Usuario actualizado via Google: {Email}", email);
            }

            // Agregar login externo si no existe
            var existingLogins = await _userManager.GetLoginsAsync(user);
            if (!existingLogins.Any(l => l.LoginProvider == "Google" && l.ProviderKey == info.ProviderKey))
            {
                var addLoginResult = await _userManager.AddLoginAsync(user, 
                    new UserLoginInfo("Google", info.ProviderKey, "Google"));
                
                if (!addLoginResult.Succeeded)
                {
                    _logger.LogWarning("No se pudo agregar el login externo: {Errors}", 
                        string.Join(", ", addLoginResult.Errors));
                }
            }

            return user;
        }
    }
}