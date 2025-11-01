using System;
using System.Linq;

namespace Proyecto_Gaming.Services
{
    public interface ISentimentService
    {
        // Ahora devuelve label como string ("positive" | "negative" | "neutral")
        (string label, float score) Classify(string text);
    }

    // Stub simple (luego podemos reemplazar por ML.NET)
    public class SimpleSentimentService : ISentimentService
    {
        static readonly string[] POS = {
            "gracias","bueno","excelente","me encanta","genial","rápido",
            "recomiendo","encantado","top","perfecto"
        };

        static readonly string[] NEG = {
            "mal","malo","terrible","lento","error","no funciona",
            "odio","reclamo","decepcion","pésimo","pesimo"
        };

        public (string label, float score) Classify(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return ("neutral", 0f);

            var t = text.ToLowerInvariant();
            int p = POS.Count(w => t.Contains(w));
            int n = NEG.Count(w => t.Contains(w));

            if (p == 0 && n == 0) return ("neutral", 0.5f);
            if (p >= n) return ("positive", Math.Clamp(p / 5f, 0f, 1f));
            return ("negative", Math.Clamp(n / 5f, 0f, 1f));
        }
    }
}
