using System;
using System.Collections.Generic;

namespace Proyecto_Gaming.Areas.AdminV2.ViewModels
{
    public class UsersByGroupItem
    {
        public string GroupName { get; set; } = "";
        public int Count { get; set; }
    }

    public class TopGameItem
    {
        public string Game { get; set; } = "";
        public int Purchases { get; set; }
    }

    public class StatsViewModel
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        public List<UsersByGroupItem> UsersByGroup { get; set; } = new();
        public List<TopGameItem> TopGames { get; set; } = new();
    }
}
