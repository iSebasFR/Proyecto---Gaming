namespace Proyecto_Gaming.ViewModels.Surveys
{
    using System.Collections.Generic;
    using Proyecto_Gaming.Models.Surveys;

    public class TakeSurveyVM
    {
        public int SurveyId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<TakeSurveyQuestionVM> Questions { get; set; } = new();
    }

    public class TakeSurveyQuestionVM
    {
        public int QuestionId { get; set; }
        public string Text { get; set; } = string.Empty;
        public QuestionType Type { get; set; }
        public List<TakeSurveyOptionVM> Options { get; set; } = new();

        // respuestas
        public int? SelectedOptionId { get; set; }
        public string? OpenAnswer { get; set; }
    }

    public class TakeSurveyOptionVM
    {
        public int OptionId { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
