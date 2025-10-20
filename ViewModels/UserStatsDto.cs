namespace Proyecto_Gaming.ViewModels
{
    public class UserStatsDto
    {
        public string UserId { get; set; } = default!;
        public int TotalGames { get; set; }
        public int CompletedGames { get; set; }
        public int TotalHours { get; set; }
        public int FriendsCount { get; set; }
        // Horas por mes para mostrar tendencias (por ejemplo, últimos 6 meses)
        public IEnumerable<int>? MonthlyHours { get; set; }
        // Juegos actualmente en estado 'Jugando'
        public int JuegosJugando { get; set; }
    // Juegos en estado 'Pendiente'
    public int PendingGames { get; set; }
    // Top juegos finalizados: clave = nombre de juego, valor = veces completado
    public Dictionary<string,int>? TopCompletedGames { get; set; }
    // Reseñas por mes (últimos 6 meses)
    public IEnumerable<int>? MonthlyReviews { get; set; }
        
    // Conteo total de reseñas realizadas
    public int ReviewsCount { get; set; }
    // Nuevos amigos por mes (últimos N meses)
    public IEnumerable<int>? MonthlyFriends { get; set; }
    // Nuevos grupos por mes (últimos N meses)
    public IEnumerable<int>? MonthlyGroups { get; set; }
    // Conteo total de grupos
    public int GroupsCount { get; set; }
    }
}
