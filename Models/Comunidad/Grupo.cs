using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Gaming.Models.Comunidad
{
    public class Grupo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        [StringLength(500)]
        public string Descripcion { get; set; }

        [StringLength(255)]
        public string FotoGrupo { get; set; }

        [StringLength(255)]
        public string BannerGrupo { get; set; }

        [Required]
        [StringLength(50)]
        public string Categoria { get; set; } // "Cooperativo", "RPG", "Aventura", "Acción", etc.

        [Required]
        public string CreadorId { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public bool EsPublico { get; set; } = true;

        // Navigation properties
        [ForeignKey("CreadorId")]
        public virtual Usuario Creador { get; set; }

        public virtual ICollection<MiembroGrupo> Miembros { get; set; }
        public virtual ICollection<PublicacionGrupo> Publicaciones { get; set; }
        public virtual ICollection<MultimediaGrupo> Multimedia { get; set; }
    }
}