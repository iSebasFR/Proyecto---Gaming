using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Proyecto_Gaming.Services
{
    public class FallbackService : IFallbackService
    {
        private static readonly string[] _helloKeywords =
            { "hola", "buenas", "buen dia", "buen día", "buenas noches", "saludos" };

        private static readonly Dictionary<string, string> _genreRecommendations = new()
        {
            ["accion"] = "Para acción frenética prueba 'DOOM Eternal', 'Devil May Cry 5' o 'Hades'." ,
            ["acción"] = "Para acción frenética prueba 'DOOM Eternal', 'Devil May Cry 5' o 'Hades'." ,
            ["aventura"] = "Si buscas aventuras narrativas te encantarán 'The Legend of Zelda: Tears of the Kingdom', 'Ori and the Will of the Wisps' o 'Life is Strange'.", 
            ["rpg"] = "Para RPG inmersivos puedes probar 'The Witcher 3', 'Final Fantasy XVI' o 'Persona 5 Royal'.",
            ["rol"] = "Para RPG inmersivos puedes probar 'The Witcher 3', 'Final Fantasy XVI' o 'Persona 5 Royal'.",
            ["terror"] = "Si te gusta el terror, intenta con 'Resident Evil Village', 'Outlast 2' o 'Alan Wake 2'.",
            ["estrateg"] = "En estrategia destacan 'Civilization VI', 'XCOM 2' y 'Age of Empires IV'.",
            ["cooper"] = "Para cooperativo local o en línea: 'It Takes Two', 'Overcooked! 2' y 'Deep Rock Galactic'.",
            ["familia"] = "Para jugar en familia prueba 'Mario Kart 8 Deluxe', 'Minecraft' o 'Stardew Valley'."
        };

        public string GetFallbackReply(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                return "¡Hola! Soy el asistente de Gaming. ¿Quieres recomendaciones de juegos, ayuda con tu cuenta o soporte?";

            var original = userMessage.Trim();
            var normalized = original.ToLower(CultureInfo.InvariantCulture);

            if (ContainsAny(normalized, _helloKeywords) || normalized == "hola" || normalized == "hi")
            {
                return "¡Hola! Soy el asistente de Gaming. Puedo recomendar juegos, ayudarte a encontrar ofertas, resolver dudas sobre tu cuenta y guiarte en la plataforma.";
            }

            if (normalized.Contains("que dia") || normalized.Contains("qué dia") || normalized.Contains("qué día") || normalized.Contains("que dia es hoy") || normalized.Contains("qué día es hoy") || normalized.Contains("fecha de hoy"))
            {
                var culture = new CultureInfo("es-ES");
                var today = DateTime.Now.ToString("dddd d 'de' MMMM 'de' yyyy", culture);
                var formatted = culture.TextInfo.ToTitleCase(today);
                return $"Hoy es {formatted}. ¿Quieres que te recuerde eventos o retos disponibles hoy?";
            }

            if (normalized.Contains("que hora") || normalized.Contains("qué hora") || normalized.Contains("hora actual") || normalized.Contains("qué hora es"))
            {
                var culture = new CultureInfo("es-ES");
                var time = DateTime.Now.ToString("HH:mm", culture);
                return $"Son las {time} según tu servidor local. ¿Necesitas planificar alguna sesión o evento?";
            }

            if (ContainsAll(normalized, "que", "puedes", "hacer") || normalized.Contains("que sabes") || normalized.Contains("como funcionas"))
            {
                return "Puedo recomendar juegos por género o modo, mostrarte ofertas activas, guiarte en la biblioteca, ayudarte con pagos o recordarte eventos en la comunidad. ¿Qué necesitas?";
            }

            if ((normalized.Contains("recomiendame") || normalized.Contains("recomiéndame") || normalized.Contains("recomendame") || normalized.Contains("recomendar"))
                || (normalized.Contains("recom") && (normalized.Contains("juego") || normalized.Contains("juegos") || normalized.Contains("game"))))
            {
                return "Aquí tienes una mezcla popular: 'The Legend of Zelda: Tears of the Kingdom' para aventura, 'Hades' para acción roguelike y 'Stardew Valley' si quieres algo relajado. ¿Buscas por género, plataforma o modo cooperativo?";
            }

            if (ContainsAll(normalized, "recom", "coop"))
            {
                return "Para cooperativos te recomiendo 'It Takes Two', 'Overcooked! 2' y 'Deep Rock Galactic'. ¿Buscas algo para PC, consola o móvil?";
            }

            foreach (var entry in _genreRecommendations)
            {
                if (normalized.Contains(entry.Key))
                {
                    return entry.Value + " ¿Quieres más sugerencias o filtrar por plataforma?";
                }
            }

            if (ContainsAny(normalized, "oferta", "descuento", "rebaja", "precio"))
            {
                return "Puedes abrir la sección de Ofertas para ver descuentos diarios. Si guardas un juego en tu lista de deseados te avisaremos cuando baje de precio.";
            }

            if (ContainsAny(normalized, "registr", "crear cuenta", "cuenta nueva", "inscrib"))
            {
                return "Puedes crear una cuenta desde el botón Registrarse en la esquina superior derecha. Solo necesitas un correo y una contraseña segura; luego podrás completar tu perfil y habilitar la autenticación en dos pasos desde Configuración.";
            }

            if (ContainsAny(normalized, "como compro", "cómo compro", "comprar", "compra juegos", "adquirir juego"))
            {
                return "Para comprar un juego abre su ficha, pulsa Comprar y elige tarjeta, PayPal o saldo de la wallet. Tras confirmar el pago, el juego aparecerá al instante en tu biblioteca listo para descargar.";
            }

            if (ContainsAny(normalized, "cuenta", "contraseña", "login", "ingresar"))
            {
                return "Si tienes problemas con tu cuenta, revisa Configuración > Seguridad para restablecer la contraseña o activar autenticación en dos pasos.";
            }

            if (ContainsAny(normalized, "pago", "tarjeta", "suscrip", "factura"))
            {
                return "Aceptamos tarjetas de crédito, débito y PayPal. En la sección Pagos puedes revisar el historial y descargar comprobantes.";
            }

            if (ContainsAny(normalized, "soporte", "ayuda", "contacto", "problema"))
            {
                return "Puedes crear un ticket desde la sección Contacto o escribirnos a soporte@proyectogaming.com. También estoy aquí para dudas rápidas.";
            }

            if (ContainsAny(normalized, "biblioteca", "pendiente", "colecci"))
            {
                return "En tu biblioteca puedes marcar juegos como completados, pendientes o favoritos. ¿Quieres que filtre por género o progreso?";
            }

            if (ContainsAny(normalized, "evento", "torneo", "comunidad", "grupo"))
            {
                return "La sección Comunidad muestra eventos y torneos activos. Puedes unirte a grupos, ver calendarios y recibir recordatorios.";
            }

            if (ContainsAny(normalized, "chatbot", "ia", "inteligencia artificial"))
            {
                return "Estoy funcionando en modo asistente local. Puedo ayudarte con recomendaciones, ofertas, soporte básico y guías de la plataforma.";
            }

            if (ContainsAll(normalized, "todo", "bien") || normalized.Contains("que tal") || normalized.Contains("como estas"))
            {
                return "¡Todo en orden por aquí! ¿Quieres que te recomiende juegos, revisar ofertas o necesitas soporte con algo de la cuenta?";
            }

            if (normalized.Contains("gracias"))
            {
                return "¡De nada! Si necesitas algo más, solo dime.";
            }

            return BuildDefaultReply(original);
        }

        private static bool ContainsAny(string normalized, params string[] keywords) =>
            keywords.Any(keyword => normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        private static bool ContainsAll(string normalized, params string[] keywords) =>
            keywords.All(keyword => normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        private static string BuildDefaultReply(string original)
        {
            if (string.IsNullOrWhiteSpace(original))
            {
                return "¿Sobre qué tema quieres hablar? Puedo ayudarte con recomendaciones, ofertas, soporte y más.";
            }

            return "Aún no tengo una respuesta específica para eso, pero puedo ayudarte con recomendaciones de juegos, ofertas, soporte de cuenta o eventos de la comunidad. ¿Te interesa alguno de esos temas?";
        }
    }
}
