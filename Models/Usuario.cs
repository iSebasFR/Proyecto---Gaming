using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace Proyecto_Gaming.Models
{
    public class Usuario : IdentityUser  // Asegúrate que herede de IdentityUser
    {
        // Propiedades personalizadas (mantén las que necesites)
        public string? NombreReal { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public string? Biografia { get; set; }
        public string? Pais { get; set; }
        public string? FotoPerfil { get; set; }
        public string? PlataformaPreferida { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
        public string? Estado { get; set; }

        // Relación con BibliotecaUsuario
        public ICollection<BibliotecaUsuario> BibliotecaUsuarios { get; set; }

        // IdentityUser ya proporciona:
        // - Id (string) ✅
        // - UserName (string)
        // - Email (string)
        // - PasswordHash (string)
        // - Y muchas más propiedades
    }
}