using Proyecto_Gaming.Models.Rawg;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Proyecto_Gaming.ViewModels
{
    public class GameCatalogViewModel
    {
        public List<Game> Games { get; set; } = new List<Game>();
        public List<Genre> Genres { get; set; } = new List<Genre>();
        public List<Platform> Platforms { get; set; } = new List<Platform>();
        public string Search { get; set; }
        public string SelectedGenre { get; set; }
        public string SelectedPlatform { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        
        // Propiedad para páginas visibles (siempre máximo 5)
        public int DisplayTotalPages => Math.Min(TotalPages, 5);

        // NUEVAS PROPIEDADES PARA FILTROS DINÁMICOS
        public List<SelectListItem> AvailableGenres { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AvailablePlatforms { get; set; } = new List<SelectListItem>();
    }
}