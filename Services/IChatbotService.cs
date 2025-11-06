using Proyecto_Gaming.ViewModels;

namespace Proyecto_Gaming.Services
{
    public interface IChatbotService
    {
        Task<ChatResponse> GetReplyAsync(ChatRequest request);
    }
}
