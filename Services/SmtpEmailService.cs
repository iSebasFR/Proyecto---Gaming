using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System;

namespace Proyecto_Gaming.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public SmtpEmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody, string? fromEmail = null)
        {
            // Lee desde appsettings.json (sección SmtpSettings + Email opcional)
            var fromName   = _config["Email:DefaultFromName"]  ?? "Proyecto Gaming";
            var fromAddr   = fromEmail ?? _config["Email:DefaultFromEmail"] ?? _config["SmtpSettings:User"];

            var host       = _config["SmtpSettings:Host"]      ?? "smtp.gmail.com";
            var portOk     = int.TryParse(_config["SmtpSettings:Port"], out var port);
            if (!portOk) port = 587;
            var enableSsl  = bool.TryParse(_config["SmtpSettings:EnableSsl"], out var ssl) ? ssl : true;
            var user       = _config["SmtpSettings:User"];
            var pass       = _config["SmtpSettings:Password"];

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromAddr));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            // 587 => StartTLS, 465 => SSL directo
            var socketOptions = (enableSsl && port == 465)
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            try
            {
                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(host, port, socketOptions);

                if (!string.IsNullOrWhiteSpace(user))
                    await smtp.AuthenticateAsync(user, pass);

                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Log claro para diagnosticar si algo falla (credenciales, bloqueo, etc.)
                Console.WriteLine($"[SMTP ERROR] {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw; // vuelve a lanzar para que el controlador muestre el mensaje de error
            }
        }
    }
}
