using System;
using System.Collections.Generic;

namespace Proyecto_Gaming.Areas.AdminV2.ViewModels
{
    public class ReviewIndexViewModel
    {
        public string Filter { get; set; } = "all";
        public int TotalAll { get; set; }
        public int TotalPositive { get; set; }
        public int TotalNegative { get; set; }

        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }

        public List<ReviewListItem> Items { get; set; } = new();
    }
}
