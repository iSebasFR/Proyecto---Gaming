using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;

namespace Proyecto_Gaming.Models
{
    public class Juego
    {
        public int IdJuego { get; set; }
        public string Nombre { get; set; }
        public string Categoria { get; set; }
        public string Plataforma { get; set; }
        public string? Imagen { get; set; }
        public decimal? PuntuacionMedia { get; set; }

        // Relación con BibliotecaUsuario
        public ICollection<BibliotecaUsuario> BibliotecaUsuarios { get; set; }
    }
}
