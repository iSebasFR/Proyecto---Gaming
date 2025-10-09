using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Gaming.Models.Comunidad
{
    public class ComentarioPublicacion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PublicacionId { get; set; }

        [Required]
        public string UsuarioId { get; set; }

        [Required]
        public string Contenido { get; set; }

        public DateTime FechaComentario { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("PublicacionId")]
        public virtual PublicacionGrupo Publicacion { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario Usuario { get; set; }
    }
}