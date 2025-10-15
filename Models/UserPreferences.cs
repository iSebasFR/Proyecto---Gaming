using System.Collections.Generic;

namespace Proyecto_Gaming.Models
{
    public class UserPreferences
    {
        public string Theme { get; set; } = "dark";
        public int GamesPerPage { get; set; } = 20;
        public bool NotificationsEnabled { get; set; } = true;
        public List<string> FavoriteGenres { get; set; } = new List<string>();
        public List<int> FavoritePlatforms { get; set; } = new List<int>();
        public bool ShowRecentSearches { get; set; } = true;
        public string DefaultSort { get; set; } = "rating";
    }
}