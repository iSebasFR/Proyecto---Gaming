using System.Collections.Generic;
using Proyecto_Gaming.Models.Surveys;

namespace Proyecto_Gaming.Areas.AdminV2.ViewModels.Surveys
{
    public class TakeSurveyVM
    {
        public int SurveyId { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public List<TakeQuestionVM> Questions { get; set; } = new();
    }

    public class TakeQuestionVM
    {
        public int QuestionId { get; set; }
        public string Text { get; set; } = "";
        public QuestionType Type { get; set; }          // enum del modelo

        // Para binding del POST
        public string? OpenAnswer { get; set; }         // para OpenText
        public int? SelectedOptionId { get; set; }      // para YesNo / MultipleChoice

        public List<TakeOptionVM> Options { get; set; } = new();
    }

    public class TakeOptionVM
    {
        public int OptionId { get; set; }
        public string Text { get; set; } = "";
    }
}
