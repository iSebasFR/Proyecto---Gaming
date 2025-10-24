using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Proyecto_Gaming.Models;
using Proyecto_Gaming.Data;
using Proyecto_Gaming.ViewModels;
using Proyecto_Gaming.Models.Comunidad;

namespace Proyecto_Gaming.Controllers
{
    public class GruposController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public GruposController(UserManager<Usuario> userManager, 
                              ApplicationDbContext context,
                              IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _context = context;
            _environment = environment;
        }

        // GET: Grupos/Index
        public async Task<IActionResult> Index(string vista = "Recomendados")
        {
            // CORREGIDO: Verificación de autenticación mejorada
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                TempData["Error"] = "Debes iniciar sesión para ver grupos.";
                return RedirectToAction("Login", "Account");
            }

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return NotFound();

            var viewModel = new GruposViewModel
            {
                VistaActiva = vista
            };

            // Obtener IDs de grupos donde el usuario ya es miembro
            var misGruposIds = await _context.MiembrosGrupo
                .Where(m => m.UsuarioId == usuario.Id)
                .Select(m => m.GrupoId)
                .ToListAsync();

            switch (vista)
            {
                case "Recomendados":
                    // CORREGIDO: Excluir grupos donde el usuario ya es miembro
                    viewModel.GruposRecomendados = await _context.Grupos
                        .Include(g => g.Creador)
                        .Include(g => g.Miembros)
                        .Where(g => g.EsPublico && !misGruposIds.Contains(g.Id)) // EXCLUIR GRUPOS DEL USUARIO
                        .OrderByDescending(g => g.FechaCreacion)
                        .Take(12)
                        .ToListAsync();
                    break;

                case "MisGrupos":
                    viewModel.MisGrupos = await _context.Grupos
                        .Include(g => g.Creador)
                        .Include(g => g.Miembros)
                        .Where(g => misGruposIds.Contains(g.Id)) // SOLO GRUPOS DEL USUARIO
                        .ToListAsync();
                    break;

                case "Todos":
                    // CORREGIDO: Excluir grupos donde el usuario ya es miembro
                    viewModel.TodosLosGrupos = await _context.Grupos
                        .Include(g => g.Creador)
                        .Include(g => g.Miembros)
                        .Where(g => g.EsPublico && !misGruposIds.Contains(g.Id)) // EXCLUIR GRUPOS DEL USUARIO
                        .OrderByDescending(g => g.FechaCreacion)
                        .ToListAsync();
                    break;
            }

