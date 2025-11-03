using System.Collections.Generic;
using Proyecto_Gaming.Models.Surveys;

namespace Proyecto_Gaming.ViewModels.Surveys
{
    /// <summary>
    /// VM que la vista Responder.cshtml espera como @model.
    /// </summary>
    public class TakeSurveyVM
    {
        public int SurveyId { get; set; }
        public string Title { get; set; } = "";
        public string? Description { get; set; }

        // Preguntas a renderizar en la vista (y a bindear en el POST)
        public List<TakeQuestionVM> Questions { get; set; } = new();
    }

    public class TakeQuestionVM
    {
        public int QuestionId { get; set; }
        public string Text { get; set; } = "";

        // Enum del dominio (Proyecto_Gaming.Models.Surveys.QuestionType)
        public QuestionType Type { get; set; }

        // Campos para binding del POST:
        // - Si Type == OpenText, usar OpenAnswer
        public string? OpenAnswer { get; set; }

        // - Si Type == YesNo o MultipleChoice, usar SelectedOptionId
        public int? SelectedOptionId { get; set; }

        // Opciones para YesNo / MultipleChoice
        public List<TakeOptionVM> Options { get; set; } = new();
    }

    public class TakeOptionVM
    {
        public int OptionId { get; set; }
        public string Text { get; set; } = "";
    }
}
