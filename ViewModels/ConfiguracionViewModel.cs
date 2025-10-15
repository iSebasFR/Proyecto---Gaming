using System.ComponentModel.DataAnnotations;

namespace Proyecto_Gaming.ViewModels
{
    public class ConfiguracionViewModel
    {
        // Información Personal
        [Required(ErrorMessage = "El nombre para mostrar es obligatorio")]
        [Display(Name = "Nombre para mostrar")]
        [StringLength(50, ErrorMessage = "El nombre no puede exceder 50 caracteres")]
        public string? UserName { get; set; }  // Ahora representa el DisplayName

        [Display(Name = "Nombre real")]
        [StringLength(100, ErrorMessage = "El nombre real no puede exceder 100 caracteres")]
        public string? NombreReal { get; set; }

        [Display(Name = "Correo electrónico")]
        public string? Email { get; set; }

        [Display(Name = "Fecha de nacimiento")]
        [DataType(DataType.Date)]
        public DateTime? FechaNacimiento { get; set; }

        [Display(Name = "Biografía")]
        [StringLength(500, ErrorMessage = "La biografía no puede exceder 500 caracteres")]
        public string? Biografia { get; set; }

        [Display(Name = "País")]
        public string? Pais { get; set; }

        // Avatar
        [Display(Name = "Foto de perfil")]
        public IFormFile? FotoPerfil { get; set; }
        public string? FotoPerfilUrl { get; set; }

        // Plataformas preferidas
        [Display(Name = "Plataformas preferidas")]
        public List<PlataformaPreferida> PlataformasPreferidas { get; set; } = new List<PlataformaPreferida>();

        // Seguridad
        [Display(Name = "Contraseña actual")]
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [Display(Name = "Nueva contraseña")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "La contraseña debe tener al menos {2} caracteres.", MinimumLength = 6)]
        public string? NewPassword { get; set; }

        [Display(Name = "Confirmar nueva contraseña")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden.")]
        public string? ConfirmPassword { get; set; }
    }

    public class PlataformaPreferida
    {
        public string? Id { get; set; }
        public string? Nombre { get; set; }
        public bool Seleccionada { get; set; }
    }
}