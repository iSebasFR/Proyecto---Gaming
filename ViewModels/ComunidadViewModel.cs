using System.ComponentModel.DataAnnotations;
using Proyecto_Gaming.Models;
using Proyecto_Gaming.Models.Comunidad;

namespace Proyecto_Gaming.ViewModels
{
    public class ComunidadViewModel
    {
        // Estadísticas para el Feed
        public int UsuariosConectados { get; set; }
        public int GruposActivos { get; set; }
        public int PublicacionesMensuales { get; set; }
        public int UsuariosSatisfechos { get; set; }
    }

    public class AmigosViewModel
    {
        public List<Amigo> Amigos { get; set; } = new List<Amigo>();
        public List<Amigo> SolicitudesPendientes { get; set; } = new List<Amigo>();
        public List<Usuario> UsuariosSugeridos { get; set; } = new List<Usuario>();
        public string SeccionActiva { get; set; } = "Todos";
    }

    public class GruposViewModel
    {
        public List<Grupo> GruposRecomendados { get; set; } = new List<Grupo>();
        public List<Grupo> MisGrupos { get; set; } = new List<Grupo>();
        public List<Grupo> TodosLosGrupos { get; set; } = new List<Grupo>();
        public Grupo GrupoSeleccionado { get; set; }
        public string VistaActiva { get; set; } = "Recomendados";
    }

    public class DetalleGrupoViewModel
    {
        public Grupo Grupo { get; set; }
        public List<Grupo> MisGrupos { get; set; } = new List<Grupo>(); // NUEVO: Para el sidebar
        public List<MiembroGrupo> Miembros { get; set; } = new List<MiembroGrupo>();
        public List<PublicacionGrupo> Publicaciones { get; set; } = new List<PublicacionGrupo>();
        public List<MultimediaGrupo> Multimedia { get; set; } = new List<MultimediaGrupo>();
        public bool EsAdministrador { get; set; }
        public bool EsMiembro { get; set; }
    }

    public class CrearGrupoViewModel
    {
        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        [StringLength(500)]
        public string Descripcion { get; set; }

        public IFormFile FotoGrupo { get; set; }
        public IFormFile BannerGrupo { get; set; }

        [Required]
        public string Categoria { get; set; }

        public bool EsPublico { get; set; } = true;
    }

    public class ConfiguracionGrupoViewModel
    {
        public int GrupoId { get; set; }
        
        [Required(ErrorMessage = "El nombre del grupo es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La descripción no puede tener más de 500 caracteres")]
        public string Descripcion { get; set; } = string.Empty;

        public IFormFile? FotoGrupo { get; set; }

        public IFormFile? BannerGrupo { get; set; }

        [Required(ErrorMessage = "La categoría es requerida")]
        public string Categoria { get; set; } = string.Empty;

        public bool EsPublico { get; set; } = true;
        
        // SOLO PARA VISUALIZACIÓN - NO DEBEN SER VALIDADAS
        public string? FotoGrupoActual { get; set; }
        public string? BannerGrupoActual { get; set; }
        
        public List<MiembroGrupo> Miembros { get; set; } = new List<MiembroGrupo>();
    }

    public class GestionMiembroRequest
    {
        public int GrupoId { get; set; }
        public string UsuarioId { get; set; }
    }

    public class MultimediaViewModel
    {
        public int GrupoId { get; set; }
        public IFormFile Archivo { get; set; }
        public string Descripcion { get; set; }
        public string TipoArchivo { get; set; }
    }

    public class ComentarioMultimediaViewModel
    {
        public int MultimediaId { get; set; }
        public string Contenido { get; set; }
    }

    public class ReaccionViewModel
    {
        public int MultimediaId { get; set; }
        public string TipoReaccion { get; set; } // "like", "love", "wow", "sad", "angry"
    }
}