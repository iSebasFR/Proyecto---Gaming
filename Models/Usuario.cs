using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Gaming.Models
{
    public class Usuario : IdentityUser
    {
        // Propiedades personalizadas
        public string? NombreReal { get; set; }
        
        [Display(Name = "Nombre para mostrar")]
        [StringLength(50)]
        public string? DisplayName { get; set; }  // NUEVO: Nombre público
        
        public DateTime FechaNacimiento { get; set; }
        public string? Biografia { get; set; }
        public string? Pais { get; set; }
        public string? FotoPerfil { get; set; }
        public string? PlataformaPreferida { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
        public string? Estado { get; set; } = "Activo";

        public string? GoogleId { get; set; }
        public string? GoogleProfilePicture { get; set; }

        // Relación con BibliotecaUsuario
        public ICollection<BibliotecaUsuario>? BibliotecaUsuarios { get; set; }
    }
}