using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MimeKit;

namespace Proyecto_Gaming.Services
{
    public class FileEmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly IHostEnvironment _env;

        public FileEmailService(IConfiguration config, IHostEnvironment env)
        {
            _config = config;
            _env = env;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody, string? fromEmail = null)
        {
            var emailSection = _config.GetSection("Email");
            var defaultFromName  = emailSection["DefaultFromName"]  ?? "Proyecto Gaming";
            var defaultFromEmail = fromEmail ?? emailSection["DefaultFromEmail"] ?? "noreply@gaming.com";
            var outbox = emailSection.GetSection("File")["OutboxFolder"] ?? "EmailsOutbox";

            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(defaultFromName, defaultFromEmail));
            msg.To.Add(MailboxAddress.Parse(toEmail));
            msg.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = htmlBody };
            msg.Body = builder.ToMessageBody();

            var dir = Path.Combine(_env.ContentRootPath, outbox);
            Directory.CreateDirectory(dir);
            var fileName = $"{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}_{toEmail}.eml";
            var fullPath = Path.Combine(dir, fileName);

            await using var fs = File.Create(fullPath);
            await msg.WriteToAsync(fs);
        }
    }
}
