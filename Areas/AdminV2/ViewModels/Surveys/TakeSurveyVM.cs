using Proyecto_Gaming.Models.Surveys;

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

    // 👇 Importante: usar el enum, no string
    public QuestionType Type { get; set; }

    // Para POST/binding:
    public string? OpenAnswer { get; set; }
    public int? SelectedOptionId { get; set; }

    public List<TakeOptionVM> Options { get; set; } = new();
}

public class TakeOptionVM
{
    public int OptionId { get; set; }
    public string Text { get; set; } = "";
}
