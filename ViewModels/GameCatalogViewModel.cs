using Microsoft.AspNetCore.Mvc.Rendering;
using Proyecto_Gaming.Models.Rawg;
using Proyecto_Gaming.Models;
using Proyecto_Gaming.Helpers;
using System.Linq;

namespace Proyecto_Gaming.ViewModels
{
    public class GameCatalogViewModel
    {
        // PROPIEDADES EXISTENTES (NO MODIFICAR)
        public List<Game> Games { get; set; } = new List<Game>();
        public List<Genre> Genres { get; set; } = new List<Genre>();
        public List<Platform> Platforms { get; set; } = new List<Platform>();
        public string? Search { get; set; }
        public string? SelectedGenre { get; set; }
        public string? SelectedPlatform { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        public List<SelectListItem> AvailableGenres { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AvailablePlatforms { get; set; } = new List<SelectListItem>();
        
        // PROPIEDADES DE SESIÓN EXISTENTES
        public UserPreferences UserPreferences { get; set; } = new UserPreferences();
        public List<UserSearch> RecentSearches { get; set; } = new List<UserSearch>();
        public List<string> RecentSearchTerms { get; set; } = new List<string>();
        public bool HasSearchHistory => RecentSearches?.Any() == true;
        public bool HasPreviousSearch { get; set; }

        // ✅ NUEVA PROPIEDAD PARA PRECIOS (AGREGAR SOLO ESTA)
        public Dictionary<int, decimal?> GamePrices { get; set; } = new Dictionary<int, decimal?>();
    }
}