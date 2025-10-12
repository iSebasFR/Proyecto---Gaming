using System.ComponentModel.DataAnnotations;

namespace Proyecto_Gaming.ViewModels
{
    public class ContactFormViewModel
    {
        [Required(ErrorMessage = "Tu nombre es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; } = default!;

        [Required(ErrorMessage = "Tu email es obligatorio")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; } = default!;

        [StringLength(150)]
        public string? Asunto { get; set; }

        [Required(ErrorMessage = "Por favor, escribe tu mensaje")]
        [StringLength(2000, ErrorMessage = "Máximo 2000 caracteres")]
        public string Mensaje { get; set; } = default!;
    }
}
