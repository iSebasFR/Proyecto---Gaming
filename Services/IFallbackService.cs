namespace Proyecto_Gaming.Services
{
    public interface IFallbackService
    {
        /// <summary>
        /// Returns a canned reply when the external LLM is not available.
        /// </summary>
        string GetFallbackReply(string userMessage);
    }
}
