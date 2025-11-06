using System;
using Proyecto_Gaming.ViewModels;

namespace Proyecto_Gaming.Services
{
    /// <summary>
    /// Servicio obsoleto mantenido únicamente para compatibilidad. Redirige al fallback local.
    /// </summary>
    [Obsolete("HuggingFaceChatService ha sido reemplazado por OllamaChatService.")]
    public class HuggingFaceChatService : IChatbotService
    {
        private readonly IFallbackService _fallback;

        public HuggingFaceChatService(IFallbackService fallback)
        {
            _fallback = fallback;
        }

        public Task<ChatResponse> GetReplyAsync(ChatRequest request)
        {
            var reply = _fallback.GetFallbackReply(request.Message);
            return Task.FromResult(new ChatResponse
            {
                IsSuccess = true,
                Reply = reply,
                Source = "local",
                Error = "Hugging Face ya no está configurado. Usa OllamaChatService."
            });
        }
    }
}
