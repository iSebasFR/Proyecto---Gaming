using Microsoft.AspNetCore.Mvc;
using Proyecto_Gaming.Services;
using Proyecto_Gaming.ViewModels;

namespace Proyecto_Gaming.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _chatbot;

        public ChatbotController(IChatbotService chatbot)
        {
            _chatbot = chatbot;
        }

        [HttpPost("message")]
        public async Task<IActionResult> PostMessage([FromBody] ChatRequest req)
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Message))
                return BadRequest(new { error = "Message is required" });

            var resp = await _chatbot.GetReplyAsync(req);
            // Always return 200 + structured ChatResponse so the UI can present friendly fallbacks
            return Ok(resp);
        }
    }
}
