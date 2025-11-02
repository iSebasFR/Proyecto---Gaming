using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Gaming.Models.Surveys
{
    public class SurveyAnswer
    {
        public int Id { get; set; }

        // FK
        public int SurveyResponseId { get; set; }
        public int SurveyQuestionId { get; set; }

        // Para preguntas abiertas
         public string? AnswerText { get; set; }

        // Para opción única
        public int? SelectedOptionId { get; set; }

        // Navs
        public SurveyResponse Response { get; set; } = null!;
        public SurveyQuestion Question { get; set; } = null!;
        public SurveyOption? SelectedOption { get; set; }
    }
}
