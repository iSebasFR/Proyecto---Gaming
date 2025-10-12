using Microsoft.AspNetCore.Mvc;
using Proyecto_Gaming.Services;
using Proyecto_Gaming.ViewModels;

namespace Proyecto_Gaming.Controllers
{
    public class ContactController : Controller
    {
        private readonly IEmailService _email;

        public ContactController(IEmailService email)
        {
            _email = email;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(ContactFormViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Por favor corrige los errores del formulario.";
                return Redirect(Request.Headers["Referer"].ToString());
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

            return Redirect(Request.Headers["Referer"].ToString());
        }
    }
}
