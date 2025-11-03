using System;
using System.Collections.Generic;

namespace Proyecto_Gaming.ViewModels.Perfil
{
    public class PerfilUsuarioVM
    {
        public string UsuarioId { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string? FotoPerfil { get; set; }

        // Lista de medallas del usuario
        public List<MedallaVM> Medallas { get; set; } = new();

        // Útil para la vista (no rompe nada si no lo usas)
        public int CantidadMedallas => Medallas.Count;

        public class MedallaVM
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = "";

            // Evitamos nulls en la vista
            public string IconoUrl { get; set; } = "";

            // Puntos como entero no-null para simplificar la UI
            public int Points { get; set; }

            // Opcional por si quieres mostrar “obtenida el …”
            public DateTime? GrantedAtUtc { get; set; }
        }
    }
}