            return View(viewModel);
        }

        // GET: Grupos/Crear
        public IActionResult Crear()
        {
            // CORREGIDO: Verificación de autenticación mejorada
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                TempData["Error"] = "Debes iniciar sesión para crear un grupo.";
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        // POST: Grupos/Crear
        [HttpPost]
        public async Task<IActionResult> Crear(CrearGrupoViewModel model)
        {
            // CORREGIDO: Verificación de autenticación mejorada
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                TempData["Error"] = "Debes iniciar sesión para crear un grupo.";
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = await _userManager.GetUserAsync(User);
            
            // CORREGIDO: Verificar que usuario no sea null
            if (usuario == null)
            {
                TempData["Error"] = "No se pudo identificar al usuario.";
                return View(model);
            }

            // Guardar imágenes si se proporcionaron
            string? fotoGrupoPath = null;
            string? bannerGrupoPath = null;

            if (model.FotoGrupo != null)
            {
                fotoGrupoPath = await GuardarArchivo(model.FotoGrupo, "grupos/fotos");
            }

            if (model.BannerGrupo != null)
            {
                bannerGrupoPath = await GuardarArchivo(model.BannerGrupo, "grupos/banners");
            }

            // CORREGIDO: Asegurar que el grupo sea público por defecto
            var grupo = new Grupo
            {
                Nombre = model.Nombre ?? "",
                Descripcion = model.Descripcion ?? "",
                Categoria = model.Categoria ?? "",
                CreadorId = usuario.Id,
                FotoGrupo = fotoGrupoPath,
                BannerGrupo = bannerGrupoPath,
                EsPublico = model.EsPublico, // Usar el valor del modelo
                FechaCreacion = DateTime.UtcNow
            };

            _context.Grupos.Add(grupo);
            await _context.SaveChangesAsync();

            // Agregar al creador como administrador
            var miembro = new MiembroGrupo
            {
                GrupoId = grupo.Id,
                UsuarioId = usuario.Id,
                Rol = "Administrador",
                FechaUnion = DateTime.UtcNow
            };

            _context.MiembrosGrupo.Add(miembro);
            await _context.SaveChangesAsync();

            TempData["Ok"] = $"Grupo '{model.Nombre}' creado exitosamente!";
            return RedirectToAction("Detalle", new { id = grupo.Id });
        }

        // POST: Grupos/Unirse
        [HttpPost]
        public async Task<IActionResult> Unirse([FromBody] GrupoRequest request)
        {
            // CORREGIDO: Verificación de autenticación mejorada
            if (!User.Identity?.IsAuthenticated ?? false)
                return Json(new { success = false, message = "No autenticado" });

            var usuario = await _userManager.GetUserAsync(User);
            
            // CORREGIDO: Verificar que usuario no sea null
            if (usuario == null)
                return Json(new { success = false, message = "Usuario no encontrado" });

            var grupo = await _context.Grupos.FindAsync(request.GrupoId);

            if (grupo == null)
                return Json(new { success = false, message = "Grupo no encontrado" });

            // Verificar si ya es miembro
            var yaEsMiembro = await _context.MiembrosGrupo
                .AnyAsync(m => m.GrupoId == request.GrupoId && m.UsuarioId == usuario.Id);

            if (yaEsMiembro)
                return Json(new { success = false, message = "Ya eres miembro de este grupo" });

            var miembro = new MiembroGrupo
            {
                GrupoId = request.GrupoId,
                UsuarioId = usuario.Id,
                Rol = "Miembro",
                FechaUnion = DateTime.UtcNow
            };

            _context.MiembrosGrupo.Add(miembro);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Te has unido al grupo" });
        }

        // POST: Grupos/Publicar
        [HttpPost]
        public async Task<IActionResult> Publicar([FromBody] PublicacionRequest request)
        {
            // CORREGIDO: Verificación de autenticación mejorada
            if (!User.Identity?.IsAuthenticated ?? false)
                return Json(new { success = false, message = "No autenticado" });

            var usuario = await _userManager.GetUserAsync(User);
            
            // CORREGIDO: Verificar que usuario no sea null
            if (usuario == null)
                return Json(new { success = false, message = "Usuario no encontrado" });

            // Verificar si es miembro del grupo
            var esMiembro = await _context.MiembrosGrupo
                .AnyAsync(m => m.GrupoId == request.GrupoId && m.UsuarioId == usuario.Id);

            if (!esMiembro)
                return Json(new { success = false, message = "No eres miembro de este grupo" });

            var publicacion = new PublicacionGrupo
            {
                GrupoId = request.GrupoId,
                UsuarioId = usuario.Id,
                Contenido = request.Contenido ?? "",
                FechaPublicacion = DateTime.UtcNow
            };

            _context.PublicacionesGrupo.Add(publicacion);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Publicación creada" });
        }

        // GET: Grupos/Configuracion/{id}
        public async Task<IActionResult> Configuracion(int id, string seccion = "configuracion")
        {
            // CORREGIDO: Verificación de autenticación mejorada
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                TempData["Error"] = "Debes iniciar sesión.";
                return RedirectToAction("Login", "Account");
            }

            var usuario = await _userManager.GetUserAsync(User);
            
            // CORREGIDO: Verificar que usuario no sea null
            if (usuario == null)
            {
                TempData["Error"] = "No se pudo identificar al usuario.";
                return RedirectToAction("Login", "Account");
            }

            var grupo = await _context.Grupos
                .Include(g => g.Miembros)
                    .ThenInclude(m => m.Usuario)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (grupo == null) return NotFound();

            // Verificar si es administrador
            var esAdministrador = await _context.MiembrosGrupo
                .AnyAsync(m => m.GrupoId == id && m.UsuarioId == usuario.Id && m.Rol == "Administrador");

            if (!esAdministrador)
            {
                TempData["Error"] = "No tienes permisos para configurar este grupo.";
                return RedirectToAction("Detalle", new { id });
            }

            var viewModel = new ConfiguracionGrupoViewModel
            {
                GrupoId = grupo.Id,
                Nombre = grupo.Nombre ?? "",
                Descripcion = grupo.Descripcion ?? "",
                Categoria = grupo.Categoria ?? "",
                EsPublico = grupo.EsPublico,
                // Solo para visualización
                FotoGrupoActual = grupo.FotoGrupo,
                BannerGrupoActual = grupo.BannerGrupo,
                Miembros = grupo.Miembros?.ToList() ?? new List<MiembroGrupo>()
            };

            ViewBag.SeccionActiva = seccion;
            return View(viewModel);
        }

        // NUEVO: POST: Grupos/SalirDelGrupo
        [HttpPost]
        public async Task<IActionResult> SalirDelGrupo([FromBody] GrupoRequest request)
        {
            // CORREGIDO: Verificación de autenticación mejorada
            if (!User.Identity?.IsAuthenticated ?? false)
                return Json(new { success = false, message = "No autenticado" });

            var usuario = await _userManager.GetUserAsync(User);
            
            // CORREGIDO: Verificar que usuario no sea null
            if (usuario == null)
                return Json(new { success = false, message = "Usuario no encontrado" });
            
            // Verificar si el usuario es miembro del grupo
            var miembro = await _context.MiembrosGrupo
                .FirstOrDefaultAsync(m => m.GrupoId == request.GrupoId && m.UsuarioId == usuario.Id);

            if (miembro == null)
                return Json(new { success = false, message = "No eres miembro de este grupo" });

            // Verificar si es administrador (los administradores no pueden salir, deben transferir administración primero)
            if (miembro.Rol == "Administrador")
            {
                // Contar cuántos administradores hay
                var administradoresCount = await _context.MiembrosGrupo
                    .CountAsync(m => m.GrupoId == request.GrupoId && m.Rol == "Administrador");
                
                if (administradoresCount <= 1)
                {
                    return Json(new { success = false, message = "No puedes salir del grupo porque eres el único administrador. Debes transferir la administración primero o eliminar el grupo." });
                }
            }

            // Eliminar al miembro del grupo
            _context.MiembrosGrupo.Remove(miembro);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Has salido del grupo correctamente" });
        }

        // POST: Grupos/ActualizarConfiguracion
        [HttpPost]
        public async Task<IActionResult> ActualizarConfiguracion(ConfiguracionGrupoViewModel model)
        {
            // CORREGIDO: Verificación de autenticación mejorada
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                TempData["Error"] = "Debes iniciar sesión.";
                return RedirectToAction("Login", "Account");
            }

            var usuario = await _userManager.GetUserAsync(User);
            
            // CORREGIDO: Verificar que usuario no sea null
            if (usuario == null)
            {
                TempData["Error"] = "No se pudo identificar al usuario.";
                return RedirectToAction("Login", "Account");
            }

            var grupo = await _context.Grupos.FindAsync(model.GrupoId);

            if (grupo == null) return NotFound();

            // Verificar si es administrador
            var esAdministrador = await _context.MiembrosGrupo
                .AnyAsync(m => m.GrupoId == model.GrupoId && m.UsuarioId == usuario.Id && m.Rol == "Administrador");

            if (!esAdministrador)
            {
                TempData["Error"] = "No tienes permisos para configurar este grupo.";
                return RedirectToAction("Detalle", new { id = model.GrupoId });
            }

            // IMPORTANTE: Remover la validación de campos que no vienen del formulario
            ModelState.Remove("FotoGrupoActual");
            ModelState.Remove("BannerGrupoActual");
            ModelState.Remove("Miembros");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["Error"] = $"Errores de validación: {string.Join(", ", errors)}";
                
                // Recargar datos para la vista
                var grupoActual = await _context.Grupos
                    .Include(g => g.Miembros)
                        .ThenInclude(m => m.Usuario)
                    .FirstOrDefaultAsync(g => g.Id == model.GrupoId);

                if (grupoActual != null)
                {
                    model.FotoGrupoActual = grupoActual.FotoGrupo;
                    model.BannerGrupoActual = grupoActual.BannerGrupo;
                    model.Miembros = grupoActual.Miembros?.ToList() ?? new List<MiembroGrupo>();
                }

                ViewBag.SeccionActiva = "configuracion";
                return View("Configuracion", model);
            }

            try
            {
                // Actualizar datos del grupo
                grupo.Nombre = model.Nombre ?? "";
                grupo.Descripcion = model.Descripcion ?? "";
                grupo.Categoria = model.Categoria ?? "";
                grupo.EsPublico = model.EsPublico;

                // Guardar nuevas imágenes si se proporcionaron
                if (model.FotoGrupo != null && model.FotoGrupo.Length > 0)
                {
                    grupo.FotoGrupo = await GuardarArchivo(model.FotoGrupo, "grupos/fotos");
                }

                if (model.BannerGrupo != null && model.BannerGrupo.Length > 0)
                {
                    grupo.BannerGrupo = await GuardarArchivo(model.BannerGrupo, "grupos/banners");
                }

                await _context.SaveChangesAsync();
                TempData["Ok"] = "Configuración del grupo actualizada correctamente.";
                return RedirectToAction("Configuracion", new { id = model.GrupoId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al guardar los cambios: {ex.Message}";
                
                // Recargar datos para la vista en caso de error
                var grupoActual = await _context.Grupos
                    .Include(g => g.Miembros)
                        .ThenInclude(m => m.Usuario)
                    .FirstOrDefaultAsync(g => g.Id == model.GrupoId);

                if (grupoActual != null)
                {
                    model.FotoGrupoActual = grupoActual.FotoGrupo;
                    model.BannerGrupoActual = grupoActual.BannerGrupo;
                    model.Miembros = grupoActual.Miembros?.ToList() ?? new List<MiembroGrupo>();
                }

                ViewBag.SeccionActiva = "configuracion";
                return View("Configuracion", model);
            }
        }

        // POST: Grupos/SubirMultimedia
        [HttpPost]
        public async Task<IActionResult> SubirMultimedia([FromForm] MultimediaViewModel model)
        {
            // CORREGIDO: Verificación de autenticación mejorada
            if (!User.Identity?.IsAuthenticated ?? false)
                return Json(new { success = false, message = "No autenticado" });

            var usuario = await _userManager.GetUserAsync(User);
            
            // CORREGIDO: Verificar que usuario no sea null
            if (usuario == null)
                return Json(new { success = false, message = "Usuario no encontrado" });

            // Verificar si es miembro del grupo
            var esMiembro = await _context.MiembrosGrupo
                .AnyAsync(m => m.GrupoId == model.GrupoId && m.UsuarioId == usuario.Id);

            if (!esMiembro)
                return Json(new { success = false, message = "No eres miembro de este grupo" });

            if (model.Archivo == null || model.Archivo.Length == 0)
                return Json(new { success = false, message = "Selecciona un archivo" });

            try
            {
                // Guardar archivo
                var archivoPath = await GuardarArchivo(model.Archivo, "grupos/multimedia");

                var multimedia = new MultimediaGrupo
                {
                    GrupoId = model.GrupoId,
                    UsuarioId = usuario.Id,
                    UrlArchivo = archivoPath ?? "",
                    TipoArchivo = "imagen",
                    Descripcion = model.Descripcion ?? "",
                    FechaSubida = DateTime.UtcNow
                };

                _context.MultimediaGrupo.Add(multimedia);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Multimedia subida correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error al subir multimedia: {ex.Message}" });
            }
        }

        // POST: Grupos/ReaccionarMultimedia
        [HttpPost]
        public async Task<IActionResult> ReaccionarMultimedia([FromBody] ReaccionViewModel model)
        {
            // CORREGIDO: Verificación de autenticación mejorada
            if (!User.Identity?.IsAuthenticated ?? false)
                return Json(new { success = false, message = "No autenticado" });

            var usuario = await _userManager.GetUserAsync(User);
            
            // CORREGIDO: Verificar que usuario no sea null
            if (usuario == null)
                return Json(new { success = false, message = "Usuario no encontrado" });

            // Verificar si ya existe una reacción del usuario
            var reaccionExistente = await _context.ReaccionesMultimedia
                .FirstOrDefaultAsync(r => r.MultimediaId == model.MultimediaId && r.UsuarioId == usuario.Id);

            var multimedia = await _context.MultimediaGrupo.FindAsync(model.MultimediaId);
            if (multimedia == null)
                return Json(new { success = false, message = "Multimedia no encontrada" });

            if (reaccionExistente != null)
            {
                // Si ya existe una reacción, actualizarla
                if (reaccionExistente.TipoReaccion != model.TipoReaccion)
                {
                    // Restar la reacción anterior
                    switch (reaccionExistente.TipoReaccion)
                    {
                        case "like": multimedia.Likes--; break;
                        case "love": multimedia.Love--; break;
                        case "wow": multimedia.Wow--; break;
                        case "sad": multimedia.Sad--; break;
                        case "angry": multimedia.Angry--; break;
                    }

                    // Sumar la nueva reacción
                    switch (model.TipoReaccion)
                    {
                        case "like": multimedia.Likes++; break;
                        case "love": multimedia.Love++; break;
                        case "wow": multimedia.Wow++; break;
                        case "sad": multimedia.Sad++; break;
                        case "angry": multimedia.Angry++; break;
                    }

                    reaccionExistente.TipoReaccion = model.TipoReaccion ?? "";
                    reaccionExistente.FechaReaccion = DateTime.UtcNow;
                }
                else
                {
                    // Si es la misma reacción, eliminarla (toggle)
                    switch (model.TipoReaccion)
                    {
                        case "like": multimedia.Likes--; break;
                        case "love": multimedia.Love--; break;
                        case "wow": multimedia.Wow--; break;
                        case "sad": multimedia.Sad--; break;
                        case "angry": multimedia.Angry--; break;
                    }
                    _context.ReaccionesMultimedia.Remove(reaccionExistente);
                }
            }
            else
            {
                // Nueva reacción
                switch (model.TipoReaccion)
                {
                    case "like": multimedia.Likes++; break;
                    case "love": multimedia.Love++; break;
                    case "wow": multimedia.Wow++; break;
                    case "sad": multimedia.Sad++; break;
                    case "angry": multimedia.Angry++; break;
                }

                var nuevaReaccion = new ReaccionMultimedia
                {
                    MultimediaId = model.MultimediaId,
                    UsuarioId = usuario.Id,
                    TipoReaccion = model.TipoReaccion ?? "",
                    FechaReaccion = DateTime.UtcNow
                };

                _context.ReaccionesMultimedia.Add(nuevaReaccion);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Reacción agregada" });
        }

        // POST: Grupos/AgregarComentarioMultimedia
        [HttpPost]
        public async Task<IActionResult> AgregarComentarioMultimedia([FromBody] ComentarioMultimediaViewModel model)
        {
            // CORREGIDO: Verificación de autenticación mejorada
            if (!User.Identity?.IsAuthenticated ?? false)
                return Json(new { success = false, message = "No autenticado" });

            var usuario = await _userManager.GetUserAsync(User);
            
            // CORREGIDO: Verificar que usuario no sea null
            if (usuario == null)
                return Json(new { success = false, message = "Usuario no encontrado" });

            // Verificar si la multimedia existe
            var multimedia = await _context.MultimediaGrupo.FindAsync(model.MultimediaId);
            if (multimedia == null)
                return Json(new { success = false, message = "Multimedia no encontrada" });

            // Verificar si es miembro del grupo
            var esMiembro = await _context.MiembrosGrupo
                .AnyAsync(m => m.GrupoId == multimedia.GrupoId && m.UsuarioId == usuario.Id);

            if (!esMiembro)
                return Json(new { success = false, message = "No eres miembro de este grupo" });

            var comentario = new ComentarioMultimedia
            {
                MultimediaId = model.MultimediaId,
                UsuarioId = usuario.Id,
                Contenido = model.Contenido ?? "",
                FechaComentario = DateTime.UtcNow
            };

            _context.ComentariosMultimedia.Add(comentario);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Comentario agregado" });
        }

        // NUEVO: POST: Grupos/EliminarGrupo
        [HttpPost]
        public async Task<IActionResult> EliminarGrupo([FromBody] GrupoRequest request)
        {
            // CORREGIDO: Verificación de autenticación mejorada
            if (!User.Identity?.IsAuthenticated ?? false)
                return Json(new { success = false, message = "No autenticado" });

            var usuario = await _userManager.GetUserAsync(User);
            
            // CORREGIDO: Verificar que usuario no sea null
            if (usuario == null)
                return Json(new { success = false, message = "Usuario no encontrado" });

            var grupo = await _context.Grupos
                .Include(g => g.Miembros)
                .FirstOrDefaultAsync(g => g.Id == request.GrupoId);

            if (grupo == null)
                return Json(new { success = false, message = "Grupo no encontrado" });

            // Verificar si es administrador
            var esAdministrador = await _context.MiembrosGrupo
                .AnyAsync(m => m.GrupoId == request.GrupoId && m.UsuarioId == usuario.Id && m.Rol == "Administrador");

            if (!esAdministrador)
                return Json(new { success = false, message = "No tienes permisos para eliminar este grupo" });

            try
            {
                // Eliminar todas las relaciones primero para evitar conflictos de clave foránea
                var miembros = await _context.MiembrosGrupo.Where(m => m.GrupoId == request.GrupoId).ToListAsync();
                var publicaciones = await _context.PublicacionesGrupo.Where(p => p.GrupoId == request.GrupoId).ToListAsync();
                var multimedia = await _context.MultimediaGrupo.Where(m => m.GrupoId == request.GrupoId).ToListAsync();

                _context.MiembrosGrupo.RemoveRange(miembros);
                _context.PublicacionesGrupo.RemoveRange(publicaciones);
                _context.MultimediaGrupo.RemoveRange(multimedia);
                
                // Finalmente eliminar el grupo
                _context.Grupos.Remove(grupo);
                
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Grupo eliminado correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error al eliminar el grupo: {ex.Message}" });
            }
        }

        // NUEVO: POST: Grupos/ExpulsarMiembro
        [HttpPost]
        public async Task<IActionResult> ExpulsarMiembro([FromBody] GestionMiembroRequest request)
        {
            // CORREGIDO: Verificación de autenticación mejorada
            if (!User.Identity?.IsAuthenticated ?? false)
                return Json(new { success = false, message = "No autenticado" });

            var usuario = await _userManager.GetUserAsync(User);
            
            // CORREGIDO: Verificar que usuario no sea null
            if (usuario == null)
                return Json(new { success = false, message = "Usuario no encontrado" });

            // Verificar si es administrador
            var esAdministrador = await _context.MiembrosGrupo
                .AnyAsync(m => m.GrupoId == request.GrupoId && m.UsuarioId == usuario.Id && m.Rol == "Administrador");

            if (!esAdministrador)
                return Json(new { success = false, message = "No tienes permisos para expulsar miembros" });

            // Encontrar y eliminar al miembro
            var miembro = await _context.MiembrosGrupo
                .FirstOrDefaultAsync(m => m.GrupoId == request.GrupoId && m.UsuarioId == request.UsuarioId && m.Rol != "Administrador");

            if (miembro == null)
                return Json(new { success = false, message = "Miembro no encontrado o no se puede expulsar" });

            _context.MiembrosGrupo.Remove(miembro);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Miembro expulsado correctamente" });
        }

        // Método auxiliar para guardar archivos
        private async Task<string?> GuardarArchivo(IFormFile archivo, string carpeta)
        {
            if (archivo == null || archivo.Length == 0)
                return null;

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", carpeta);
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(archivo.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            return $"/uploads/{carpeta}/{fileName}";
        }

        // GET: Grupos/Detalle/{id}
        public async Task<IActionResult> Detalle(int id, string seccion = "miembros")
        {
            // CORREGIDO: Verificación de autenticación mejorada
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                TempData["Error"] = "Debes iniciar sesión para ver el grupo.";
                return RedirectToAction("Login", "Account");
            }

            var usuario = await _userManager.GetUserAsync(User);
            
            // CORREGIDO: Verificar que usuario no sea null
            if (usuario == null)
            {
                TempData["Error"] = "No se pudo identificar al usuario.";
                return RedirectToAction("Login", "Account");
            }
            
            // Obtener los grupos del usuario para el sidebar
            var misGruposIds = await _context.MiembrosGrupo
                .Where(m => m.UsuarioId == usuario.Id)
                .Select(m => m.GrupoId)
                .ToListAsync();

            var misGrupos = await _context.Grupos
                .Include(g => g.Miembros)
                .Where(g => misGruposIds.Contains(g.Id))
                .ToListAsync();

            var grupo = await _context.Grupos
                .Include(g => g.Creador)
                .Include(g => g.Miembros!)
                    .ThenInclude(m => m.Usuario)
                .Include(g => g.Publicaciones!)
                    .ThenInclude(p => p.Usuario)
                .Include(g => g.Multimedia!)
                    .ThenInclude(m => m.Usuario)
                .Include(g => g.Multimedia!)
                    .ThenInclude(m => m.Comentarios!)
                        .ThenInclude(c => c.Usuario)
                .Include(g => g.Multimedia!)
                    .ThenInclude(m => m.Reacciones) // NUEVO: Incluir reacciones
                .FirstOrDefaultAsync(g => g.Id == id);

            if (grupo == null) return NotFound();

            var esMiembro = await _context.MiembrosGrupo
                .AnyAsync(m => m.GrupoId == id && m.UsuarioId == usuario.Id);

            var esAdministrador = await _context.MiembrosGrupo
                .AnyAsync(m => m.GrupoId == id && m.UsuarioId == usuario.Id && m.Rol == "Administrador");

            var viewModel = new DetalleGrupoViewModel
            {
                Grupo = grupo,
                MisGrupos = misGrupos,
                Miembros = grupo.Miembros?.ToList() ?? new List<MiembroGrupo>(),
                Publicaciones = grupo.Publicaciones?
                    .OrderByDescending(p => p.FechaPublicacion)
                    .ToList() ?? new List<PublicacionGrupo>(),
                Multimedia = grupo.Multimedia?
                    .OrderByDescending(m => m.FechaSubida)
                    .Take(12)
                    .ToList() ?? new List<MultimediaGrupo>(),
                EsMiembro = esMiembro,
                EsAdministrador = esAdministrador
            };

            ViewBag.UsuarioActualId = usuario.Id;
            ViewBag.SeccionActiva = seccion;

            return View(viewModel);
        }
    }

    // Clases para manejar las solicitudes JSON (FUERA de la clase del controlador)
    public class GrupoRequest
    {
        public int GrupoId { get; set; }
    }

    public class PublicacionRequest
    {
        public int GrupoId { get; set; }
        public string Contenido { get; set; } = string.Empty; // CORREGIDO: Inicializado
    }

    public class GestionMiembroRequest
    {
        public int GrupoId { get; set; }
        public string UsuarioId { get; set; } = string.Empty; // CORREGIDO: Inicializado
    }

    // CORREGIDO: Clases adicionales que faltaban
    public class MultimediaViewModel
    {
        public int GrupoId { get; set; }
        public IFormFile? Archivo { get; set; }
        public string? Descripcion { get; set; }
    }

    public class ReaccionViewModel
    {
        public int MultimediaId { get; set; }
        public string? TipoReaccion { get; set; }
    }

    public class ComentarioMultimediaViewModel
    {
        public int MultimediaId { get; set; }
        public string? Contenido { get; set; }
    }
}