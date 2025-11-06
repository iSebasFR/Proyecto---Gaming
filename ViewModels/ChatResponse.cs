namespace Proyecto_Gaming.ViewModels
{
    public class ChatResponse
    {
        public bool IsSuccess { get; set; }
        public string Reply { get; set; } = string.Empty;
    public string Source { get; set; } = "local";
        public string? Error { get; set; }
    }
}
