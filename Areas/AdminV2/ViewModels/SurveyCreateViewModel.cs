using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Proyecto_Gaming.Models.Surveys;

namespace Proyecto_Gaming.Areas.AdminV2.ViewModels
{
    public class SurveyCreateViewModel
    {
        [Required, MaxLength(160)]
        public string Title { get; set; } = "";

        public string? Description { get; set; }

        [Display(Name = "Inicio (UTC)")]
        [DataType(DataType.DateTime)]
        public DateTime StartDateUtc { get; set; } = DateTime.UtcNow;

        [Display(Name = "Fin (UTC)")]
        [DataType(DataType.DateTime)]
        public DateTime EndDateUtc { get; set; } = DateTime.UtcNow.AddDays(7);

        [Display(Name = "Medalla (recompensa)")]
        public int? MedalId { get; set; }

        public List<QuestionVM> Questions { get; set; } = new();
        public List<MedalItemVM> Medals { get; set; } = new();

        public class MedalItemVM
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }

        public class QuestionVM
        {
            [Required] public string Text { get; set; } = "";
            public QuestionType Type { get; set; } = QuestionType.MultipleChoice;
            public List<OptionVM> Options { get; set; } = new();
        }

        public class OptionVM
        {
            [Required] public string Text { get; set; } = "";
        }
    }
}
