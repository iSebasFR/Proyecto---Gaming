using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Proyecto_Gaming.Models; // ✅ AGREGAR ESTA LÍNEA

namespace Proyecto_Gaming.Models.Payment
{
    public class Transaction
    {
        public int Id { get; set; }
        
        [Required]
        public string UsuarioId { get; set; }
        
        [Required]
        public int GameId { get; set; }
        
        [Required]
        public string GameTitle { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        [Required]
        public string Currency { get; set; } = "USD";
        
        [Required]
        public string PaymentStatus { get; set; } = "Pending";
        
        [Required]
        public string PaymentProvider { get; set; } = "Stripe";
        
        // ✅ CAMBIAR A NULLABLE - Solo se llena cuando se completa el pago
        public string? TransactionId { get; set; }
        
        [Required]
        public string SessionId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // ✅ CAMBIAR A NULLABLE - Solo se llena cuando se completa
        public DateTime? CompletedAt { get; set; }
        
        // Navigation property
        public virtual Usuario Usuario { get; set; }
    }
}