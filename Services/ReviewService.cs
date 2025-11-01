using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Proyecto_Gaming.Data;
using Proyecto_Gaming.Models;                          // ContactMessage
using Proyecto_Gaming.Areas.AdminV2.ViewModels;       // ReviewIndexViewModel / ReviewListItem

namespace Proyecto_Gaming.Services
{
    public interface IReviewService
    {
        Task<ReviewIndexViewModel> GetAsync(string? filter, int page);
    }

    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _db;

        public ReviewService(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <param name="filter">"all" | "positive" | "negative"</param>
        /// <param name="page">>= 1</param>
        public async Task<ReviewIndexViewModel> GetAsync(string? filter, int page)
        {
            filter = (filter ?? "all").Trim().ToLowerInvariant();
            if (filter is not ("all" or "positive" or "negative"))
                filter = "all";

            page = page < 1 ? 1 : page;
            const int pageSize = 20;

            var totalAll      = await _db.ContactMessages.CountAsync();
            var totalPositive = await _db.ContactMessages.CountAsync(c => c.Sentiment == "positive");
            var totalNegative = await _db.ContactMessages.CountAsync(c => c.Sentiment == "negative");

            IQueryable<ContactMessage> query = _db.ContactMessages.AsNoTracking();

            if (filter == "positive")
                query = query.Where(c => c.Sentiment == "positive");
            else if (filter == "negative")
                query = query.Where(c => c.Sentiment == "negative");

            query = query.OrderByDescending(c => c.CreatedAtUtc);

            var totalFiltered = await query.CountAsync();
            var totalPages    = Math.Max(1, (int)Math.Ceiling(totalFiltered / (double)pageSize));
            if (page > totalPages) page = totalPages;
            var skip = (page - 1) * pageSize;

            List<ReviewListItem> items = await query
                .Skip(skip)
                .Take(pageSize)
                .Select(c => new ReviewListItem
                {
                    Id           = c.Id,
                    Name         = c.Name,
                    Email        = c.Email,
                    Message      = c.Message,
                    CreatedAtUtc = c.CreatedAtUtc,
                    Sentiment    = c.Sentiment ?? "unknown",
                    SentimentScore = c.SentimentScore
                })
                .ToListAsync();

            return new ReviewIndexViewModel
            {
                Filter        = filter,
                TotalAll      = totalAll,
                TotalPositive = totalPositive,
                TotalNegative = totalNegative,
                Page          = page,
                PageSize      = pageSize,
                TotalPages    = totalPages,
                Items         = items
            };
        }
    }
}
