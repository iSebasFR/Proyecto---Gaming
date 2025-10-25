using System;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Gaming.Models.Comunidad
{
    public class Evento
    {
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string Titulo { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Descripcion { get; set; }

        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin   { get; set; }

        [MaxLength(30)]
        public string? Categoria { get; set; }  // "Torneo", "Trivia", etc.

        [MaxLength(300)]
        public string? ImagenBanner { get; set; }

        public bool EstaActivo { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
