using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Gaming.Models.Comunidad
{
    public class ComentarioMultimedia
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MultimediaId { get; set; }

        [Required]
        public string UsuarioId { get; set; }

        [Required]
        public string Contenido { get; set; }

        public DateTime FechaComentario { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("MultimediaId")]
        public virtual MultimediaGrupo Multimedia { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario Usuario { get; set; }
    }
}