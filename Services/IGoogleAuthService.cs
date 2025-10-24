using Microsoft.AspNetCore.Identity;
using Proyecto_Gaming.Models;

namespace Proyecto_Gaming.Services
{
    public interface IGoogleAuthService
    {
        Task<Usuario> CreateOrUpdateUserFromGoogleAsync(ExternalLoginInfo info);
    }
}