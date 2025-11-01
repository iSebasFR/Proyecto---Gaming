using System.Collections.Generic;

namespace Proyecto_Gaming.Models.Surveys
{
    public enum QuestionType
    {
        MultipleChoice = 0,   // una respuesta entre varias
        YesNo = 1,            // Sí / No
        OpenText = 2          // respuesta libre
    }

    public class SurveyQuestion
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public Survey Survey { get; set; } = null!;

        public string Text { get; set; } = "";
        public QuestionType Type { get; set; }
        public int Order { get; set; } = 0;

        public ICollection<SurveyOption> Options { get; set; } = new List<SurveyOption>();
    }
}
