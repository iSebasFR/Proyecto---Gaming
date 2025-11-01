using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Gaming.Data;
using Proyecto_Gaming.Areas.AdminV2.ViewModels;

namespace Proyecto_Gaming.Areas.AdminV2.Controllers
{
    [Area("AdminV2")]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ReviewsController(ApplicationDbContext db) => _db = db;

        public IActionResult Index(string? filter)
        {
            var q = _db.ContactMessages.AsNoTracking();

            var positives = q.Where(c => c.Sentiment == "positive")
                             .OrderByDescending(c => c.CreatedAtUtc)
                             .Select(c => new ReviewListItem {
                                 Id = c.Id,
                                 Name = c.Name ?? "",
                                 Email = c.Email ?? "",
                                 Message = c.Message,
                                 CreatedAtUtc = c.CreatedAtUtc,
                                 Sentiment = c.Sentiment ?? "unknown",
                                 SentimentScore = c.SentimentScore
                             })
                             .ToList();

            var negatives = q.Where(c => c.Sentiment == "negative")
                             .OrderByDescending(c => c.CreatedAtUtc)
                             .Select(c => new ReviewListItem {
                                 Id = c.Id,
                                 Name = c.Name ?? "",
                                 Email = c.Email ?? "",
                                 Message = c.Message,
                                 CreatedAtUtc = c.CreatedAtUtc,
                                 Sentiment = c.Sentiment ?? "unknown",
                                 SentimentScore = c.SentimentScore
                             })
                             .ToList();

            var vm = new ReviewsPageViewModel
            {
                Positives = positives,
                Negatives = negatives
            };

            return View(vm);
        }
    }
}
