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
        public string? DisplayName { get; set; }

        public DateTime FechaNacimiento { get; set; }
        
        [StringLength(500)]
        public string? Biografia { get; set; }
        
        [StringLength(100)]
        public string? Pais { get; set; }
        
        [StringLength(500)]
        public string? FotoPerfil { get; set; }
        
        [StringLength(200)]
        public string? PlataformaPreferida { get; set; }
        
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
        
        [StringLength(50)]
        public string? Estado { get; set; } = "Activo";

        // Nuevas propiedades para Google
        public string? GoogleId { get; set; }
        public string? GoogleProfilePicture { get; set; }

        // Relación con BibliotecaUsuario
        public ICollection<BibliotecaUsuario>? BibliotecaUsuarios { get; set; }
    }
}