using Proyecto_Gaming.Models;

namespace Proyecto_Gaming.ViewModels
{
    public class PerfilViewModel
    {
        public Usuario Usuario { get; set; }
        public int TotalJuegos { get; set; }
        public int JuegosPendientes { get; set; }
        public int JuegosJugando { get; set; }
        public int JuegosCompletados { get; set; }
        public List<BibliotecaUsuario> BibliotecaReciente { get; set; }
        public List<BibliotecaUsuario> JuegosDestacados { get; set; }
        
        // Para la sección visual de amigos (no funcional aún)
        public List<UsuarioAmigoViewModel> AmigosVisual { get; set; }
    }

    public class UsuarioAmigoViewModel
    {
        public string Nombre { get; set; }
        public string Estado { get; set; }
        public string Avatar { get; set; }
    }
}