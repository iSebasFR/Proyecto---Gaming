using Microsoft.AspNetCore.Mvc;
using Proyecto_Gaming.Services;
using Proyecto_Gaming.ViewModels;

// ✅ NUEVO
using Proyecto_Gaming.Data;
using Proyecto_Gaming.Models;
using Microsoft.Extensions.Configuration;

namespace Proyecto_Gaming.Controllers
{
    public class ContactController : Controller
    {
        private readonly IEmailService _email;

        // ✅ NUEVO
        private readonly ApplicationDbContext _db;
        private readonly ISentimentService _sentiment;

        // ANTES:
        // public ContactController(IEmailService email)
        // {
        //     _email = email;
        // }

        // ✅ DESPUÉS (inyectamos DbContext y Sentiment)
        public ContactController(IEmailService email, ApplicationDbContext db, ISentimentService sentiment)
        {
            _email = email;
            _db = db;                 // ✅ NUEVO
            _sentiment = sentiment;   // ✅ NUEVO
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(ContactFormViewModel vm)
        {
            // ✅ NUEVO: referer seguro (por si viene vacío)
            var referer = Request.Headers["Referer"].ToString();
            if (string.IsNullOrWhiteSpace(referer))
                referer = Url.Action("Index", "Home")!;

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Por favor corrige los errores del formulario.";
                // ANTES: return Redirect(Request.Headers["Referer"].ToString());
                return Redirect(referer); // ✅ NUEVO (seguro si no hay Referer)
            }

            // Email al administrador del sitio
            var adminTo = HttpContext.RequestServices
                          .GetRequiredService<IConfiguration>()["Email:AdminTo"]
                          ?? "admin@example.com";

            var asunto = string.IsNullOrWhiteSpace(vm.Asunto)
                ? "Nuevo mensaje de contacto"
                : vm.Asunto!.Trim();

            var html = $@"
                <h2>Nuevo mensaje desde GAMING</h2>
                <p><strong>Nombre:</strong> {System.Net.WebUtility.HtmlEncode(vm.Nombre)}</p>
                <p><strong>Email:</strong> {System.Net.WebUtility.HtmlEncode(vm.Email)}</p>
                <p><strong>Asunto:</strong> {System.Net.WebUtility.HtmlEncode(asunto)}</p>
                <p><strong>Mensaje:</strong><br/>{System.Net.WebUtility.HtmlEncode(vm.Mensaje).Replace("\n","<br/>")}</p>
                <hr/>
                <small>Enviado el {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</small>";

            // ✅ NUEVO: clasificar sentimiento + guardar en BD ANTES del envío de email
            try
            {
                var (label, score) = _sentiment.Classify(vm.Mensaje ?? string.Empty);

                _db.ContactMessages.Add(new ContactMessage
                {
                    Name = vm.Nombre,
                    Email = vm.Email,
                    Message = vm.Mensaje ?? string.Empty,
                    CreatedAtUtc = DateTime.UtcNow,
                    Sentiment = label,
                    SentimentScore = score
                });
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // No romper el flujo si falla el guardado: registramos y continuamos con el email
                Console.WriteLine("Error guardando contacto/reseña: " + ex.Message);
            }

            try
            {
                await _email.SendAsync(adminTo, $"[Contacto] {asunto}", html, vm.Email);

                // (Opcional) Autorespuesta breve al usuario:
                var autoHtml = $@"
                    <p>Hola {System.Net.WebUtility.HtmlEncode(vm.Nombre)},</p>
                    <p>Hemos recibido tu mensaje y te responderemos pronto.</p>
                    <p><em>Resumen:</em></p>
                    <blockquote>{System.Net.WebUtility.HtmlEncode(vm.Mensaje).Replace("\n","<br/>")}</blockquote>
                    <hr/><small>GAMING</small>";
                await _email.SendAsync(vm.Email, "Hemos recibido tu mensaje", autoHtml);

                TempData["Ok"] = "¡Gracias! Tu mensaje fue enviado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudo enviar el mensaje. Intenta más tarde.";
                Console.WriteLine("Error envío contacto: " + ex.Message);
            }

            // ANTES: return Redirect(Request.Headers["Referer"].ToString());
            return Redirect(referer); // ✅ NUEVO (seguro si no hay Referer)
        }
    }
}
