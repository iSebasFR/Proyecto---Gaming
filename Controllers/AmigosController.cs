using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Proyecto_Gaming.Models;
using Proyecto_Gaming.Data;
using Proyecto_Gaming.ViewModels;
using Proyecto_Gaming.Models.Comunidad;

namespace Proyecto_Gaming.Controllers
{
    public class AmigosController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly ApplicationDbContext _context;

        public AmigosController(UserManager<Usuario> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: Amigos/Index
        public async Task<IActionResult> Index(string seccion = "Todos")
        {
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                TempData["Error"] = "Debes iniciar sesión para gestionar amigos.";
                return RedirectToAction("Login", "Account");
            }

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return NotFound();

            // CORREGIDO: Pasar el ID del usuario actual a la vista
            ViewBag.UsuarioActualId = usuario.Id;

            var viewModel = new AmigosViewModel
            {
                SeccionActiva = seccion
            };

            switch (seccion)
            {
                case "Todos":
                    // CORREGIDO: Mostrar amigos en ambas direcciones
                    var amigosComoUsuario = await _context.Amigos
                        .Include(a => a.AmigoUsuario)
                        .Where(a => a.UsuarioId == usuario.Id && a.Estado == "Aceptado")
                        .ToListAsync();

                    var amigosComoAmigo = await _context.Amigos
                        .Include(a => a.Usuario)
                        .Where(a => a.AmigoId == usuario.Id && a.Estado == "Aceptado")
                        .ToListAsync();

                    // Combinar ambas listas
                    viewModel.Amigos = amigosComoUsuario.Concat(amigosComoAmigo).ToList();
                    break;

                case "Solicitudes":
                    viewModel.SolicitudesPendientes = await _context.Amigos
                        .Include(a => a.Usuario)
                        .Where(a => a.AmigoId == usuario.Id && a.Estado == "Pendiente")
                        .ToListAsync();
                    break;

                case "Conocer":
                    // Usuarios que no son amigos ni tienen solicitudes pendientes
                    var amigosIds = await _context.Amigos
                        .Where(a => (a.UsuarioId == usuario.Id || a.AmigoId == usuario.Id) && a.Estado == "Aceptado")
                        .Select(a => a.UsuarioId == usuario.Id ? a.AmigoId : a.UsuarioId)
                        .Distinct()
                        .ToListAsync();

                    var solicitudesPendientesIds = await _context.Amigos
                        .Where(a => a.UsuarioId == usuario.Id && a.Estado == "Pendiente")
                        .Select(a => a.AmigoId)
                        .Distinct()
                        .ToListAsync();

                    viewModel.UsuariosSugeridos = await _context.Users
                        .Where(u => u.Id != usuario.Id && 
                                   !amigosIds.Contains(u.Id) && 
                                   !solicitudesPendientesIds.Contains(u.Id))
                        .Take(20)
                        .ToListAsync();
                    break;
            }

            return View(viewModel);
        }

        // POST: Amigos/EnviarSolicitud
        [HttpPost]
        public async Task<IActionResult> EnviarSolicitud([FromBody] SolicitudAmigoRequest request)
        {
            if (!User.Identity?.IsAuthenticated ?? false)
                return Json(new { success = false, message = "No autenticado" });

            var usuario = await _userManager.GetUserAsync(User);
            
            // CORREGIDO: Verificar que usuario no sea null
            if (usuario == null)
                return Json(new { success = false, message = "Usuario no encontrado" });

            // CORREGIDO: Verificar que request.AmigoId no sea null o vacío
            if (string.IsNullOrEmpty(request.AmigoId))
                return Json(new { success = false, message = "ID de amigo inválido" });

            var amigo = await _userManager.FindByIdAsync(request.AmigoId);
            if (amigo == null)
                return Json(new { success = false, message = "Usuario no encontrado" });

            // Verificar si ya existe una solicitud en cualquier dirección
            var solicitudExistente = await _context.Amigos
                .FirstOrDefaultAsync(a => (a.UsuarioId == usuario.Id && a.AmigoId == request.AmigoId) ||
                                         (a.UsuarioId == request.AmigoId && a.AmigoId == usuario.Id));

            if (solicitudExistente != null)
            {
                if (solicitudExistente.Estado == "Pendiente")
                    return Json(new { success = false, message = "Ya existe una solicitud pendiente" });
                else
                    return Json(new { success = false, message = "Ya son amigos" });
            }

            var nuevaSolicitud = new Amigo
            {
                UsuarioId = usuario.Id,
                AmigoId = request.AmigoId,
                Estado = "Pendiente",
                FechaSolicitud = DateTime.UtcNow
            };

            _context.Amigos.Add(nuevaSolicitud);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Solicitud enviada" });
        }

