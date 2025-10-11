using System.ComponentModel.DataAnnotations;

namespace Proyecto_Gaming.Models
{
    public class BibliotecaUsuario
    {
        [Key]
        public int Id { get; set; }

        // Coincide con la columna real en BD
        public required string UsuarioId { get; set; } = null!;

        [Required]
        public int RawgGameId { get; set; }

        public string Estado { get; set; } = "Pendiente";

        public required string GameName { get; set; } = null!;

        public string GameImage { get; set; } =
            "https://via.placeholder.com/400x200?text=Imagen+No+Disponible";

        public string? Resena { get; set; } = "";
        public int Calificacion { get; set; } = 0;

        public DateTime? FechaCompletado { get; set; }
        public DateTime? FechaResena { get; set; }
    }
}
