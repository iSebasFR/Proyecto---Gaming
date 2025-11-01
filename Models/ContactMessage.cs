using System;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Gaming.Models
{
    public class ContactMessage
    {
        public int Id { get; set; }

        [MaxLength(120)]
        public string? Name { get; set; }

        [MaxLength(160)]
        public string? Email { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // ✅ Ahora es string (para ML o texto manual)
        // valores esperados: "positive" | "negative" | "neutral" | null
        [MaxLength(20)]
        public string? Sentiment { get; set; } = null;

        public float? SentimentScore { get; set; } // 0..1
    }
}
