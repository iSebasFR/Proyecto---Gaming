using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Gaming.Models
{
    public class Usuario : IdentityUser
    {
        // ─────────────────────────────
        // Datos básicos / perfil
        // ─────────────────────────────
        [StringLength(100)]
        public string? NombreReal { get; set; }

        [Display(Name = "Nombre para mostrar")]
        [StringLength(50)]
        public string? DisplayName { get; set; }

        // Hacerla nullable para evitar 01/01/0001 cuando no se captura
        public DateTime? FechaNacimiento { get; set; }

        [StringLength(500)]
        public string? Biografia { get; set; }

        [StringLength(100)]
        public string? Pais { get; set; }

        [StringLength(500)]
        public string? FotoPerfil { get; set; }

        [StringLength(200)]
        public string? PlataformaPreferida { get; set; }

        // ─────────────────────────────
        // Auditoría / estado
        // ─────────────────────────────
        /// <summary>
        /// Fecha de registro en UTC (columna real en BD).
        /// </summary>
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Alias de conveniencia para compatibilidad con código que usa "CreatedAt".
        /// No se mapea como columna nueva; refleja FechaRegistro.
        /// </summary>
        [NotMapped]
        public DateTime CreatedAt
        {
            get => FechaRegistro;
            set => FechaRegistro = value;
        }

        [StringLength(50)]
        public string? Estado { get; set; } = "Activo";

        // ─────────────────────────────
        // OAuth / integraciones
        // ─────────────────────────────
        public string? GoogleId { get; set; }
        public string? GoogleProfilePicture { get; set; }

        // ─────────────────────────────
        // Relaciones
        // ─────────────────────────────
        public ICollection<BibliotecaUsuario>? BibliotecaUsuarios { get; set; }
    }
}
