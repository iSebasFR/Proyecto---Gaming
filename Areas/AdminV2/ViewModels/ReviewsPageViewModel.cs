using System;
using System.Collections.Generic;

namespace Proyecto_Gaming.Areas.AdminV2.ViewModels
{
    public class ReviewListItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime CreatedAtUtc { get; set; }
        // "positive" | "negative" | "unknown"
        public string Sentiment { get; set; } = "unknown";
        public float? SentimentScore { get; set; }
    }

    public class ReviewsPageViewModel
    {
        public IEnumerable<ReviewListItem>? Positives { get; set; }
        public IEnumerable<ReviewListItem>? Negatives { get; set; }
    }
}
