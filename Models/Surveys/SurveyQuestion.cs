using System.Collections.Generic;

namespace Proyecto_Gaming.Models.Surveys
{
    public class SurveyQuestion
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public Survey Survey { get; set; } = null!;

        public string Text { get; set; } = "";
        public QuestionType Type { get; set; }
        public int Order { get; set; } = 0;

        public ICollection<SurveyOption> Options { get; set; } = new List<SurveyOption>();

        // ➜ AGREGA ESTO:
        public ICollection<SurveyAnswer> Answers { get; set; } = new List<SurveyAnswer>();
    }
}
