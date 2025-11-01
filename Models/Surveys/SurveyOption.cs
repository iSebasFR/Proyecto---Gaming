namespace Proyecto_Gaming.Models.Surveys
{
    public class SurveyOption
    {
        public int Id { get; set; }
        public int SurveyQuestionId { get; set; }
        public SurveyQuestion Question { get; set; } = null!;
        public string Text { get; set; } = "";
        public int Order { get; set; } = 0;
    }
}
