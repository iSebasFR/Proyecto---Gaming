using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Gaming.Models.Comunidad
{
    public class ReaccionMultimedia
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MultimediaId { get; set; }

        [Required]
        public string UsuarioId { get; set; }

        [Required]
        [StringLength(20)]
        public string TipoReaccion { get; set; } // "like", "love", "wow", "sad", "angry"

        public DateTime FechaReaccion { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("MultimediaId")]
        public virtual MultimediaGrupo Multimedia { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario Usuario { get; set; }
    }
}