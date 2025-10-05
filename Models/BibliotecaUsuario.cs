using System.ComponentModel.DataAnnotations;

namespace Proyecto_Gaming.Models
{
    public class BibliotecaUsuario
    {
        [Key]
        public int Id { get; set; }

        public string IdUsuario { get; set; }
        public int RawgGameId { get; set; }  // ID de RAWG
        public string Estado { get; set; }   // "Pendiente", "Jugando", "Completado"

        // Información básica del juego
        public string GameName { get; set; }
        public string GameImage { get; set; }
    }
}