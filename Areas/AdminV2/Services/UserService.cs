// Areas/AdminV2/Services/UserService.cs
using System.Threading.Tasks;

namespace Proyecto_Gaming.Services   // <-- importante: usa este namespace
{
    // Interfaz
    public interface IUserService
    {
        Task<UserProfileDto> GetProfileAsync(string userId);
    }

    // Implementación mínima (stub)
    public class UserService : IUserService
    {
        public Task<UserProfileDto> GetProfileAsync(string userId)
        {
            // TODO: reemplazar por tu lógica real
            var dto = new UserProfileDto
            {
                Id = userId,
                DisplayName = "Jugador",
                Email = "user@example.com"
            };
            return Task.FromResult(dto);
        }
    }

    // DTO simple para Perfil
    public class UserProfileDto
    {
        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Email { get; set; } = "";
    }
}
