using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Gaming.Models.Comunidad
{
    public class PublicacionGrupo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GrupoId { get; set; }

        [Required]
        public string? UsuarioId { get; set; }

        [Required]
        public string? Contenido { get; set; }

        public DateTime FechaPublicacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaEdicion { get; set; }

        public int Likes { get; set; } = 0;

        // Navigation properties
        [ForeignKey("GrupoId")]
        public virtual Grupo? Grupo { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario? Usuario { get; set; }

        public virtual ICollection<ComentarioPublicacion>? Comentarios { get; set; }
    }
}