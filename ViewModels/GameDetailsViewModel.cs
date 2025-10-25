using Proyecto_Gaming.Models.Rawg;

namespace Proyecto_Gaming.ViewModels
{
    public class GameDetailsViewModel
    {
        public Game Game { get; set; } = new Game();
        public decimal? CurrentPrice { get; set; }
        public string? BestStore { get; set; }
        public bool IsOnSale { get; set; }
        public List<StoreOffer> StoreOffers { get; set; } = new List<StoreOffer>();
    }

    public class StoreOffer
    {
        public string StoreName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal RetailPrice { get; set; }
        public string Savings { get; set; } = string.Empty;
        public string DealUrl { get; set; } = string.Empty;
        public bool IsOnSale => Price < RetailPrice;
    }
}