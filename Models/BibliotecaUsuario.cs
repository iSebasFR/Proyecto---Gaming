using System.ComponentModel.DataAnnotations;

namespace Proyecto_Gaming.Models
{
    public class BibliotecaUsuario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string IdUsuario { get; set; }

        [Required]
        public int RawgGameId { get; set; }

        [Required]
        public string Estado { get; set; } = "Pendiente";

        [Required]
        public string GameName { get; set; }

        [Required]
        public string GameImage { get; set; } = "https://via.placeholder.com/400x200?text=Imagen+No+Disponible";

        public string Resena { get; set; } = "";
        public int Calificacion { get; set; } = 0;

        public DateTime? FechaCompletado { get; set; }
        public DateTime? FechaResena { get; set; }
    }
}