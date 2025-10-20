using Proyecto_Gaming.Models;

namespace Proyecto_Gaming.ViewModels
{
    public class PerfilViewModel
    {
        public Usuario? Usuario { get; set; }
        public int TotalJuegos { get; set; }
        public int JuegosPendientes { get; set; }
        public int JuegosJugando { get; set; }
        public List<BibliotecaUsuario>? BibliotecaReciente { get; set; }
        public List<BibliotecaUsuario>? JuegosDestacados { get; set; }
        
        // Para la sección visual de amigos (no funcional aún)
        public List<UsuarioAmigoViewModel>? AmigosVisual { get; set; }
        // Estadísticas de usuario
        public int AmigosCount { get; set; }
        public int TotalHoras { get; set; }
        public int JuegosCompletados { get; set; }
        public int GruposCount { get; set; }
    // Datos para graficas de tendencia
    public IEnumerable<int>? MonthlyHours { get; set; }
        
    // Datos para gráfica de barras: juegos completados top
    public Dictionary<string,int>? TopJuegosFinalizados { get; set; }
    // Datos para gráfica de tendencia de reseñas por mes
    public IEnumerable<int>? MonthlyReviews { get; set; }
    // Conteo total de reseñas realizadas
    public int TotalReviews { get; set; }
    // Nuevos amigos por mes para gráfica
    public IEnumerable<int>? MonthlyFriends { get; set; }
    // Nuevos grupos por mes para gráfica
    public IEnumerable<int>? MonthlyGroups { get; set; }
    }

    public class UsuarioAmigoViewModel
    {
        public string? Nombre { get; set; }
        public string? Estado { get; set; }
        public string? Avatar { get; set; }
    }
}