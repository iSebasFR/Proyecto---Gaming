using System;
using Proyecto_Gaming.Models.Payment; // si tu Usuario está aquí usa el namespace correcto
using Proyecto_Gaming.Models.Surveys;

namespace Proyecto_Gaming.Models.Surveys
{
    public class UserMedal
    {
        public int Id { get; set; }
        public string UsuarioId { get; set; } = default!;
        public int MedalId { get; set; }
        public DateTime GrantedAtUtc { get; set; } = DateTime.UtcNow;

        public Usuario? Usuario { get; set; }
        public Medal? Medal { get; set; }
    }
}