        // CORREGIDO: POST: Amigos/AceptarSolicitud - Ahora crea la relación bidireccional
        [HttpPost]
        public async Task<IActionResult> AceptarSolicitud([FromBody] SolicitudAmigoRequest request)
        {
            if (!User.Identity?.IsAuthenticated ?? false)
                return Json(new { success = false, message = "No autenticado" });

            var usuario = await _userManager.GetUserAsync(User);
            
            // CORREGIDO: Verificar que usuario no sea null
            if (usuario == null)
                return Json(new { success = false, message = "Usuario no encontrado" });

            var solicitud = await _context.Amigos
                .FirstOrDefaultAsync(a => a.Id == request.SolicitudId && 
                                        a.AmigoId == usuario.Id && 
                                        a.Estado == "Pendiente");

            if (solicitud == null)
                return Json(new { success = false, message = "Solicitud no encontrada" });

            // 1. Actualizar la solicitud original a "Aceptado"
            solicitud.Estado = "Aceptado";
            solicitud.FechaAceptacion = DateTime.UtcNow;

            // 2. Crear la relación inversa para que ambos usuarios se vean como amigos
            var relacionInversa = new Amigo
            {
                UsuarioId = usuario.Id, // El que acepta
                AmigoId = solicitud.UsuarioId, // El que envió la solicitud
                Estado = "Aceptado",
                FechaSolicitud = DateTime.UtcNow,
                FechaAceptacion = DateTime.UtcNow
            };

            _context.Amigos.Add(relacionInversa);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Solicitud aceptada" });
        }

        // POST: Amigos/RechazarSolicitud
        [HttpPost]
        public async Task<IActionResult> RechazarSolicitud([FromBody] SolicitudAmigoRequest request)
        {
            if (!User.Identity?.IsAuthenticated ?? false)
                return Json(new { success = false, message = "No autenticado" });

            var usuario = await _userManager.GetUserAsync(User);
            
            // CORREGIDO: Verificar que usuario no sea null (aunque no se use directamente, es buena práctica)
            if (usuario == null)
                return Json(new { success = false, message = "Usuario no encontrado" });

            var solicitud = await _context.Amigos
                .FirstOrDefaultAsync(a => a.Id == request.SolicitudId && a.Estado == "Pendiente");

            if (solicitud == null)
                return Json(new { success = false, message = "Solicitud no encontrada" });

            _context.Amigos.Remove(solicitud);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Solicitud rechazada" });
        }

        // CORREGIDO: POST: Amigos/EliminarAmigo - Error de tipos arreglado
        [HttpPost]
        public async Task<IActionResult> EliminarAmigo([FromBody] SolicitudAmigoRequest request)
        {
            if (!User.Identity?.IsAuthenticated ?? false)
                return Json(new { success = false, message = "No autenticado" });

            var usuario = await _userManager.GetUserAsync(User);
            
            // CORREGIDO: Verificar que usuario no sea null
            if (usuario == null)
                return Json(new { success = false, message = "Usuario no encontrado" });

            // CORREGIDO: Buscar por ID de relación o por IDs de usuario
            var relaciones = await _context.Amigos
                .Where(a => (a.Id == request.AmistadIdInt) || // Buscar por ID de relación
                           (a.UsuarioId == usuario.Id && a.AmigoId == request.AmigoId) || // Buscar por usuario actual como remitente
                           (a.AmigoId == usuario.Id && a.UsuarioId == request.AmigoId)) // Buscar por usuario actual como receptor
                .ToListAsync();

            if (!relaciones.Any())
                return Json(new { success = false, message = "Amistad no encontrada" });

            _context.Amigos.RemoveRange(relaciones);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Amigo eliminado" });
        }
    }

    // CORREGIDO: Clase para manejar las solicitudes JSON
    public class SolicitudAmigoRequest
    {
        public string? AmigoId { get; set; }
        public int SolicitudId { get; set; }
        public string? AmistadId { get; set; } // Para cuando se pasa como string desde JavaScript
        
        // Propiedad adicional para manejar el ID como entero
        public int AmistadIdInt 
        { 
            get 
            { 
                return int.TryParse(AmistadId, out int result) ? result : 0; 
            } 
        }
    }
}