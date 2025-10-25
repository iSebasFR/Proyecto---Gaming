using System.ComponentModel.DataAnnotations;

namespace Proyecto_Gaming.Models
{
    public class AdminLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Action { get; set; } = string.Empty;

        public string? TargetUser { get; set; }

        public string? PerformedBy { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
