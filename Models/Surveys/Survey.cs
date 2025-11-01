using System;
using System.Collections.Generic;

namespace Proyecto_Gaming.Models.Surveys
{
    public class Survey
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public DateTime StartDateUtc { get; set; }
        public DateTime EndDateUtc { get; set; }

        // Recompensa
        public int? MedalId { get; set; }
        public Medal? Medal { get; set; }

        public ICollection<SurveyQuestion> Questions { get; set; } = new List<SurveyQuestion>();
    }
}
