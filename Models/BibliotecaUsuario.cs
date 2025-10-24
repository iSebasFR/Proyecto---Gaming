// Models/BibliotecaUsuario.cs
using Proyecto_Gaming.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proyecto_Gaming.Models
{
    public class BibliotecaUsuario
    {
        public int Id { get; set; }
        public string? UsuarioId { get; set; }
        public int RawgGameId { get; set; }
        public string? Estado { get; set; }
        public string? GameName { get; set; }
        public string? GameImage { get; set; }
        
        // Cambia estas propiedades para que tengan valores por defecto
        public string Resena { get; set; } = string.Empty; // No puede ser null
        public int Calificacion { get; set; } = 0; // Valor por defecto
        public DateTime? FechaCompletado { get; set; }
        public DateTime? FechaResena { get; set; }
        
        // Navigation property
        public Usuario? Usuario { get; set; }
    }
}