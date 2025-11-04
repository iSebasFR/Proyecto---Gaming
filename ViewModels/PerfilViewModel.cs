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

        // Datos para gráficas de tendencia
        public IEnumerable<int>? MonthlyHours { get; set; }

        // Datos para gráfica de barras: juegos completados top
        public Dictionary<string, int>? TopJuegosFinalizados { get; set; }

        // Datos para gráfica de tendencia de reseñas por mes
        public IEnumerable<int>? MonthlyReviews { get; set; }

        // Conteo total de reseñas realizadas
        public int TotalReviews { get; set; }

        // Nuevos amigos por mes para gráfica
        public IEnumerable<int>? MonthlyFriends { get; set; }

        // Nuevos grupos por mes para gráfica
        public IEnumerable<int>? MonthlyGroups { get; set; }

        // 🏅 Medallas del usuario (para el parcial _UserMedals)
   public List<Proyecto_Gaming.ViewModels.Perfil.PerfilUsuarioVM.MedallaVM> Medallas { get; set; } = new();

    }

    public class UsuarioAmigoViewModel
    {
        public string? Nombre { get; set; }
        public string? Estado { get; set; }
        public string? Avatar { get; set; }
    }

    // 👇 Mantengo el nombre que usas en la vista: PerfilUsuarioVM.MedallaVM
    public static class PerfilUsuarioVM
    {
        public class MedallaVM
        {
            public int MedalId { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string? Descripcion { get; set; }
            public string? IconoUrl { get; set; }   // ej. "/img/medals/gold.png"
            public int Points { get; set; }         // si no usas puntos, déjalo en 0
            public DateTime? GrantedAtUtc { get; set; }
        }
    }
}
