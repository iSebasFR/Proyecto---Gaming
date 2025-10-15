using System.Text.Json;
using Proyecto_Gaming.Models;

namespace Proyecto_Gaming.Helpers
{
    public static class SessionHelper
    {
        // Claves de sesión para ML
        public const string UserSearchHistory = "UserSearchHistory";
        public const string UserPreferences = "UserPreferences";
        public const string RecentSearches = "RecentSearches";
        public const string UserFilters = "UserFilters";

        // Métodos de extensión para sesiones
        public static void SetObject<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T? GetObject<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }

        // Historial de búsquedas para ML
        public static void AddSearchToHistory(this ISession session, UserSearch search)
        {
            var history = session.GetObject<List<UserSearch>>(UserSearchHistory) ?? new List<UserSearch>();
            history.Add(search);
            
            // Mantener solo las últimas 50 búsquedas
            if (history.Count > 50)
                history = history.TakeLast(50).ToList();
                
            session.SetObject(UserSearchHistory, history);
        }

        public static List<UserSearch> GetSearchHistory(this ISession session)
        {
            return session.GetObject<List<UserSearch>>(UserSearchHistory) ?? new List<UserSearch>();
        }

        // Preferencias de usuario
        public static void SetUserPreferences(this ISession session, UserPreferences prefs)
        {
            session.SetObject(UserPreferences, prefs);
        }

        public static UserPreferences GetUserPreferences(this ISession session)
        {
            return session.GetObject<UserPreferences>(UserPreferences) ?? new UserPreferences();
        }

        // Búsquedas recientes
        public static void AddRecentSearch(this ISession session, string searchTerm)
        {
            var recent = session.GetObject<List<string>>(RecentSearches) ?? new List<string>();
            
            // Remover si ya existe y agregar al inicio
            recent.Remove(searchTerm);
            recent.Insert(0, searchTerm);
            
            // Mantener solo las últimas 10 búsquedas
            if (recent.Count > 10)
                recent = recent.Take(10).ToList();
                
            session.SetObject(RecentSearches, recent);
        }

        public static List<string> GetRecentSearches(this ISession session)
        {
            return session.GetObject<List<string>>(RecentSearches) ?? new List<string>();
        }
    }

    // Modelos para ML
    public class UserSearch
    {
        public string? SearchTerm { get; set; }
        public string? Genre { get; set; }
        public string? Platform { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int? UserRating { get; set; } // Para correlaciones ML
    }
}