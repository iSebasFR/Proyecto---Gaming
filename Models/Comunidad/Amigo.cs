using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Gaming.Models.Comunidad
{
    public class Amigo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? UsuarioId { get; set; }

        [Required]
        public string? AmigoId { get; set; }

        [Required]
        public string? Estado { get; set; } // "Pendiente", "Aceptado", "Rechazado"

        public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;
        public DateTime? FechaAceptacion { get; set; }

        // Navigation properties
        [ForeignKey("UsuarioId")]
        public virtual Usuario? Usuario { get; set; }

        [ForeignKey("AmigoId")]
        public virtual Usuario? AmigoUsuario { get; set; }
    }
}