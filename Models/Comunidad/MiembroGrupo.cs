using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Gaming.Models.Comunidad
{
    public class MiembroGrupo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GrupoId { get; set; }

        [Required]
        public string UsuarioId { get; set; }

        [Required]
        [StringLength(20)]
        public string Rol { get; set; } // "Administrador", "Moderador", "Miembro"

        public DateTime FechaUnion { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("GrupoId")]
        public virtual Grupo Grupo { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario Usuario { get; set; }
    }
}