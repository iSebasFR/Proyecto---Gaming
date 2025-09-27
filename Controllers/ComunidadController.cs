using Microsoft.AspNetCore.Mvc;

namespace TuProyecto.Controllers
{
    public class ComunidadController : Controller
    {
        public IActionResult Index() => View();
        public IActionResult Feed() => View();
        public IActionResult Amigos() => View();
        public IActionResult Grupos() => View();
        public IActionResult MisGrupos() => View();
        public IActionResult MisGruposChat() => View();
        public IActionResult MisGruposMiembros() => View();
        public IActionResult MisGruposMultimedia() => View();
        public IActionResult CrearGrupo() => View();
        public IActionResult ConfigGrupo() => View();
    }
}
