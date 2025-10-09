using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Gaming.Models.Comunidad
{
    public class MultimediaGrupo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GrupoId { get; set; }

        [Required]
        public string UsuarioId { get; set; }

        [Required]
        [StringLength(255)]
        public string UrlArchivo { get; set; }

        [StringLength(50)]
        public string TipoArchivo { get; set; } // "imagen", "video"

        [StringLength(200)]
        public string Descripcion { get; set; }

        public DateTime FechaSubida { get; set; } = DateTime.UtcNow;
        public int Likes { get; set; } = 0;

        // Nuevos campos para reacciones
        public int Love { get; set; } = 0;
        public int Wow { get; set; } = 0;
        public int Sad { get; set; } = 0;
        public int Angry { get; set; } = 0;

        // Navigation properties
        [ForeignKey("GrupoId")]
        public virtual Grupo Grupo { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario Usuario { get; set; }

        public virtual ICollection<ComentarioMultimedia> Comentarios { get; set; }
        
        // NUEVO: Navigation property para reacciones
        public virtual ICollection<ReaccionMultimedia> Reacciones { get; set; }
    }
}