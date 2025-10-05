using System.ComponentModel.DataAnnotations;

namespace Proyecto_Gaming.Models
{
    public class BibliotecaUsuario
    {
        [Key]
        public int Id { get; set; }

        public string IdUsuario { get; set; }  // string para Identity
        public int IdJuego { get; set; }
        public string Estado { get; set; }  // "Jugando", "Completado", "En lista", etc.

        // Navigation properties
        public Usuario Usuario { get; set; }
        public Juego Juego { get; set; }
    }
}