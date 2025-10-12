using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Proyecto_Gaming.Models;
using Proyecto_Gaming.ViewModels;
using Proyecto_Gaming.Data;
using Npgsql;
using System.Diagnostics;

namespace Proyecto_Gaming.Controllers
{
    public class ConfiguracionController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ConfiguracionController(UserManager<Usuario> userManager, 
                                     ApplicationDbContext context,
                                     IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _context = context;
            _environment = environment;
        }

        // GET: Configuracion/Index
        public async Task<IActionResult> Index()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                TempData["Error"] = "Debes iniciar sesión para acceder a la configuración.";
                return RedirectToAction("Login", "Account");
            }

            var viewModel = await ConstruirConfiguracionViewModel(usuario);
            return View(viewModel);
        }

        // POST: Configuracion/ActualizarPerfil - CON SQL DIRECTO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarPerfil(ConfiguracionViewModel model)
        {
            try
            {
                var usuario = await _userManager.GetUserAsync(User);
                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado.";
                    return RedirectToAction("Index");
                }

                // SOLUCIÓN: Usar SQL directo para evitar problemas de Entity Framework
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                try
                {
                    var query = @"
                        UPDATE ""AspNetUsers"" 
                        SET 
                            display_name = @displayName,
                            nombre_real = @nombreReal,
                            biografia = @biografia,
                            pais = @pais,
                            fecha_nacimiento = @fechaNacimiento
                        WHERE ""Id"" = @userId";

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = query;
                        command.Parameters.Add(new NpgsqlParameter("displayName", model.UserName ?? ""));
                        command.Parameters.Add(new NpgsqlParameter("nombreReal", model.NombreReal ?? ""));
                        command.Parameters.Add(new NpgsqlParameter("biografia", model.Biografia ?? ""));
                        command.Parameters.Add(new NpgsqlParameter("pais", model.Pais ?? ""));
                        command.Parameters.Add(new NpgsqlParameter("fechaNacimiento", model.FechaNacimiento ?? DateTime.MinValue));
                        command.Parameters.Add(new NpgsqlParameter("userId", usuario.Id));

                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        
                        if (rowsAffected > 0)
                        {
                            // Actualizar avatar por separado si se subió
                            if (model.FotoPerfil != null && model.FotoPerfil.Length > 0)
                            {
                                var fileName = await GuardarAvatar(model.FotoPerfil);
                                
                                using (var avatarCommand = connection.CreateCommand())
                                {
                                    avatarCommand.CommandText = @"UPDATE ""AspNetUsers"" SET foto_perfil = @fotoPerfil WHERE ""Id"" = @userId";
                                    avatarCommand.Parameters.Add(new NpgsqlParameter("fotoPerfil", fileName));
                                    avatarCommand.Parameters.Add(new NpgsqlParameter("userId", usuario.Id));
                                    await avatarCommand.ExecuteNonQueryAsync();
                                }
                            }
                            
                            TempData["Ok"] = "✅ Perfil actualizado correctamente!";
                        }
                        else
                        {
                            TempData["Error"] = "❌ No se pudo actualizar el perfil.";
                        }
                    }
                }
                finally
                {
                    await connection.CloseAsync();
                }

                // Recargar el usuario actualizado
                var usuarioActualizado = await _userManager.FindByIdAsync(usuario.Id);
                var updatedViewModel = await ConstruirConfiguracionViewModel(usuarioActualizado);
                return View("Index", updatedViewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Error: {ex.Message}";
                var usuario = await _userManager.GetUserAsync(User);
                var currentViewModel = await ConstruirConfiguracionViewModel(usuario);
                return View("Index", currentViewModel);
            }
        }

        // POST: Configuracion/ActualizarPlataformas - CON SQL DIRECTO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarPlataformas(List<string> plataformasSeleccionadas)
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction("Index");
            }

            try
            {
                // SOLUCIÓN: Usar SQL directo
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"UPDATE ""AspNetUsers"" SET plataforma_preferida = @plataformas WHERE ""Id"" = @userId";
                        command.Parameters.Add(new NpgsqlParameter("plataformas", plataformasSeleccionadas != null ? string.Join(",", plataformasSeleccionadas) : ""));
                        command.Parameters.Add(new NpgsqlParameter("userId", usuario.Id));
                        
                        var rowsAffected = await command.ExecuteNonQueryAsync();
                        
                        if (rowsAffected > 0)
                        {
                            TempData["Ok"] = "Plataformas preferidas actualizadas correctamente.";
                        }
                        else
                        {
                            TempData["Error"] = "Error al actualizar las plataformas.";
                        }
                    }
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // POST: Configuracion/CambiarContrasena
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarContrasena(string currentPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword)
            {
                TempData["Error"] = "Las contraseñas no coinciden o están vacías.";
                return RedirectToAction("Index");
            }

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction("Index");
            }

            var resultado = await _userManager.ChangePasswordAsync(usuario, currentPassword, newPassword);
            if (resultado.Succeeded)
            {
                TempData["Ok"] = "Contraseña cambiada correctamente.";
            }
            else
            {
                TempData["Error"] = "Error al cambiar la contraseña: " + string.Join(", ", resultado.Errors.Select(e => e.Description));
            }

            return RedirectToAction("Index");
        }

        // POST: Configuracion/EliminarCuenta
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarCuenta(string confirmacion)
        {
            if (confirmacion?.ToLower() != "eliminar")
            {
                TempData["Error"] = "Debes escribir 'ELIMINAR' para confirmar la eliminación de la cuenta.";
                return RedirectToAction("Index");
            }

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction("Index");
            }

            try
            {
                // Eliminar registros de biblioteca primero
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();
                
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"DELETE FROM ""BibliotecaUsuario"" WHERE ""UsuarioId"" = @userId";
                    command.Parameters.Add(new NpgsqlParameter("userId", usuario.Id));
                    await command.ExecuteNonQueryAsync();
                }
                await connection.CloseAsync();

                // Luego eliminar el usuario
                var resultado = await _userManager.DeleteAsync(usuario);
                if (resultado.Succeeded)
                {
                    await _userManager.UpdateSecurityStampAsync(usuario);
                    TempData["Ok"] = "Cuenta eliminada correctamente.";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["Error"] = "Error al eliminar la cuenta: " + string.Join(", ", resultado.Errors.Select(e => e.Description));
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar la cuenta: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        private async Task<string> GuardarAvatar(IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
                return null;

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "avatars");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(archivo.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            return $"/avatars/{fileName}";
        }

        private async Task<ConfiguracionViewModel> ConstruirConfiguracionViewModel(Usuario usuario)
        {
            // Obtener plataformas seleccionadas actualmente
            var plataformasActuales = usuario.PlataformaPreferida?.Split(',') ?? new string[0];

            var plataformasDisponibles = new List<PlataformaPreferida>
            {
                new PlataformaPreferida { Id = "pc", Nombre = "PC", Seleccionada = plataformasActuales.Contains("pc") },
                new PlataformaPreferida { Id = "playstation", Nombre = "PlayStation", Seleccionada = plataformasActuales.Contains("playstation") },
                new PlataformaPreferida { Id = "xbox", Nombre = "Xbox", Seleccionada = plataformasActuales.Contains("xbox") },
                new PlataformaPreferida { Id = "nintendo", Nombre = "Nintendo", Seleccionada = plataformasActuales.Contains("nintendo") },
                new PlataformaPreferida { Id = "mobile", Nombre = "Mobile", Seleccionada = plataformasActuales.Contains("mobile") }
            };

            return new ConfiguracionViewModel
            {
                // Usar DisplayName si existe, si no usar UserName
                UserName = usuario.DisplayName ?? usuario.UserName,
                NombreReal = usuario.NombreReal,
                Email = usuario.Email,
                FechaNacimiento = usuario.FechaNacimiento == DateTime.MinValue ? null : usuario.FechaNacimiento,
                Biografia = usuario.Biografia,
                Pais = usuario.Pais,
                FotoPerfilUrl = usuario.FotoPerfil,
                PlataformasPreferidas = plataformasDisponibles
            };
        }
    }
}