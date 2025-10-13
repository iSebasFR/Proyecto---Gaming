using System.Threading.Tasks;

namespace Proyecto_Gaming.Services
{
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string subject, string htmlBody, string? fromEmail = null);
    }
}
