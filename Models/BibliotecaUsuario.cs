// Models/BibliotecaUsuario.cs
using Proyecto_Gaming.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Gaming.Models
{
    public class BibliotecaUsuario
    {
        public int Id { get; set; }

        // IMPORTANTE: el nombre correcto que usamos ahora es UsuarioId
        public string? UsuarioId { get; set; } = default!;

        public int RawgGameId { get; set; }

        public string Estado { get; set; } = default!; // "Pendiente", "Jugando", "Completado"

        public string GameName { get; set; } = default!;
        public string GameImage { get; set; } = default!;

        public string Resena { get; set; } = "";       // puede ser vacío
        public int Calificacion { get; set; }          // 0..10

        public DateTime? FechaCompletado { get; set; }
        public DateTime? FechaResena { get; set; }

        // Navegación (opcional, pero útil)
        public Usuario Usuario { get; set; } = default!;
    }
}